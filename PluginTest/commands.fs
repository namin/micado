#light

module BioStream.Micado.Plugin.Test.Commands

open Autodesk.AutoCAD.Runtime

open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

open BioStream.Micado.Plugin.Editor.Extra

[<CommandMethod("micado_test_polyline2flow")>]
/// tests converting a polyline to a flow line
let test_polyline2flow() =
    Editor.promptSelectFlowSegment "select a flow segment (try a polyline): "
 |> Option.map Debug.drawFlowSegment 
 |> ignore

[<CommandMethod("micado_test_drawArrow")>]
/// tests drawing an arrow
let test_drawArrow() =
    Editor.promptSelectFlowSegment "select a flow segment (try a polyline): "
 |> Option.map (fun flow -> Debug.drawArrow flow.Segment.StartPoint flow.Segment.EndPoint)
 |> ignore

[<CommandMethod("micado_test_collectChipEntities")>]
/// tests collecting the chip entities
let test_collectChipEntities() =
    let chipEntities = Database.collectChipEntities()
    Editor.writeLine (sprintf "Collected %d flow entities and %d control entities" 
                              chipEntities.FlowEntities.Length
                              chipEntities.ControlEntities.Length)

[<CommandMethod("micado_test_chip")>]
/// tests the chip representation                   
let test_chip() =
    let chip = Chip.create (Database.collectChipEntities())
    Editor.writeLine ( sprintf "Flow: %d segments, %d punches" 
                               (chip.FlowLayer.Segments.Length)
                               (chip.FlowLayer.Punches.Length) )
    Editor.writeLine ( sprintf "Control: %d control lines, %d unconnected control lines, %d unconnected punches, %d obstacles"
                               (chip.ControlLayer.Lines.Length)
                               (chip.ControlLayer.UnconnectedLines.Length)
                               (chip.ControlLayer.UnconnectedPunches.Length)
                               (chip.ControlLayer.Obstacles.Length) )
                               
[<CommandMethod("micado_test_grid")>]
/// test the routing grid
let test_grid() =
    Chip.create (Database.collectChipEntities())
 |> Routing.createChipGrid
 |> Debug.drawGrid
 |> ignore
 
[<CommandMethod("micado_test_entities_intersect")>]
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

[<CommandMethod("micado_test_min_cost_flow_routing")>]
/// test the min cost flow routing algorithm
let test_min_cost_flow_routing() =
    let chipGrid = Chip.create (Database.collectChipEntities()) |> Routing.createChipGrid
    chipGrid
 |> Routing.minCostFlowRouting
 |> function
    | None -> Editor.writeLine "no solution found"
    | Some connections ->
        Routing.presentConnections chipGrid connections
     |> Database.writeEntities |> ignore
 |> ignore

[<CommandMethod("micado_test_routing")>]
/// test the routing algorithms in sequence: 
/// start with the min cost flow routing algorithm
/// then interactively iterate over the iterative routing algorithm
let test_routing() =
    let chipGrid = Chip.create (Database.collectChipEntities()) |> Routing.createChipGrid
    let presenter = Routing.presentConnections chipGrid 
    let rec promptIterate (iterativeSolver : Routing.IterativeRouting) currentSolution =
        match Editor.promptYesOrNo true "Iterate?" with
        | false -> ()
        | true -> Database.eraseEntities currentSolution
                  let stable = iterativeSolver.iterate()
                  let currentEntities = iterativeSolver.Solution |> presenter |> Database.writeEntities
                  match stable with
                  | false ->  currentEntities |> promptIterate iterativeSolver
                  | true -> Editor.writeLine "Reached stable routing"
    chipGrid
 |> Routing.minCostFlowRouting
 |> function
    | None -> Editor.writeLine "no solution found"
    | Some connections ->
        let entities = presenter connections |> Database.writeEntities
        promptIterate (new Routing.IterativeRouting (chipGrid, connections)) entities
 |> ignore
 
[<CommandMethod("micado_test_routing_stable")>]
/// test the routing algorithms in sequence:
/// first with the min cost flow routing algorithm
/// then the iterative routing algorithm until it stabilizes
let test_routing_stable() =
    let chipGrid = Chip.create (Database.collectChipEntities()) |> Routing.createChipGrid
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
 
[<CommandMethod("micado_test_flow_intersections")>]
/// test detection of flow intersections
let test_flow_intersections() =
    let chip = Chip.create (Database.collectChipEntities())
    let segments = chip.FlowLayer.Segments
    let length = if segments.Length > 0 then (segments.[0].Width/2.0) else 1.0
    let points =
        seq { for i in [|0..segments.Length-1|] do
                for j in [|i..segments.Length-1|] do
                   for Some point in [segments.[i].intersectWith(segments.[j])] do
                    yield point
            }
    Seq.iter (Debug.drawPoint length) points
    
[<CommandMethod("micado_test_flow_representation")>]
/// test representation of flow
let test_flow_representation() =
    let chip = Chip.create (Database.collectChipEntities())
    let flowRep = FlowRepresentation.create chip.FlowLayer
    {0..flowRep.EdgeCount-1}
 |> Seq.map flowRep.ToFlowSegment
 |> Seq.iter Debug.drawFlowSegment

[<CommandMethod("micado_test_flow_click")>]
/// test mapping between points and edges of flow representation
let test_flow_click() =
    let point = Editor.promptPoint "Select a point near some flow: "
             |> Option.map Geometry.to2d
    match point with
    | None -> ()
    | Some point ->
        let chip = Chip.create (Database.collectChipEntities())
        let flowRep = FlowRepresentation.create chip.FlowLayer
        let edge = flowRep.ClosestEdge point
        let segment = flowRep.ToFlowSegment edge
        Editor.setColor 3 // Green
        Editor.setHighlight true
        Debug.drawFlowSegment segment
        Editor.resetColor()
        Editor.resetHighlight()
        
[<CommandMethod("micado_test_flow_representation_with_valves")>]
/// test representation of flow when valves are added
let test_flow_representation_with_valves() =
    let chip = Chip.create (Database.collectChipEntities())
    let rawFlowRep = FlowRepresentation.create chip.FlowLayer
    let flowRep = FlowRepresentation.addValves chip.ControlLayer.Valves rawFlowRep
    {0..flowRep.EdgeCount-1}
 |> Seq.map flowRep.ToFlowSegment
 |> Seq.iter Debug.drawFlowSegment;
    chip.ControlLayer.Valves
 |> Array.iteri (fun vi valve -> 
                    Debug.drawPoint (Debug.maxSegmentLength valve) (flowRep.ToPoint (rawFlowRep.NodeCount+vi)))

      
[<CommandMethod("micado_test_prompt_flow_punch")>]
/// test prompting the user for a flow punch
let test_prompt_flow_punch() =
    let chip = Chip.create (Database.collectChipEntities())
    chip.FlowLayer.promptPunch "Select a flow punch: "
 |> Option.map (fun (i : int) -> Editor.writeLine ("You selected punch #" ^ i.ToString()))
 |> ignore


[<CommandMethod("micado_test_prompt_path_box")>]
/// test prompting for a path flow box (an instruction part)
let test_prompt_path_box() =
    let chip = Chip.create (Database.collectChipEntities())
    let ic = Instructions.InstructionChip chip
    let rep = ic.Representation
    Instructions.Interactive.promptPathBox ic 
 |> Option.map (function
                | Instructions.FlowBox.Primitive (a, u) ->
                    let aLength = chip.FlowLayer.Segments.[0].Width * 0.8 // so we can still see the white of valves
                    u.Edges 
                 |> Set.iter (rep.ToFlowSegment >> Debug.drawFlowSegment);
                    u.Valves
                 |> Set.iter (fun vi -> 
                                let valve = chip.ControlLayer.Valves.[vi]
                                let node = ic.OfNodeType (Instructions.ValveNode vi) 
                                Debug.drawPoint (Debug.maxSegmentLength valve) (rep.ToPoint node));
                    (match a.InputAttachment with
                     | Some node ->
                         Editor.setColor 3 // Green
                         Debug.drawPoint aLength (rep.ToPoint node)
                         Editor.resetColor()
                     | None -> 
                         Editor.writeLine ("Input attachment missing (weird)."));
                    (match a.OutputAttachment with
                     | Some node ->
                         Editor.setColor 6 // Magenta
                         Debug.drawPoint aLength (rep.ToPoint node)
                         Editor.resetColor()
                     | None -> 
                         Editor.writeLine ("Output attachment missing (weird)."));
                    Editor.writeLine ("Box drawn (input green, output magenta)")
                | _ -> 
                    Editor.writeLine("Box built, but is not primitive flow box (wierd)."))
 |> ignore

[<CommandMethod("micado_test_export_image")>]
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
 
[<CommandMethod("micado_test_number_control_lines")>]
/// test numbering control lines
let test_number_control_lines() =
    let chip = Chip.create (Database.collectChipEntities())
    if chip.ControlLayer.numberLines()
    then verify_control_line_number chip.ControlLayer

[<CommandMethod("micado_test_verify_control_line_number")>]
/// test numbering control lines
let test_verify_control_line_number() =
    let chip = Chip.create (Database.collectChipEntities())
    verify_control_line_number chip.ControlLayer
