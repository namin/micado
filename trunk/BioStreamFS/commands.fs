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
    
    [<CommandMethod("micado_number_control_lines")>]    
    /// prompts the user to number the control lines by selecting them in turn
    let micado_number_control_lines() =
        use chip = Chip.FromDatabase.create()
        chip.ControlLayer.numberLines()
    
    let doc() =
        Database.doc()

    let computeInstructionChip() =
        new InstructionChip (Chip.FromDatabase.create())
                
    type CacheEntry() =
        let instructionChip = computeInstructionChip()
        let flowAnnotations = Store.fromDatabase instructionChip
        let mutable boxes = Map.empty : Map<string, FlowBox.FlowBox>
        let rec findBox name =
            match boxes.TryFind name with
            | Some box -> box
            | None ->
                let ann = Map.find name flowAnnotations
                let box = Store.flowAnnotationToBox instructionChip findBox ann
                boxes <- Map.add name box boxes
                box            
        let cleanup() =
            (instructionChip :> System.IDisposable).Dispose()
        member v.InstructionChip = instructionChip
        member v.FlowAnnotations = flowAnnotations
        member v.Box name = findBox name
        member v.Boxes with get() = flowAnnotations |> Map.mapi (fun name ann -> v.Box name)
        member v.GetInstruction entity =
            match Store.readInstructionReferenceInEntity entity with
            | None -> Editor.writeLine "No instruction associated with entity."; None
            | Some (key,i) ->
                let entities = Store.readInstructionSetInDatabase key |> Option.get
                let instructions = Store.flowBox2instructions key (v.Box key) entities |> Array.of_seq
                Some instructions.[i]
        member v.Instructions with get() = Store.readAllInstructionSetsInDatabase() |> Map.to_seq |> Seq.map_concat (fun (name,entities) -> Store.flowBox2instructions name (v.Box name) entities) |> Array.of_seq                  
        interface System.IDisposable with
            member v.Dispose() = cleanup()
                                        
    let tryCommand command =
        try
            use ce = new CacheEntry()
            try
                command(ce)
            with
                | NoPathFound _ | :? KeyNotFoundException | :? System.ArgumentException ->
                    // the store is out of date with the drawing
                    if Editor.promptYesOrNo false "Command aborted because the flow annotations are out of date with the drawing.\nWould you like to delete out-of-date flow annotations?"
                    then Store.purgeFlowAnnotations (ce.InstructionChip)
        with
            | Failure(msg) ->
                // if there is no flow segments, the creation of the flow representation might fail
                // thus aborting the creation of the instruction chip          
                Editor.writeLine msg
                
    let markSaveClear ic (store,box) =
        let save name = Store.toDatabase name store            
        Debug.drawFlowBox ic box
        Editor.promptIdNameNotEmpty "Name box: " |> Option.map save |> ignore
        Editor.clearMarks()
        
    let promptNewBox ic makeBox =
        makeBox ic
     |> Option.map (markSaveClear ic)
     |> ignore

    [<CommandMethod("micado_new_box_input")>]    
    /// prompts the user to create a new input box
    let micado_new_box_input() = tryCommand (fun (ce) ->
        promptNewBox (ce.InstructionChip) Interactive.promptInputBox)

    [<CommandMethod("micado_new_box_output")>]    
    /// prompts the user to create a new output box
    let micado_new_box_output() = tryCommand (fun (ce) ->
        promptNewBox (ce.InstructionChip) Interactive.promptOutputBox)
    
    [<CommandMethod("micado_new_box_or_input")>]    
    /// prompts the user to create a new or box made of input boxes
    let micado_new_box_or_input() = tryCommand (fun (ce) ->
        promptNewBox (ce.InstructionChip) Interactive.promptOrInputBox)

    [<CommandMethod("micado_new_box_or_output")>]    
    /// prompts the user to create a new or box made of output boxes
    let micado_new_box_or_output() = tryCommand (fun (ce) ->
        promptNewBox (ce.InstructionChip) Interactive.promptOrOutputBox)

    [<CommandMethod("micado_new_box_path")>]    
    /// prompts the user to create a new path box        
    let micado_new_box_path() = tryCommand (fun (ce) ->
        promptNewBox (ce.InstructionChip) Interactive.promptPathBox)
                         
    let displayAttachmentKind = function
        | Instructions.Attachments.Complete -> "complete"
        | Instructions.Attachments.Input _ -> "input"
        | Instructions.Attachments.Output _ -> "output"
        | Instructions.Attachments.Intermediary (_,_) -> "intermediary"
    
    let displayName name kind = (displayAttachmentKind kind) ^ " " ^ name
    
    let boxDisplayName name box = displayName name (FlowBox.attachmentKind box)
    
    let annDisplayName name entry = displayName name (Store.flowAnnotationKind entry)
        
    let flowAnnProperties (ce : CacheEntry) =
        let entries =
            [for kv in ce.FlowAnnotations do
                let name = kv.Key
                let entry = kv.Value
                let kind = Store.flowAnnotationKind entry
                yield name, annDisplayName name entry, kind, entry]
        List.sort compare entries
    
    let annPropDisplayName (_,x,_,_) = x
    let annPropName (x,_,_,_) = x
    let annPropKind (_,_,x,_) = x
    let annPropAnn (_,_,_,x) = x
    
    let allGood x = true
    
    let promptSelectAnnAndName (ce : CacheEntry) kindFilter boxFilter message =
        let map = ce.FlowAnnotations
        let keys = flowAnnProperties ce 
                |> List.filter (annPropKind >> kindFilter)
                |> List.filter (annPropAnn >> boxFilter)
                |> List.map annPropName
        if keys.Length=0
        then Editor.writeLine "(None)"
             None
        else 
        Editor.promptSelectIdName message keys
     |> Option.bind (fun (s) ->
                        match map.TryFind(s) with
                        | Some res -> Some (res, s)
                        | None -> Editor.writeLine "No such flow annotation."
                                  None)
    
    let promptSelectAnyAnnAndName ce = promptSelectAnnAndName ce allGood allGood
            
    [<CommandMethod("micado_mark_box")>]    
    /// prompts the user to select an annotation, marking it on the drawing
    let micado_mark_box() = tryCommand (fun (ce) ->
        match promptSelectAnyAnnAndName ce "Annotation " with
        | None -> ()
        | Some (ann, name) ->
            Debug.drawFlowBox (ce.InstructionChip) (ce.Box name)
            Editor.writeLine (annDisplayName name ann))
 
    [<CommandMethod("micado_list_boxes")>]
    /// prints out the list of boxes for active drawing
    let micado_list_boxes() = tryCommand (fun (ce) ->
        flowAnnProperties ce
     |> List.iter (fun prop -> Editor.writeLine (annPropDisplayName prop)))

    let selectBoxesMessage (i : int) = "Box #" ^ i.ToString()
    
    let arrayOfRevList = Routing.arrayOfRevList
    
    let promptSelectAnnsOfSameKind ce =
        let message = selectBoxesMessage
        let rec acc kindFilter anns names i =
            let annFilter ann = List.mem ann anns |> not
            match promptSelectAnnAndName ce kindFilter annFilter (message i) with
            | None -> Some ((anns |> arrayOfRevList), (names |> arrayOfRevList))
            | Some (ann,name) -> acc kindFilter (ann::anns) (name::names) (i+1)
        match promptSelectAnyAnnAndName ce (message 1) with
        | None -> None
        | Some (ann,name) ->
            acc (Attachments.sameKind (Store.flowAnnotationKind ann)) [ann] [name] 2
    
    let promptSelectAnnsForSeq ce =
        let message = selectBoxesMessage
        let hasInputAttachment kind =
            match kind with
            | Attachments.Complete | Attachments.Input _ -> false
            | _ -> true
        let hasOutputAttachment kind =
            match kind with
            | Attachments.Complete | Attachments.Output _ -> false
            | _ -> true
        let rec acc anns names i =
            let result anns names = Some ((anns |> arrayOfRevList), (names |> arrayOfRevList)) 
            match promptSelectAnnAndName ce hasInputAttachment allGood (message i) with
            | None -> result anns names
            | Some (ann,name) -> 
                let anns' = ann :: anns
                let names' = name :: names
                if ann |> Store.flowAnnotationKind |> hasOutputAttachment |> not
                then Editor.writeLine "(Stopping at Output)"
                     result anns' names'
                else acc anns' names' (i+1)
        match promptSelectAnnAndName ce hasOutputAttachment allGood (message 1) with
        | None -> None
        | Some (ann,name) ->
            acc [ann] [name] 2
            
    let promptNewCombinationBox (ce : CacheEntry) selectAnns makeBox =
        match selectAnns ce with
        | None -> ()
        | Some (anns,names) ->
            let ic = ce.InstructionChip
            let boxes = Array.map ce.Box names
            match makeBox ic boxes with
            | None -> ()
            | Some box ->
                let dispatcher =
                    match box with
                    | FlowBox.Or (a,c,o) -> Store.orDispatcher names o
                    | FlowBox.And (a,c) -> Store.andDispatcher names
                    | FlowBox.Seq (a,c) -> Store.seqDispatcher names
                    | FlowBox.Primitive _ | FlowBox.Extended _ -> failwith "Not a combination box"
                let ann = Store.attachmentsDispatch ic dispatcher (FlowBox.attachment box)
                markSaveClear ic (ann,box)

    [<CommandMethod("micado_new_box_or")>]    
    /// prompts the user to create a new or box
    let micado_new_box_or() = tryCommand (fun (ce) ->
        promptNewCombinationBox ce promptSelectAnnsOfSameKind Interactive.promptOrBox)
        
    [<CommandMethod("micado_new_box_and")>]    
    /// prompts the user to create a new and box
    let micado_new_box_and() = tryCommand (fun (ce) ->
        promptNewCombinationBox ce promptSelectAnnsOfSameKind Interactive.promptAndBox)

    [<CommandMethod("micado_new_box_seq")>]    
    /// prompts the user to create a new seq box
    let micado_new_box_seq() = tryCommand (fun (ce) ->
        promptNewCombinationBox ce promptSelectAnnsForSeq Interactive.promptSeqBox)
            
    [<CommandMethod("micado_new_instruction_set")>]
    /// prompts the user to select a box and builds an instruction set out of it
    let micado_new_instruction_set() = tryCommand (fun (ce) ->
        match promptSelectAnyAnnAndName ce "Box " with
        | None -> ()
        | Some (ann, name) ->
            let ic = ce.InstructionChip
            let box = ce.Box name
            Debug.drawFlowBox ic box
            let instructions = Instructions.Interactive.promptInstructions ic name box
            Editor.clearMarks()
            match instructions with
            | None -> ()
            | Some instructions -> Store.writeInstructionSetToDatabase name instructions
    )
    
    let markInstruction (ce : CacheEntry) (instruction : Instruction) =
        Editor.writeLine ((if instruction.Partial then "partial" else "complete")
                      ^ " instruction " ^ instruction.Name)
        instruction.Extents |> Option.map Debug.drawExtents |> ignore
        Debug.drawUsed ce.InstructionChip instruction.Used    
                
    [<CommandMethod("micado_mark_instruction")>]
    /// prompts the user to select an entity associated with an instruction and marks the instruction on the drawing
    let micado_mark_instruction() = tryCommand (fun (ce) ->
        Editor.promptSelectEntity "Select an entity associated with the extents of an instruction: "
     |> Option.bind ce.GetInstruction
     |> Option.map (markInstruction ce)
     |> ignore)

    [<CommandMethod("micado_list_instructions")>]
    /// mark the instructions one after the other
    let micado_list_instructions() = tryCommand (fun (ce) ->
        Editor.clearMarks()
        let instructions = ce.Instructions
        if Seq.is_empty instructions
        then Editor.writeLine "(None)"
        else
        for instruction in instructions do
            markInstruction ce instruction)
            
    [<CommandMethod("micado_export_to_gui")>]
    /// export files for the java GUI
    let micado_export_to_gui() = tryCommand (fun (ce) ->
        let ic = ce.InstructionChip
        let instructions = ce.Instructions
        Export.GUI.prompt ic instructions |> ignore
    )
            
    // micado_
    //        new_
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
    //        export_to_gui (v)
    //        number_control_lines (v)
    
    [<CommandMethod("micado_generate_control")>]
    /// generate the control lines from the instructions
    let micado_generate_control() = tryCommand (fun (ce) ->
        let ic = ce.InstructionChip
        let instructions = ce.Instructions
        ControlInference.Plugin.generate ic instructions
    )

    [<CommandMethod("micado_generate_multiplexed_control")>]
    /// generate the control lines with multiplexers from the instructions and boxes
    let micado_generate_multiplexed_control() = tryCommand (fun (ce) ->
        let ic = ce.InstructionChip
        let instructions = ce.Instructions
        let boxes =  ce.Boxes |> Map.to_seq |> Seq.map snd
        ControlInference.Plugin.generateWithMultiplexers ic instructions boxes
    )
     
    [<CommandMethod("micado_generate_multiplexer")>]
    /// prompts the user for a flow box and, if possible, generates a related multiplexer
    let micado_generate_multiplexer() = tryCommand (fun (ce) ->
        let ic = ce.InstructionChip
        match promptSelectAnyAnnAndName ce "Annotation " with
        | None -> ()
        | Some (ann,name) ->
            let box = ce.Box name
            ControlInference.Plugin.generateMultiplexer ic box
    )
    
    end
