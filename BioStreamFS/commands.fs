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
 |> Option.map Database.writeEntity 
 |> Option.map (fun (entity) -> entity :?> Punch)
 |> Option.map (fun (punch) -> Editor.writeLine ("Created punch at" ^ punch.Center.ToString() ^ "."))
 |> ignore

/// asks the user for a flow segment and places a valve centered on the segment, 
/// as close as possible to where the user clicked
[<CommandMethod("PlaceValve")>]
let placeValve() =
    Editor.promptSelectFlowSegmentAndPoint "Select point on flow segment: "
 |> Option.map (fun (flowSegment, point3d) -> Creation.valve flowSegment (Geometry.to2d point3d))
 |> Option.map Database.writeEntity
 |> Option.map (fun (entity) -> entity :?> Valve)
 |> Option.map (fun (valve) -> Editor.writeLine ("Created valve at" ^ valve.Center.ToString() ^ "."))
 |> ignore

/// automatic routing of control layer
/// will try to connect each unconnected control line to some unconnected control punch
/// according to the user settings
[<CommandMethod("ConnectValvesToPunches")>]
let connectValvesToPunches() =
    let chip = Chip.create (Database.collectChipEntities())
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
    // micado_
    //        new_
    //            box (?)
    //            box_
    //                input
    //                output
    //                path
    //                seq
    //                and
    //                or
    //            instruction
    //        mark_
    //             box (print out kind and name)
    //             instruction (print out partial/complete and name)
    //        list_
    //             boxes
    //             instructions (?)
    //        export_
    //               to_gui
    //               boxes
    //               instructions
    //        import_
    //               boxes
    //               instructions
    end
