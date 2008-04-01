#light

module BioStream.Micado.Legacy

open BioStream
open BioStream.Micado.Plugin
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core

/// whether the given valve is a legacy valve
let isLegacyValve (valve : Valve) =
    not valve.Closed
    
/// converts a legacy valve
let convertLegacyValve (valve : Valve) =
    let valve' = new Valve()
    let addVertex = addVertexTo valve'
    valve'.Closed <- true
    valve'.LayerId <- valve.LayerId
    valve'.Center <- valve.Center
    match valve.NumberOfVertices with
    | 3 -> let width = (valve.GetStartWidthAt 0) / 2.0
           let segment = valve.GetLineSegment2dAt 0
           List.iter addVertex (Geometry.rectangleCorners width segment)
           Some valve'
    | 5 -> [0..3] |> List.map valve.GetPoint2dAt |> List.iter addVertex
           Some valve'
    | _ -> Editor.writeLine "warning: could not convert legacy valve"
           None

open Autodesk.AutoCAD.Runtime

[<CommandMethod("micado_legacy_convert_valves")>]
/// converts all legacy valves in the drawing to new valves
let legacy_convert_valves() =
    let valves = Database.collect (fun dbObject -> dbObject :? Valve) |> List.map (fun dbObject -> dbObject :?> Valve) 
    let legacyValves = valves |> List.filter isLegacyValve
    let newValves = [for valve in legacyValves do
                       let Some valve' = convertLegacyValve valve
                       yield (valve, valve')]
    List.iter (fun (valve : Valve, valve' : Valve) -> Database.eraseEntity valve; Database.writeEntity valve' |> ignore) newValves
    let n = newValves.Length
    Editor.writeLine ("converted " ^ n.ToString() ^ " legacy valves")
    valves.Iterate (fun e -> e.Dispose())