#light

/// All user-end AutoCAD commands advertised by the plug-in
module BioStream.Micado.Plugin.Commands

open BioStream
open BioStream.Micado.Core
open BioStream.Micado.Plugin
open Autodesk.AutoCAD.Runtime

[<CommandMethod("PlacePunch")>]
/// asks the user for a point and places a punch centered around the picked point
let placePunch() =
    Editor.promptPoint "Pick center of punch: "
 |> Option.map (fun point3d -> Creation.punch (Geometry.to2d point3d))
 |> Option.map Database.writeEntityAndReturn 
 |> Option.map (fun (entity) -> entity :?> Punch)
 |> Option.map (fun (punch) -> Editor.writeLine ("Created punch at" ^ punch.Center.ToString() ^ "."); punch.Dispose())
 |> ignore

/// asks the user for a flow segment and places a valve centered on the segment, 
/// as close as possible to where the user clicked
[<CommandMethod("PlaceValve")>]
let placeValve() =
    Editor.promptSelectFlowSegmentAndPoint "Select point on flow segment: "
 |> Option.map (fun (flowSegment, point3d) -> Creation.valve flowSegment (Geometry.to2d point3d))
 |> Option.map Database.writeEntityAndReturn
 |> Option.map (fun (entity) -> entity :?> Valve)
 |> Option.map (fun (valve) -> Editor.writeLine ("Created valve at" ^ valve.Center.ToString() ^ "."); valve.Dispose())
 |> ignore

/// automatic routing of control layer
/// will try to connect each unconnected control line to some unconnected control punch
/// according to the user settings
[<CommandMethod("ConnectValvesToPunches")>]
let connectValvesToPunches() =
    use chip = Chip.FromDatabase.create()
    let nLines = chip.ControlLayer.Lines.Length
    let nUnconnectedLines = chip.ControlLayer.UnconnectedLines.Length
    let nPunches = chip.ControlLayer.Punches.Length
    let nUnconnectedPunches = chip.ControlLayer.UnconnectedPunches.Length
    let outOf (nUnconnected : int) (n : int) =
        if nUnconnected=n then "" else " (out of " ^ n.ToString() ^ ")"
    Editor.writeLine (sprintf "Collected %d unconnected control lines%s and %d unconnected punches%s."
                              nUnconnectedLines
                              (outOf nUnconnectedLines nLines)
                              nUnconnectedPunches
                              (outOf nUnconnectedPunches nPunches))
    if nUnconnectedLines = 0
    then Editor.writeLine "Routing aborted, because there is nothing to connect."
    else
    if nUnconnectedPunches < nUnconnectedLines
    then Editor.writeLine "Routing aborted, because the number of unconnected punches is less than the number of unconnected control lines."
    else
    let chipGrid =  Routing.createChipGrid chip    
    let mcfSolution = Routing.minCostFlowRouting chipGrid
    match mcfSolution with
    | None -> 
        Editor.writeLine "Routing failed: try more relaxed settings, perhaps."
    | Some mcfSolution -> 
        let iterativeSolver = new Routing.IterativeRouting (chipGrid, mcfSolution)
        let presenter = Routing.presentConnections chipGrid
        let n = iterativeSolver.stabilize()
        let solution = iterativeSolver.Solution
        solution |> presenter |> Database.writeEntities |> ignore
        Editor.writeLine ("Routing succeeded: " ^ solution.Length.ToString() ^ " new connections.")

module Instructions = begin
    open BioStream.Micado.Core.Instructions
    open BioStream.Micado.Plugin.Editor.Extra
    
    open Autodesk.AutoCAD.DatabaseServices
    open Autodesk.AutoCAD.ApplicationServices
    
    open System.Collections.Generic
    
    let doc() =
        Database.doc()

    let computeInstructionChip() =
        new InstructionChip (Chip.FromDatabase.create())
                
    type CacheEntry() =
        let instructionChip = computeInstructionChip()
        let mutable boxes = Map.empty : Map<string, FlowBox.FlowBox>
        let mutable unsavedChanges = false
        let instructions = new Dictionary<Entity, Instruction>()
        let cleanup() =
            (instructionChip :> System.IDisposable).Dispose()
            for kv in instructions do
                kv.Key.Dispose()
        member v.InstructionChip = instructionChip
        member v.Boxes with get() = boxes 
                        and set(value) = boxes <- value
                                         unsavedChanges <- true
        member v.AddInstruction (instruction : Instruction) =
            // if entity is already in the instructions map,
            // sounds risky to dispose of it?
            instructions.[instruction.Entity] <- instruction
            unsavedChanges <- true
        member v.GetInstruction entity =
            if instructions.ContainsKey entity |> not
            then Editor.writeLine "No instruction associated with entity."
                 None
            else Some instructions.[entity]
        member v.Instructions with get() = let a = instructions.Values |> Array.of_seq
                                           a |> Array.sort (fun (i : Instruction) (i' : Instruction) -> 
                                                                compare i.Name i'.Name)
                                           a
        member v.SimilarInstructions (instruction : Instruction) =
            instructions.Values
         |> Seq.filter (fun (instruction' : Instruction) -> 
                            instruction'.Entity.ObjectId = instruction.Entity.ObjectId 
                         || instruction'.Name = instruction.Name)                         
        member v.UnsavedChanges with get() = unsavedChanges and set(value) = unsavedChanges <- value
        interface System.IDisposable with
            member v.Dispose() = cleanup()
                        
    let globalCache = new Dictionary<Document, CacheEntry>()

    let deleteCacheEntry(d) =
        let ok,entry = globalCache.TryGetValue(d)
        if ok then globalCache.Remove(d) |> ignore; (entry :> System.IDisposable).Dispose()             
                    
    let activeCacheEntry() =
        let d = doc()
        let ok,res = globalCache.TryGetValue(d)
        if ok
        then res
        else let res = new CacheEntry()
             globalCache.[d] <- res
             d.BeginDocumentClose.Add (fun args -> deleteCacheEntry(d))
             res
        
    let activeInstructionChip() =
        activeCacheEntry().InstructionChip
        
    let activeBoxes() = activeCacheEntry().Boxes
        
    let activeInstructions() = activeCacheEntry().Instructions
        
    let addInstruction instruction = activeCacheEntry().AddInstruction instruction
    
    let getInstruction entity = activeCacheEntry().GetInstruction entity
        
    let savedActiveCache() = activeCacheEntry().UnsavedChanges |> not

    let savingActiveCache() = activeCacheEntry().UnsavedChanges <- false
            
    let setActiveBoxes map =
        activeCacheEntry().Boxes <- map
    
    [<CommandMethod("micado_clear_cache")>]    
    /// clears the cache entry of the active document
    let micado_clear_cache() =
        let d = doc()
        if not (globalCache.ContainsKey d)
        then Editor.writeLine "(No Cache to clear.)"
        else
        if savedActiveCache() ||
           Editor.promptYesOrNo false 
                                "Are you sure you want to clear unsaved boxes and instructions associated with this drawing?"
        then deleteCacheEntry d;
             Editor.writeLine "(Cache cleared.)"
        else Editor.writeLine "(Cache kept.)"

    // won't be able to create a ChipInstruction if the flow representation fails
    // because there is no flow segments
    let tryCommand command =
        try
            command()
        with Failure(msg) ->
            Editor.writeLine msg
                    
    [<CommandMethod("micado_number_control_lines")>]    
    /// prompts the user to number the control lines by selecting them in turn
    let micado_number_control_lines() = tryCommand (fun () ->
        activeInstructionChip().Inferred.Chip.ControlLayer.numberLines()        
     |> ignore)

    let addBox (name,box) =
        setActiveBoxes(Map.add name box (activeBoxes()))
        
    let markSaveClear ic box =
        let save name = addBox (name,box)            
        Debug.drawFlowBox ic box
        Editor.promptIdNameNotEmpty "Name box: " |> Option.map save |> ignore
        Editor.clearMarks()
        
    let promptNewBox makeBox =
        let ic = activeInstructionChip()
        makeBox ic
     |> Option.map (markSaveClear ic)
     |> ignore

    [<CommandMethod("micado_new_box_input")>]    
    /// prompts the user to create a new input box
    let micado_new_box_input() = tryCommand (fun () ->
        promptNewBox Interactive.promptInputBox)

    [<CommandMethod("micado_new_box_output")>]    
    /// prompts the user to create a new output box
    let micado_new_box_output() = tryCommand (fun () ->
        promptNewBox Interactive.promptOutputBox)
    
    [<CommandMethod("micado_new_box_or_input")>]    
    /// prompts the user to create a new or box made of input boxes
    let micado_new_box_or_input() = tryCommand (fun () ->
        promptNewBox Interactive.promptOrInputBox)

    [<CommandMethod("micado_new_box_or_output")>]    
    /// prompts the user to create a new or box made of output boxes
    let micado_new_box_or_output() = tryCommand (fun () ->
        promptNewBox Interactive.promptOrOutputBox)

    [<CommandMethod("micado_new_box_path")>]    
    /// prompts the user to create a new path box        
    let micado_new_box_path() = tryCommand (fun () ->
        promptNewBox Interactive.promptPathBox)
                         
    let displayAttachmentKind = function
        | Instructions.Attachments.Complete -> "complete"
        | Instructions.Attachments.Input _ -> "input"
        | Instructions.Attachments.Output _ -> "output"
        | Instructions.Attachments.Intermediary (_,_) -> "intermediary"
    
    let boxDisplayName name box =
        (FlowBox.attachmentKind box |> displayAttachmentKind) ^ " " ^ name
        
    let activeBoxesProperties() =
        let boxes =
            [for kv in activeBoxes() do
                let name = kv.Key
                let box = kv.Value
                let kind = FlowBox.attachmentKind box
                yield boxDisplayName name box, name, kind, box]
        List.sort compare boxes
    
    let boxPropDisplayName (x,_,_,_) = x
    let boxPropName (_,x,_,_) = x
    let boxPropKind (_,_,x,_) = x
    let boxPropBox (_,_,_,x) = x
    
    let allGood x = true
    
    let promptSelectBoxAndName kindFilter boxFilter message =
        let map = activeBoxes()
        let keys = activeBoxesProperties() 
                |> List.filter (boxPropKind >> kindFilter)
                |> List.filter (boxPropBox >> boxFilter)
                |> List.map boxPropName
        if keys.Length=0
        then Editor.writeLine "(None)"
             None
        else 
        Editor.promptSelectIdName message keys
     |> Option.bind (fun (s) ->
                        match map.TryFind(s) with
                        | Some res -> Some (res, s)
                        | None -> Editor.writeLine "No such box."
                                  None)
    
    let promptSelectBox kindFilter boxFilter message = 
        promptSelectBoxAndName kindFilter boxFilter message
     |> Option.map fst
    
    let promptSelectAnyBoxAndName = promptSelectBoxAndName allGood allGood
    let promptSelectAnyBox = promptSelectBox allGood allGood
            
    [<CommandMethod("micado_mark_box")>]    
    /// prompts the user to select a box, marking it on the drawing
    let micado_mark_box() = tryCommand (fun () ->
        match promptSelectAnyBoxAndName "Box " with
        | None -> ()
        | Some (box, name) ->
            Debug.drawFlowBox (activeInstructionChip()) box
            Editor.writeLine (boxDisplayName name box))
 
    [<CommandMethod("micado_list_boxes")>]
    /// prints out the list of boxes for active drawing
    let micado_list_boxes() = tryCommand (fun () ->
        activeBoxesProperties() 
     |> List.iter (fun prop -> Editor.writeLine (boxPropDisplayName prop)))

    let selectBoxesMessage (i : int) = "Box #" ^ i.ToString()
    
    let arrayOfRevList = Routing.arrayOfRevList
    
    let promptSelectBoxesOfSameKind() =
        let message = selectBoxesMessage
        let rec acc kindFilter boxes i =
            let boxFilter box = List.mem box boxes |> not
            match promptSelectBox kindFilter boxFilter (message i) with
            | None -> Some (boxes |> arrayOfRevList)
            | Some box -> acc kindFilter (box::boxes) (i+1)
        match promptSelectAnyBox (message 1) with
        | None -> None
        | Some box ->
            acc (Attachments.sameKind (FlowBox.attachmentKind box)) [box] 2
    
    let promptSelectBoxesForSeq() =
        let message = selectBoxesMessage
        let hasInputAttachment kind =
            match kind with
            | Attachments.Complete | Attachments.Input _ -> false
            | _ -> true
        let hasOutputAttachment kind =
            match kind with
            | Attachments.Complete | Attachments.Output _ -> false
            | _ -> true
        let rec acc boxes i =
            match promptSelectBox hasInputAttachment allGood (message i) with
            | None -> Some (boxes |> arrayOfRevList)
            | Some box -> 
                let boxes' = box :: boxes
                if box |> FlowBox.attachmentKind |> hasOutputAttachment |> not
                then Editor.writeLine "(Stopping at Output)"
                     Some (boxes' |> arrayOfRevList)
                else acc boxes' (i+1)
        match promptSelectBox hasOutputAttachment allGood (message 1) with
        | None -> None
        | Some box ->
            acc [box] 2
            
    let promptNewCombinationBox selectBoxes makeBox =
        selectBoxes()
     |> Option.map (let ic = activeInstructionChip()
                    makeBox ic >> Option.map (markSaveClear ic))
     |> ignore

    [<CommandMethod("micado_new_box_or")>]    
    /// prompts the user to create a new or box
    let micado_new_box_or() = tryCommand (fun () ->
        promptNewCombinationBox promptSelectBoxesOfSameKind Interactive.promptOrBox)
        
    [<CommandMethod("micado_new_box_and")>]    
    /// prompts the user to create a new and box
    let micado_new_box_and() = tryCommand (fun () ->
        promptNewCombinationBox promptSelectBoxesOfSameKind Interactive.promptAndBox)

    [<CommandMethod("micado_new_box_seq")>]    
    /// prompts the user to create a new seq box
    let micado_new_box_seq() = tryCommand (fun () ->
        promptNewCombinationBox promptSelectBoxesForSeq Interactive.promptSeqBox)

    [<CommandMethod("micado_rename_box")>]    
    /// prompts the user to select a box and rename it
    let micado_rename_box() = tryCommand (fun () ->
        match promptSelectAnyBoxAndName "Box to rename " with
        | None -> ()
        | Some (box, name) ->
            Editor.promptIdNameNotEmpty "New name for box: "
         |> Option.map (fun name' ->
                            setActiveBoxes(activeBoxes() |> Map.remove name |> Map.add name' box)
                            Editor.writeLine ("Renamed box " ^ name ^ " to " ^ name'))
         |> ignore)
            
    [<CommandMethod("micado_new_instruction_set")>]
    /// prompts the user to select a box and builds an instruction set out of it
    let micado_new_instruction_set() = tryCommand (fun () ->
        let ic = activeInstructionChip()
        match promptSelectAnyBoxAndName "Box " with
        | None -> ()
        | Some (box, name) ->
            Debug.drawFlowBox ic box
            let instructions = Instructions.Interactive.promptInstructions ic name box
            Editor.clearMarks()
            match instructions with
            | None -> ()
            | Some instructions -> Seq.iter addInstruction instructions)

    let markInstruction (instruction : Instruction) =
        Editor.writeLine ((if instruction.Partial then "partial" else "complete")
                          ^ " instruction " ^ instruction.Name)
        Debug.drawExtents instruction.Extents
        Debug.drawUsed (activeInstructionChip()) instruction.Used
        
    [<CommandMethod("micado_mark_instruction")>]
    /// prompts the user to select an entity associated with an instruction and marks the instruction on the drawing
    let micado_mark_instruction() = tryCommand (fun () ->
        Editor.promptSelectEntity "Select an entity associated with the extents of an instruction: "
     |> Option.bind getInstruction
     |> Option.map markInstruction
     |> ignore)

    [<CommandMethod("micado_list_instructions")>]
    /// mark the instructions one after the other
    let micado_list_instructions() = tryCommand (fun () ->
        Editor.clearMarks()
        let instructions = activeInstructions()
        if instructions.Length = 0
        then Editor.writeLine "(None)"
        else
        for instruction in instructions do
            markInstruction instruction)
            
    [<CommandMethod("micado_export_to_gui")>]
    /// export files for the java GUI
    let micado_export_to_gui() = tryCommand (fun () ->
        let ic = activeInstructionChip()
        let instructions = activeInstructions()
        if ic.Inferred.Default || instructions.Length = ic.Inferred.OpenSets.Length
        then Export.GUI.prompt ic instructions |> ignore
        else Editor.writeLine "Cannot export GUI because control generation is out of date with instructions."
    )
        
    [<CommandMethod("micado_export_boxes_and_instructions")>]
    /// export boxes and instructions in order to import them back later
    let micado_export_boxes_and_instructions() = tryCommand (fun () ->
        let boxes = 
            activeBoxesProperties() 
         |> Seq.map (fun prop -> boxPropName prop, boxPropBox prop)
        if Serialization.export boxes (activeInstructions())
        then savingActiveCache())
        
    [<CommandMethod("micado_import_boxes_and_instructions")>]
    /// import boxes and instructions from an earlier exported file
    let micado_import_boxes_and_instructions() = tryCommand (fun () ->
        let entry = activeCacheEntry()
        let ic = activeInstructionChip()
        let currentBoxes = activeBoxes()
        let checkUsed (used : Used) =
            (used.Edges.IsEmpty || used.Edges.MaximumElement < ic.Representation.EdgeCount)
         && (used.Valves.IsEmpty || used.Valves.MaximumElement < ic.Chip.ControlLayer.Valves.Length)            
        let checkInstruction (instruction : Instruction) =
            if instruction.Entity = null
            then Editor.writeLine ("Skipping instruction " ^ instruction.Name ^ " because associated entity doesn't exist in drawing.")
                 false
            else
            if checkUsed instruction.Used |> not
            then Editor.writeLine ("Skipping instruction " ^ instruction.Name ^ " because it doesn't fit with current flow representation.")
                 false
            else
            let sims = entry.SimilarInstructions instruction
            if Seq.nonempty sims
            then let sim = Seq.hd sims
                 if sim.Entity.ObjectId = instruction.Entity.ObjectId
                 then Editor.writeLine ("Skipping instruction " ^ instruction.Name ^ " because associated entity is already in use.")
                 else Editor.writeLine ("Skipping instruction " ^ instruction.Name ^ " because its name is already in use.")
                 false
            else Editor.writeLine ("Adding instruction " ^ instruction.Name ^ ".")
                 true
        let checkNameBox (name,box) =
           if currentBoxes.ContainsKey name
           then Editor.writeLine ("Skipping box " ^ name ^ "because its name is already in use.")
                false
           else
           if checkUsed (FlowBox.mentionedUsed box) |> not
           then Editor.writeLine ("Skipping box " ^ name ^ "because it doesn't fit with current flow representation.")
                false
           else Editor.writeLine ("Adding box " ^ name ^ ".")
                true
        match Serialization.import() with
        | None -> ()
        | Some (boxes, instructions) ->
            let keepBoxes = boxes |> List.filter checkNameBox
            let keepInstructions = instructions |> Array.filter checkInstruction
            keepBoxes |> Seq.iter addBox
            keepInstructions |> Seq.iter addInstruction
            Editor.writeLine ("Summary")
            Editor.writeLine ("Added " ^ keepBoxes.Length.ToString() ^ " boxes (out of " ^ boxes.Length.ToString() ^ ").")
            Editor.writeLine ("Added " ^ keepInstructions.Length.ToString() ^ " instructions (out of " ^ instructions.Length.ToString() ^ ")."))    
    
    // micado_
    //        new_
    //            box (?)
    //            box_
    //                input (v)
    //                output (v)
    //                path (v)
    //                seq (v)
    //                and (v)
    //                or (v)
    //                  _input (v)
    //                  _output (v)
    //            instruction_set (v)
    //        mark_
    //             box (print out kind and name) (v)
    //             instruction (print out partial/complete and name) (v)
    //        list_
    //             boxes (v)
    //             instructions (v)
    //        rename_box (v)
    //        export_
    //               to_gui (v)
    //               boxes_and_instructions
    //        import_boxes_and_instructions
    //        number_control_lines (v)
    //        clear_cache (v)
    
    [<CommandMethod("micado_generate_control")>]
    /// generate the control lines from the instructions
    let micado_generate_control() = tryCommand (fun () ->
        let ic = activeInstructionChip()
        let instructions = activeInstructions()
        ControlInference.Plugin.generate ic instructions
    )

    [<CommandMethod("micado_generate_multiplexed_control")>]
    /// generate the control lines with multiplexers from the instructions and boxes
    let micado_generate_multiplexed_control() = tryCommand (fun () ->
        let ic = activeInstructionChip()
        let instructions = activeInstructions()
        let boxes = activeBoxes() |> Map.to_seq |> Seq.map snd
        ControlInference.Plugin.generateWithMultiplexers ic instructions boxes
    )
     
    [<CommandMethod("micado_generate_multiplexer")>]
    /// prompts the user for a flow box and, if possible, generates a related multiplexer
    let micado_generate_multiplexer() = tryCommand (fun () ->
        let ic = activeInstructionChip()
        match promptSelectAnyBox "Box " with
        | None -> ()
        | Some box ->
            ControlInference.Plugin.generateMultiplexer ic box
    )
    
    end
