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

open System
open System.Diagnostics

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
    let allInferredValves = closedPerInstruction |> Seq.fold Set.union Set.empty |> Set.to_array
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

type MultiplexerPath = (int * bool) list
type Multiplexer = MultiplexerPath array

let withinMultiplexerPath f f' =
    let maxDiff = 10
    let getAngle (f : FlowSegment) = Geometry.rad2deg f.Segment.Direction.Angle
    let d,d' = getAngle f, getAngle f'
    let diff = d-d' % 180
    Geometry.angleWithin (-maxDiff) maxDiff diff    
  
let inferMultiplexer (ic : Instructions.InstructionChip) nodes =
    if Array.length nodes <= 6
    then None
    else
    let calculatePath node =
        let rep = ic.Representation
        let edges = rep.NodeEdges node
        if edges.Count <> 1
        then None
        else
        let edge = edges.Choose
        let nodesOfEdge = FlowRepresentation.edge2nodes rep
        let otherNode (a,e) = FlowRepresentation.differentFrom a (nodesOfEdge edge)
        let reverseSeg (a,e) = a <> rep.OfPoint ((rep.ToFlowSegment e).Segment.StartPoint)
        let next (a,e) =
            let b = otherNode (a,e)
            let es = Set.remove e (rep.NodeEdges b)
            if es.Count <> 1
            then None
            else
            let e' = es.Choose
            let f,f' = rep.ToFlowSegment e, rep.ToFlowSegment e'
            if withinMultiplexerPath f f'
            then Some ((e',reverseSeg(b,e')), (b,e'))
            else None
        Some (((edge,reverseSeg(node,edge)) :: (Seq.unfold next (node, edge) |> List.of_seq)) : MultiplexerPath)
    let opaths = nodes |> Array.map calculatePath
    if Array.for_all Option.is_some opaths
    then Some ((opaths |> Array.map Option.get) : Multiplexer)
    else None

let nLinesForMultiplexer (multiplexer : Multiplexer) =
    let nPaths = multiplexer.Length
    int(Math.Ceiling(Math.Log(float(nPaths), 2.0))) * 2
    
let createMultiplexer (ic : Instructions.InstructionChip) (multiplexer : Multiplexer) =
    let relativeExtraSide = Settings.Current.ValveRelativeHeight * 2.0
    let rep = ic.Representation
    let nPaths = multiplexer.Length
    let nLines = nLinesForMultiplexer multiplexer 
    let nValvesPerPath = nLines / 2
    let grid = Array2.zero_create nPaths nLines
    let measurements = Array.zero_create nPaths
    let fillForPath pi path =
        let fs = path |> Seq.map (fun (e,r) -> rep.ToFlowSegment e, r) |> Array.of_seq
        let ls = fs |> Array.map (fun (f,r) -> f.Segment.Length)
        let totalLength = Array.fold_left (+) 0.0 ls
        let maxFlowWidth = fs |> Array.fold_left (fun w (f,r) -> max w f.Width) 0.0
        let startLength = relativeExtraSide*maxFlowWidth
        let usableLength = totalLength - startLength*2.0
        let lengthPerLine = usableLength / float(nLines)
        let lengthPerLine2 = lengthPerLine / 2.0
        measurements.[pi] <- pi,lengthPerLine, maxFlowWidth
        let rec findLength length curLength curSegIndex =
            let segLength = ls.[curSegIndex]
            let curLength' = curLength + segLength
            if curLength' < length
            then findLength length curLength' (curSegIndex+1)
            else (curLength, curSegIndex)
        let rec fill1 li fillLength prevLength prevSegIndex =
            let curLength, curSegIndex = findLength fillLength prevLength prevSegIndex
            let diffLength = fillLength - curLength
            let f, r = fs.[curSegIndex]
            let p = diffLength / f.Segment.Length
            Debug.Assert(p <= 1.0)
            let p = if r then 1.0-p else p
            grid.[pi,li] <- f.Segment.EvaluatePoint p, f
            let li' = li+1
            if li'<nLines
            then fill1 li' (fillLength + lengthPerLine) curLength curSegIndex
        fill1 0 (startLength+lengthPerLine2) 0.0 0
    Array.iteri fillForPath multiplexer
    let valveExtra = Settings.Current.ValveExtraWidth
    let enoughSpace pi =
        let pi,lengthPerLine,maxFlowWidth = measurements.[pi]
        if lengthPerLine < Settings.Current.Resolution
        then false
        else
        let maxValveWidth = Settings.Current.ValveRelativeWidth * maxFlowWidth
        let maxValveHeight = Settings.Current.ValveRelativeHeight * maxFlowWidth
        if lengthPerLine < maxValveHeight + 2.0*valveExtra
        then false
        else
        true
    if not (Seq.for_all enoughSpace {0..nPaths-1})
    then None
    else
    let lines = Array.zero_create nLines
    let fillLine li =
        let polyline = new Polyline()
        let addVertex point = polyline.AddVertexAt(polyline.NumberOfVertices, point, 0.0, Settings.Current.ConnectionWidth, Settings.Current.ConnectionWidth)
        let addIndex pi = addVertex (fst grid.[pi,li])
        Seq.iter addIndex {0..nPaths-1}
        lines.[li] <- polyline
    Seq.iter fillLine {0..nLines-1}
    let valves = Array.zero_create (nValvesPerPath*nPaths)
    let fillValvesForPath pi =
        let baseVi = pi * nValvesPerPath
        let rec fill1 subVi leftPi =
            let li = 2*subVi
            let li = if leftPi % 2 = 1 then li+1 else li
            let pt,f = grid.[pi,li]
            valves.[baseVi+subVi] <- Creation.valve f pt
            let subVi' = subVi+1
            if subVi'<nValvesPerPath
            then fill1 (subVi+1) (leftPi/2)
        fill1 0 pi
    Seq.iter fillValvesForPath {0..nPaths-1}
    Some (valves, lines)

let inferMultiplexerForBox (ic : Instructions.InstructionChip) box =
    match box with
    | Instructions.FlowBox.Or(_,boxes,_) ->
        let extractNode box =
            match box with
            | Instructions.FlowBox.Extended(_,_,Instructions.FlowBox.Primitive(a, _)) -> 
                match a.Kind with
                | Instructions.Attachments.Input node 
                | Instructions.Attachments.Output node -> Some (node : int)
                | _ -> None
            | _ -> None
        let nodes = boxes |> Array.choose extractNode
        if nodes.Length < boxes.Length
        then None
        else
        inferMultiplexer ic nodes
    | _ -> None
              
module Plugin =
    open BioStream.Micado.Plugin
    
    let currentLayerOK () =
        let isCurrentLayer = 
            let currentLayer = Database.currentLayer()
            fun layer -> currentLayer = layer                
        if not (Array.exists isCurrentLayer Settings.Current.ControlLayers)
        then Editor.writeLine "The current layer must be a control layer, for control generation. Please either change the current layer or update the settings by adding the current layer to the control layers."
             false
        else true

    let updateChipWith (ic : Instructions.InstructionChip) valves openSets =
        let valves = valves
                  |> Array.map (Database.writeEntityAndReturn >> (fun entity -> entity :?> Valve))
        let newChip = Chip.FromDatabase.create()
        try
            ic.UpdateInferred (newChip, valves, openSets)
            Editor.writeLine "Control generation succeeded."
        with :? System.Collections.Generic.KeyNotFoundException ->
            Editor.writeLine "The generation could not complete, because some old valves could not be found."
            Editor.writeLine "Undoing inferred valves..."
            valves |> Array.iter (Database.eraseEntity)
            
    let generate (ic : Instructions.InstructionChip) (instructions : Instructions.Instruction array) =
        if not (currentLayerOK())
        then ()
        else
        let allInferredValves, stateTable = calculate ic instructions
        let valves = createValves ic allInferredValves 
        let openSets = stateTable |> Array.map states2openSet
        updateChipWith ic valves openSets
                                
    let generateMultiplexer (ic : Instructions.InstructionChip) box =
        match inferMultiplexerForBox ic box with
        | None ->
            Editor.writeLine "Box isn't suited for multiplexer."
        | Some multiplexer ->
            Editor.writeLine "Box is suited for multiplexer."
            match createMultiplexer ic multiplexer with
            | None ->
                Editor.writeLine "Not enough space to create multiplexer."
            | Some (valves, polylines) ->
                Editor.writeLine "Multiplexer created."
                Database.writeEntities valves |> ignore
                Database.writeEntities polylines |> ignore
                
        