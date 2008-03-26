#light

/// Prototype for BioStreamFs' commands related to instructions,
/// which will be the actual user interface
module BioStream.Micado.Plugin.Test.Instructions.Commands

open Autodesk.AutoCAD.Runtime
open Autodesk.AutoCAD.DatabaseServices

open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

open BioStream.Micado.Plugin.Editor.Extra

open System.Collections.Generic

let doc() =
    Database.doc()

let doc2ichip = new Dictionary<Autodesk.AutoCAD.ApplicationServices.Document, Instructions.InstructionChip>()

let doc2namedFlowBoxes = new Dictionary<Autodesk.AutoCAD.ApplicationServices.Document, Map<string, Instructions.FlowBox.FlowBox>>()

let doc2entity2instruction = new Dictionary<Autodesk.AutoCAD.ApplicationServices.Document, Dictionary<Entity, Instructions.Instruction>>()

let defaultGet d (dict : Dictionary<'a,'b>) thunk =
    if not (dict.ContainsKey d)
    then dict.[d] <- thunk()
    dict.[d]

let getIChip() =
    defaultGet (doc()) 
               doc2ichip 
               (fun () -> new Instructions.InstructionChip (Database.collectChipEntities() |> Chip.create))

let getNamedFlowBoxes() = 
    defaultGet (doc()) doc2namedFlowBoxes (fun () -> Map.empty)

let getEntity2Instruction() =
    defaultGet (doc())
               doc2entity2instruction
               (fun () -> new Dictionary<Entity, Instructions.Instruction>())
                   
let setNamedFlowBoxes map =
    let d = doc()
    doc2namedFlowBoxes.[d] <- map

let nameNSaveFlowBox box =
    let saveName name =
        setNamedFlowBoxes (Map.add name box (getNamedFlowBoxes()))
    Editor.promptIdNameNotEmpty "Name box: "
 |> Option.map saveName

let saveNclear = nameNSaveFlowBox >> ignore >> Editor.clearMarks

let drawNsaveNclear ic box =
    Debug.drawFlowBox ic box
    saveNclear box

let prettyStringOfAttachmentKind = function
    | Instructions.Attachments.Complete -> "complete"
    | Instructions.Attachments.Input _ -> "input"
    | Instructions.Attachments.Output _ -> "output"
    | Instructions.Attachments.Intermediary (_,_) -> "intermediary"
    
let prettyStringOfBox flowBox =
    prettyStringOfAttachmentKind (Instructions.FlowBox.attachmentKind flowBox)

let getNamedFlowBoxesList() =
    let map = getNamedFlowBoxes()
    let lst =
        [for kv in map do
            let name = kv.Key
            let box = kv.Value
            yield ((prettyStringOfBox box) ^ " " ^ name), name]
    List.sort compare lst

let promptSelectBoxName message =
    let map = getNamedFlowBoxes()
    let lst = getNamedFlowBoxesList()
    let keys = lst |> List.map snd
    if keys.Length=0
    then Editor.writeLine "(None)"
         None
    else
    Editor.promptSelectIdName message keys
 |> Option.bind (fun (s) -> 
                    if map.ContainsKey s
                    then Some (map.[s], s)
                    else Editor.writeLine "no such box name"
                         None)
    
let promptSelectBox message =
    promptSelectBoxName message
 |> Option.map fst

let promptBox f =
    let ic = getIChip()
    f ic 
 |> Option.map (drawNsaveNclear ic)
 |> ignore
                                      
[<CommandMethod("micado_debug_instruction_prompt_path_box")>]
/// prompts for a path flow box
let instruction_prompt_path_box() =
    promptBox Instructions.Interactive.promptPathBox

[<CommandMethod("micado_debug_instruction_prompt_input_box")>]
let instruction_prompt_input_box() =
    promptBox Instructions.Interactive.promptInputBox

[<CommandMethod("micado_debug_instruction_prompt_output_box")>]
let instruction_prompt_output_box() =
    promptBox Instructions.Interactive.promptOutputBox

let promptSelectBoxes() =
    let promptBox (i : int) = promptSelectBox ("Box #" ^ i.ToString())
    let ra = new ResizeArray<Instructions.FlowBox.FlowBox>()
    let res1 = promptBox 1
    match res1 with
    | None -> None
    | Some _ ->
        let mutable res = res1
        let mutable counter = 1
        while Option.is_some res do
            ra.Add(Option.get res)
            counter <- counter + 1
            res <- promptBox counter
        Some (ra.ToArray())

let promptCombinationBox f =
    promptSelectBoxes()
 |> Option.map (fun boxes ->
        let ic = getIChip()
        let box = f ic boxes
        box  |> Option.map (drawNsaveNclear ic) |> ignore)
 |> ignore
                     
[<CommandMethod("micado_debug_instruction_prompt_seq_box")>]
let instruction_prompt_seq_box() =
    promptCombinationBox Instructions.Interactive.promptSeqBox

[<CommandMethod("micado_debug_instruction_prompt_and_box")>]
let instruction_prompt_and_box() =
    promptCombinationBox Instructions.Interactive.promptAndBox

[<CommandMethod("micado_debug_instruction_prompt_or_box")>]
let instruction_prompt_or_box() =
    promptCombinationBox Instructions.Interactive.promptOrBox
                            
[<CommandMethod("micado_debug_instruction_list_boxes")>]
/// lists all flow boxes 
let instruction_list_boxes() =
    let lst = getNamedFlowBoxesList()
    if lst = []
    then Editor.writeLine "(None)"
    else 
    lst
 |> List.iter (fst >> Editor.writeLine)
    

[<CommandMethod("micado_debug_instruction_clear_cache")>]    
/// clear the cache of both the instruction chip and the flow boxes for the active drawing
let instruction_clear_cache() =
    let d = doc()
    doc2ichip.Remove(d) |> ignore
    doc2namedFlowBoxes.Remove(d) |> ignore
    doc2entity2instruction.Remove(d) |> ignore
    
[<CommandMethod("micado_debug_instruction_draw_box")>]    
/// gets and draw a flow box
let instruction_draw_box() =
    promptSelectBox "Box "
 |> Option.map (fun (s) -> Debug.drawFlowBox (getIChip()) s)
 |> ignore
 
[<CommandMethod("micado_debug_instruction_build")>]    
/// build instructions from a flow box
let instruction_build() =
    let ic = getIChip()
    match promptSelectBoxName "Box " with
    | None -> ()
    | Some (box, name) ->
        Debug.drawFlowBox ic box
        let instructions = Instructions.Interactive.promptInstructions ic name box
        Editor.clearMarks()
        match instructions with
        | None -> ()
        | Some instructions -> 
            let entity2instruction = getEntity2Instruction()
            for instruction in instructions do
                entity2instruction.Add(instruction.Entity,instruction)

[<CommandMethod("micado_debug_instruction_see")>]    
/// see an instruction
let instruction_see() =
    let seeInstruction entity =
        let entity2instruction = getEntity2Instruction()
        if not (entity2instruction.ContainsKey entity)
        then Editor.writeLine "The selected entity is not associated with the extents of any instruction."
        else
        let instruction = entity2instruction.[entity]
        Editor.writeLine ("instruction " ^ instruction.Name)
        Debug.drawExtents (instruction.Extents)
        Debug.drawUsed (getIChip()) (instruction.Used)
    Editor.promptSelectEntity "Select the entity associated with the extents of the instruction: "
 |> Option.map seeInstruction
 |> ignore
 
[<CommandMethod("micado_debug_instruction_export_gui")>]
/// test exporting a java gui
let instruction_export_gui() =
    let ic = getIChip()
    let entity2instruction = getEntity2Instruction()
    let instructions = entity2instruction.Values |> Array.of_seq
    Export.GUI.prompt ic instructions |> ignore
    
