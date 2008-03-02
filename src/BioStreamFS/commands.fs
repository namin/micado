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