#light

module BioStream.Micado.Core.Instructions

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream.Micado.User
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

open System.Diagnostics

open System.Collections.Generic

type SegmentIndex = int
type NodeIndex = int
type Ordering = HorizontalOrdering | VerticalOrdering

type Used = 
    {Edges : Set<int>; Valves : Set<int> }
    member u1.append (u2 : Used) = 
        {Edges = Set.union u1.Edges u2.Edges;
         Valves = Set.union u1.Valves u2.Valves}
let used edges valves = {Edges = edges; Valves = valves}
let usedEmpty = used Set.empty Set.empty

module FlowBox = begin
    type Attachments = {InputAttachment : NodeIndex option; OutputAttachment : NodeIndex option }
    let attachments inputAttachment outputAttachment = {InputAttachment = inputAttachment; OutputAttachment = outputAttachment}
        
    type FlowBox =
        Primitive of Attachments * Used
      | Extended of Attachments * Used * FlowBox
      | Or of Attachments * FlowBox array * Ordering
      | And of Attachments * FlowBox array
      | Seq of Attachments * FlowBox array
      
    let (|Complete|Input|Output|Intermediary|) flowBox =
        match flowBox with
        | Primitive (a, _)
        | Extended (a, _, _)
        | Or (a, _, _)
        | And (a, _)
        | Seq (a, _) ->
            match a with
            | {InputAttachment = None; OutputAttachment = None} -> Complete
            | {InputAttachment = None; OutputAttachment = Some output} -> Input (output)
            | {InputAttachment = Some input; OutputAttachment = None} -> Output (input)
            | {InputAttachment = Some input; OutputAttachment = Some output} -> Intermediary (input, output)
end

module InstructionBox = begin 
    type InstructionBox =
        Single of Used
      | Multi of Ordering * InstructionBox array

    let rec appendUsed u instructionBox =
        match instructionBox with
        | Single u' -> 
            Single (u'.append u)
        | Multi (o,bs) -> 
            Multi (o, bs |> Array.map (appendUsed u))

    let rec wrapAround outer inner =
        match outer, inner with
        | Single u, _ ->
            appendUsed u inner
        | _, Single u ->
            appendUsed u outer
        | Multi (out_o, out_bs), _ ->
            let bs = Array.map (fun outer -> wrapAround outer inner) out_bs
            Multi (out_o, bs)
                
    let rec of_flowBox flowBox =
        match flowBox with
        | FlowBox.Primitive (_, u) -> 
            Single u
        | FlowBox.Extended (_, u, f) ->
            appendUsed u (of_flowBox flowBox)
        | FlowBox.Or (_, fs, o) ->
            Multi (o, (fs |> Array.map of_flowBox))
        | FlowBox.And (_, fs) | FlowBox.Seq (_, fs) ->
            fs |> Array.map of_flowBox |> Array.fold1_right wrapAround
end

type NodeType = 
    | ValveNode of int
    | PunchNode of int
    | IntersectionNode of int
    
type InstructionChip (chip : Chip) =
    let rep = 
        FlowRepresentation.create chip.FlowLayer
     |> FlowRepresentation.addValves chip.ControlLayer.Valves
    let punches = chip.FlowLayer.Punches
    let valves = chip.ControlLayer.Valves
    let nPunches = punches.Length
    let nValves = valves.Length
    let valveStartingNodeIndex = rep.NodeCount - nValves
    let node2type node =
        if node < nPunches
        then PunchNode node
        else
        if node >= valveStartingNodeIndex
        then ValveNode (node - valveStartingNodeIndex)
        else
        IntersectionNode (node - nPunches)
    let punch2node pi = pi
    let valve2node vi = vi + valveStartingNodeIndex
    let intersection2node ii = ii + nPunches
    let type2node nt =
        match nt with
        | ValveNode vi -> valve2node vi
        | PunchNode pi -> punch2node pi
        | IntersectionNode ii -> intersection2node ii
    member v.Chip = chip
    member v.Representation = rep
    member v.ToNodeType node = node2type node
    member v.OfNodeType nt = type2node nt

module Build =

    let addIfValve (ic : InstructionChip) node set =
        match ic.ToNodeType node with
        | ValveNode vi -> Set.add vi set
        | _ -> set
        
    let inputBox (ic : InstructionChip) pi =
        let punchNode = ic.OfNodeType (PunchNode pi)
        FlowBox.Primitive ({InputAttachment = None; OutputAttachment = Some punchNode}, usedEmpty)
    
    let outputBox (ic : InstructionChip) pi =
        let punchNode = ic.OfNodeType (PunchNode pi)
        FlowBox.Primitive ({InputAttachment = Some punchNode; OutputAttachment = None}, usedEmpty)
    
    let pathBox (ic : InstructionChip) (inputClick, inputEdge) (outputClick, outputEdge) =
        if inputEdge=outputEdge
        then let edge = inputEdge
             let f = ic.Representation.ToFlowSegment edge
             let s,t = f.Segment.StartPoint, f.Segment.EndPoint
             let ds,dt = s.GetDistanceTo(inputClick), t.GetDistanceTo(outputClick)
             let inputPoint,outputPoint = if ds<dt then s,t else t,s
             let inputNode,outputNode = ic.Representation.OfPoint inputPoint, ic.Representation.OfPoint outputPoint
             let edges = Set.singleton edge
             let valves = Set.empty |> addIfValve ic inputNode |> addIfValve ic outputNode
             FlowBox.Primitive ({InputAttachment = Some inputNode; OutputAttachment = Some outputNode}, 
                                used edges valves)
        else
        // todo
        FlowBox.Primitive ({InputAttachment = None; OutputAttachment = None}, usedEmpty)     
    
module Interactive =

    open BioStream.Micado.Plugin
    open BioStream.Micado.Plugin.Editor.Extra

    let promptEdge (flowRep : FlowRepresentation.IFlowRepresentation) message =
        Editor.promptPoint message
     |> Option.map (Geometry.to2d >> (fun (point) -> point, flowRep.ClosestEdge point))
        
    type BioStream.Micado.Core.FlowRepresentation.IFlowRepresentation with
        member v.promptEdge message = promptEdge v message
    
    let promptInputBox (ic : InstructionChip) =
        ic.Chip.FlowLayer.promptPunch "Select an input flow punch: "
     |> Option.map (Build.inputBox ic)

    let promptOutputBox (ic : InstructionChip) =
        ic.Chip.FlowLayer.promptPunch "Select an output flow punch: "
     |> Option.map (Build.outputBox ic)
        
    let promptPathBox (ic : InstructionChip) =
        let promptInputEdge() = ic.Representation.promptEdge "Select an input point on the flow: "
        let promptOutputEdge() = ic.Representation.promptEdge "Select an outpoint point on the flow: "
        match promptInputEdge() with
        | Some inputEdge ->
            match promptOutputEdge() with
            | Some outputEdge -> Some (Build.pathBox ic inputEdge outputEdge)
            | None -> None
        | None -> None



