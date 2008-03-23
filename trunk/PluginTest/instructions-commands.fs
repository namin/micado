#light

module BioStream.Micado.Plugin.Test.Instructions.Commands

open Autodesk.AutoCAD.Runtime

open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

open BioStream.Micado.Plugin.Editor.Extra

open System.Collections.Generic

let doc() =
    Database.doc()

let doc2ichip = new Dictionary<Autodesk.AutoCAD.ApplicationServices.Document, Instructions.InstructionChip>()

let doc2namedFlowBoxes = new Dictionary<Autodesk.AutoCAD.ApplicationServices.Document, Map<string, Instructions.FlowBox.FlowBox>>()

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
    
let setNamedFlowBoxes map =
    let d = doc()
    doc2namedFlowBoxes.[d] <- map

let nameNSaveFlowBox box =
    let saveName name =
        setNamedFlowBoxes (Map.add name box (getNamedFlowBoxes()))
    Editor.promptIdName "Name box: "
 |> Option.map saveName

let saveNclear = nameNSaveFlowBox >> ignore >> Editor.clearMarks

let drawNsaveNclear ic box =
    Debug.drawFlowBox ic box
    saveNclear box

let promptSelectBox message =
    let map = getNamedFlowBoxes()
    let keys = map |> Map.to_array |> Array.map fst 
    if keys.Length=0
    then Editor.writeLine "(None)"
         None
    else
    Editor.promptSelectIdName message keys
 |> Option.bind (fun (s) -> 
                    if map.ContainsKey s
                    then Some map.[s]
                    else Editor.writeLine "no such box name"
                         None)

let promptBox f =
    let ic = getIChip()
    f ic 
 |> Option.map (drawNsaveNclear ic)
 |> ignore
                                      
[<CommandMethod("micado_instruction_prompt_path_box")>]
/// prompts for a path flow box
let instruction_prompt_path_box() =
    promptBox Instructions.Interactive.promptPathBox

[<CommandMethod("micado_instruction_prompt_input_box")>]
let instruction_prompt_input_box() =
    promptBox Instructions.Interactive.promptInputBox

[<CommandMethod("micado_instruction_prompt_output_box")>]
let instruction_prompt_output_box() =
    promptBox Instructions.Interactive.promptOutputBox

[<CommandMethod("micado_instruction_prompt_seq_box")>]
let instruction_prompt_seq_box() =
    let promptBox (i : int) = promptSelectBox ("Box name #" ^ i.ToString())
    let ra = new ResizeArray<Instructions.FlowBox.FlowBox>()
    let res1 = promptBox 1
    match res1 with
    | None -> ()
    | Some _ ->
        let mutable res = res1
        let mutable counter = 1
        while Option.is_some res do
            ra.Add(Option.get res)
            counter <- counter + 1
            res <- promptBox counter
        let ic = getIChip()
        let seqBox = Instructions.Interactive.promptSeqBox ic (ra.ToArray())
        seqBox  |> Option.map (drawNsaveNclear ic) |> ignore
    
let prettyStringOfAttachmentKind = function
    | Instructions.Attachments.Complete -> "complete"
    | Instructions.Attachments.Input _ -> "input"
    | Instructions.Attachments.Output _ -> "output"
    | Instructions.Attachments.Intermediary (_,_) -> "intermediary"
    
let prettyStringOfBox flowBox =
    prettyStringOfAttachmentKind (Instructions.FlowBox.attachmentKind flowBox)
    
[<CommandMethod("micado_instruction_list_boxes")>]
/// lists all flow boxes 
let instruction_list_boxes() =
    let map = getNamedFlowBoxes()
    if Map.is_empty map
    then Editor.writeLine "(None)" 
    for kv in map do
        let name = kv.Key
        let box = kv.Value
        Editor.writeLine ((prettyStringOfBox box) ^ " " ^ name)
    ()

[<CommandMethod("micado_instruction_clear_cache")>]    
/// clear the cache of both the instruction chip and the flow boxes for the active drawing
let instruction_clear_cache() =
    let d = doc()
    doc2ichip.Remove(d) |> ignore
    doc2namedFlowBoxes.Remove(d) |> ignore
    
[<CommandMethod("micado_instruction_draw_box")>]    
/// gets and draw an instruction
let instruction_get() =
    promptSelectBox "Box name"
 |> Option.map (fun (s) -> Debug.drawFlowBox (getIChip()) s)
 |> ignore