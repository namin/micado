#light

module BioStream.Micado.Plugin.Test.Commands

open Autodesk.AutoCAD.Runtime

open BioStream
open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

open BioStream.Micado.Plugin.Editor.Extra

[<CommandMethod("micadotest_polyline2flow")>]
/// tests converting a polyline to a flow line
let test_polyline2flow() =
    Editor.promptSelectFlowSegment "select a flow segment (try a polyline): "
 |> Option.map Debug.drawFlowSegment 
 |> ignore

[<CommandMethod("micadotest_drawArrow")>]
/// tests drawing an arrow
let test_drawArrow() =
    Editor.promptSelectFlowSegment "select a flow segment (try a polyline): "
 |> Option.map (fun flow -> Debug.drawArrow flow.Segment.StartPoint flow.Segment.EndPoint)
 |> ignore

[<CommandMethod("micadotest_collectChipEntities")>]
/// tests collecting the chip entities
let test_collectChipEntities() =
    use chipEntities = Database.collectChipEntities()
    Editor.writeLine (sprintf "Collected %d flow entities and %d control entities" 
                              chipEntities.FlowEntities.Length
                              chipEntities.ControlEntities.Length)

[<CommandMethod("micadotest_chip")>]
/// tests the chip representation                   
let test_chip() =
    use chip = Chip.FromDatabase.create()
    Editor.writeLine ( sprintf "Flow: %d segments, %d punches" 
                               (chip.FlowLayer.Segments.Length)
                               (chip.FlowLayer.Punches.Length) )
    Editor.writeLine ( sprintf "Control: %d control lines, %d unconnected control lines, %d unconnected punches, %d obstacles"
                               (chip.ControlLayer.Lines.Length)
                               (chip.ControlLayer.UnconnectedLines.Length)
                               (chip.ControlLayer.UnconnectedPunches.Length)
                               (chip.ControlLayer.Obstacles.Length) )
                               
[<CommandMethod("micadotest_grid")>]
/// test the routing grid
let test_grid() =
    use chip = Chip.FromDatabase.create()
    chip
 |> Routing.createChipGrid
 |> Debug.drawGrid
 |> ignore
 
[<CommandMethod("micadotest_entities_intersect")>]
/// prompts for two entities and prints whether they intersect
let test_entities_intersect() =
    let entity1 = Editor.promptSelectEntity "select first entity"
    let entity2 = entity1 |> Option.bind (fun entity1 -> Editor.promptSelectEntity "select second entity")
    match entity1, entity2 with
    | Some entity1, Some entity2 ->
        match Datatypes.entitiesIntersect entity1 entity2 with
        | true -> Editor.writeLine "selected entities intersect"
        | false -> Editor.writeLine "selected entities do not intersect"
    | _ -> ()

[<CommandMethod("micadotest_min_cost_flow_routing")>]
/// test the min cost flow routing algorithm
let test_min_cost_flow_routing() =
    use chip = Chip.FromDatabase.create()
    let chipGrid = chip |> Routing.createChipGrid
    chipGrid
 |> Routing.minCostFlowRouting
 |> function
    | None -> Editor.writeLine "no solution found"
    | Some connections ->
        Routing.presentConnections chipGrid connections
     |> Database.writeEntities |> ignore
 |> ignore

[<CommandMethod("micadotest_routing")>]
/// test the routing algorithms in sequence: 
/// start with the min cost flow routing algorithm
/// then interactively iterate over the iterative routing algorithm
let test_routing() =
    use chip = Chip.FromDatabase.create()
    let chipGrid = chip |> Routing.createChipGrid
    let presenter = Routing.presentConnections chipGrid 
    let rec promptIterate (iterativeSolver : Routing.IterativeRouting) currentSolution =
        match Editor.promptYesOrNo true "Iterate?" with
        | false -> Datatypes.disposeAll (currentSolution)
        | true -> Database.eraseEntities currentSolution
                  Datatypes.disposeAll (currentSolution)
                  let stable = iterativeSolver.iterate()
                  let currentEntities = 
                    iterativeSolver.Solution |> presenter |> Database.writeEntitiesAndReturn
                  match stable with
                  | false ->  currentEntities |> promptIterate iterativeSolver
                  | true -> Editor.writeLine "Reached stable routing"
    chipGrid
 |> Routing.minCostFlowRouting
 |> function
    | None -> Editor.writeLine "no solution found"
    | Some connections ->
        let entities = presenter connections |> Database.writeEntitiesAndReturn
        promptIterate (new Routing.IterativeRouting (chipGrid, connections)) entities
 |> ignore
 
[<CommandMethod("micadotest_routing_stable")>]
/// test the routing algorithms in sequence:
/// first with the min cost flow routing algorithm
/// then the iterative routing algorithm until it stabilizes
let test_routing_stable() =
    use chip = Chip.FromDatabase.create()
    let chipGrid = chip |> Routing.createChipGrid
    let presenter = Routing.presentConnections chipGrid 
    chipGrid
 |> Routing.minCostFlowRouting
 |> function
    | None -> Editor.writeLine "no solution found"
    | Some connections ->
        let iterativeSolver = new Routing.IterativeRouting (chipGrid, connections)
        let n = iterativeSolver.stabilize()
        iterativeSolver.Solution |> presenter |> Database.writeEntities |> ignore
        Editor.writeLine ("iterative routing ran for " ^ n.ToString() ^ " iterations")
 |> ignore
 
[<CommandMethod("micadotest_flow_intersections")>]
/// test detection of flow intersections
let test_flow_intersections() =
    use chip = Chip.FromDatabase.create()
    let segments = chip.FlowLayer.Segments
    let length = if segments.Length > 0 then (segments.[0].Width/2.0) else 1.0
    let points =
        seq { for i in [|0..segments.Length-1|] do
                for j in [|i..segments.Length-1|] do
                   for Some point in [segments.[i].intersectWith(segments.[j])] do
                    yield point
            }
    Seq.iter (Debug.drawPoint length) points
    
[<CommandMethod("micadotest_flow_representation")>]
/// test representation of flow
let test_flow_representation() =
    use chip = Chip.FromDatabase.create()
    let flowRep = FlowRepresentation.create chip.FlowLayer
    {0..flowRep.EdgeCount-1}
 |> Seq.map flowRep.ToFlowSegment
 |> Seq.iter Debug.drawFlowSegment

[<CommandMethod("micadotest_flow_click")>]
/// test mapping between points and edges of flow representation
let test_flow_click() =
    let point = Editor.promptPoint "Select a point near some flow: "
             |> Option.map Geometry.to2d
    match point with
    | None -> ()
    | Some point ->
        use chip = Chip.FromDatabase.create()
        let flowRep = FlowRepresentation.create chip.FlowLayer
        let edge = flowRep.ClosestEdge point
        let segment = flowRep.ToFlowSegment edge
        Editor.setColor 3 // Green
        Editor.setHighlight true
        Debug.drawFlowSegment segment
        Editor.resetColor()
        Editor.resetHighlight()
        
[<CommandMethod("micadotest_flow_representation_with_valves")>]
/// test representation of flow when valves are added
let test_flow_representation_with_valves() =
    use chip = Chip.FromDatabase.create()
    let rawFlowRep = FlowRepresentation.create chip.FlowLayer
    let flowRep = FlowRepresentation.addValves chip.ControlLayer.Valves rawFlowRep
    {0..flowRep.EdgeCount-1}
 |> Seq.map flowRep.ToFlowSegment
 |> Seq.iter Debug.drawFlowSegment;
    chip.ControlLayer.Valves
 |> Array.iteri (fun vi valve -> 
                    Debug.drawPoint (Debug.maxSegmentLength valve) (flowRep.ToPoint (rawFlowRep.NodeCount+vi)))

[<CommandMethod("micadotest_flow_representation_check_valves")>]
/// check whether each valve is well represented in the flow representation (i.e. not skipped over)
let test_flow_representation_check_valves() =
    use chip = Chip.FromDatabase.create()
    let rawFlowRep = FlowRepresentation.create chip.FlowLayer
    let flowRep = FlowRepresentation.addValves chip.ControlLayer.Valves rawFlowRep
    let getValveNode vi = rawFlowRep.NodeCount+vi
    let skipped vi =
        let valveNode = getValveNode vi
        let ok =
            flowRep.Neighbors(valveNode) //|> List.of_seq |> (fun lst -> lst.Length = 2) 
         |> Seq.for_all (fun node -> Seq.exists ((=) valveNode) (flowRep.Neighbors(node)))
        not ok
    let skippedIndices = [|0..chip.ControlLayer.Valves.Length-1|] |> Array.filter skipped
    if skippedIndices.Length = 0
    then Editor.writeLine "All valves OK!"
    else
    for vi in skippedIndices do
        let valveNode = getValveNode vi
        let valve = chip.ControlLayer.Valves.[vi]
        Debug.drawPoint (Debug.maxSegmentLength valve) (flowRep.ToPoint (rawFlowRep.NodeCount+vi))
        flowRep.NodeEdges valveNode |> Set.iter (flowRep.ToFlowSegment >> Debug.drawFlowSegment)
    Editor.writeLine (skippedIndices.Length.ToString() ^ " skipped valves highlighted")
            
      
[<CommandMethod("micadotest_prompt_flow_punch")>]
/// test prompting the user for a flow punch
let test_prompt_flow_punch() =
    use chip = Chip.FromDatabase.create()
    chip.FlowLayer.promptPunch "Select a flow punch: "
 |> Option.map (fun (i : int) -> Editor.writeLine ("You selected punch #" ^ i.ToString()))
 |> ignore

[<CommandMethod("micadotest_export_image")>]
/// test exporting a snapshot image
let test_export_image() =
    Export.GUI.promptImageFilename()
 |> Option.map Export.GUI.exportImage
 |> ignore
 
let verify_control_line_number (controlLayer : Datatypes.Control) =
    let tellUserLineIndex lineIndex =
        let userLineIndex = controlLayer.LineNumbering.[lineIndex]
        Editor.writeLine ("Selected line is #" ^ (userLineIndex+1).ToString())
    controlLayer.promptLine "Select line to verify numbering: "
 |> Option.map tellUserLineIndex
 |> ignore
 
[<CommandMethod("micadotest_verify_control_line_number")>]
/// test numbering control lines
let test_verify_control_line_number() =
    use chip = Chip.FromDatabase.create()
    verify_control_line_number chip.ControlLayer
