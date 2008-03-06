#light

#I @"C:\Program Files\Autodesk\Acade 2008"
#r "acdbmgd.dll"
#r "acmgd.dll"

#I @"..\debug\"
#r "biostreamfs.dll"

open Autodesk.AutoCAD.Runtime

open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

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
