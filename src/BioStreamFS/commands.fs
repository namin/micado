#light

/// All user-end AutoCAD commands advertised by the plug-in
module BioStream.Micado.Plugin.Commands

open BioStream.Micado.Core
open BioStream.Micado.Plugin
open Autodesk.AutoCAD.Runtime

[<CommandMethod("PlacePunch")>]
let placePunch() =
    Editor.promptPoint "Pick center of punch: "
 |> Option.map (fun point3d -> Creation.punch (Geometry.to2d point3d))
 |> Option.map Database.writeEntity
 |> Option.map (fun (punch) -> Editor.writeLine ("Created punch at" ^ punch.Center.ToString() ^ "."))
 |> ignore
 
[<CommandMethod("PlaceValve")>]
let placeValve() =
    Editor.promptSelectFlowSegmentAndPoint "Select point on flow segment: "
 |> Option.map (fun (flowSegment, point3d) -> Creation.valve flowSegment (Geometry.to2d point3d))
 |> Option.map Database.writeEntity
 |> Option.map (fun (valve) -> Editor.writeLine ("Created valve at" ^ valve.Center.ToString() ^ "."))
 |> ignore
 