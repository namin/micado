#light

module BioStream.Micado.Core.ControlInference

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream
open BioStream.Micado.User

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

type inferredValve = { Edge : int; Node : int }

type valveState = Open | Closed | Don'tCare

let calculate (ic : Instructions.InstructionChip) (instructions : Instructions.Instruction array) =
    let nodesOfEdge = FlowRepresentation.edge2nodes ic.Representation
    let inferClosed (instruction : Instructions.Instruction) =
        let edges = instruction.Used.Edges
        let explore node =
            ic.Representation.NodeEdges(node) 
         |> Set.filter (fun edge -> not (edges.Contains edge))
         |> Set.map (fun edge -> {Edge=edge; Node=node})
        edges 
     |> Seq.map_concat (fun edge -> let a,b = nodesOfEdge edge in Set.union (explore a) (explore b))
     |> Set.of_seq
    let closedPerInstruction = instructions |> Array.map inferClosed
    let allInferredValves = closedPerInstruction |> Seq.concat |> Array.of_seq
    let calculateStates i (instruction : Instructions.Instruction) =
        let edges = instruction.Used.Edges
        let closed = closedPerInstruction.[i]
        let calculateState (iv : inferredValve) =
            if closed.Contains iv
            then Closed
            else
            if edges.Contains iv.Edge
            then Open
            else Don'tCare
        Array.map calculateState allInferredValves
    let stateTable = instructions |> Array.mapi calculateStates
    (allInferredValves, stateTable)

let states2openSet (states : valveState array) =
    states
 |> Seq.mapi (fun i s -> i,s)
 |> Seq.filter (fun (i,s) -> s = Open)
 |> Seq.map (fun (i,s) -> i)
 |> Set.of_seq
  
let createValve (ic : Instructions.InstructionChip) (iv : inferredValve) =
    let flowSegment = ic.Representation.ToFlowSegment iv.Edge
    let nodePoint = ic.Representation.ToPoint iv.Node
    let dir = 
        let seg = flowSegment.Segment
        if seg.StartPoint = nodePoint then seg.Direction else seg.Direction.Negate()
    let clickedPoint = nodePoint+dir.MultiplyBy(0.7*Settings.Current.ValveRelativeHeight)
    Creation.valve flowSegment clickedPoint
        
let createValves (ic : Instructions.InstructionChip) (ivs : inferredValve array) =
    ivs |> Array.map (createValve ic)
            
module Plugin =
    open BioStream.Micado.Plugin
    
    let generate (ic : Instructions.InstructionChip) (instructions : Instructions.Instruction array) =
        let isCurrentLayer = 
            let currentLayer = Database.currentLayer()
            fun layer -> currentLayer = layer                
        if not (Array.exists isCurrentLayer Settings.Current.ControlLayers)
        then Editor.writeLine "The current layer must be a control layer, for control generation. Please either change the current layer or update the settings by adding the current layer to the control layers."
        else
        let allInferredValves, stateTable = calculate ic instructions
        let valves = createValves ic allInferredValves 
                  |> Array.map (Database.writeEntityAndReturn >> (fun entity -> entity :?> Valve))
        let newChip = Chip.FromDatabase.create()
        let openSets = stateTable |> Array.map states2openSet
        try
            ic.UpdateInferred (newChip, valves, openSets)
            Editor.writeLine "Control generation succeeded."
        with Not_found ->
            Editor.writeLine "The generation could not complete, because some old valves could not be found."
            Editor.writeLine "Undoing inferred valves..."
            valves |> Array.iter (Database.eraseEntity)