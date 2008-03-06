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
 