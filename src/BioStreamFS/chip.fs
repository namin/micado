#light

/// chip representation
module BioStream.Micado.Core.Chip

open BioStream
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core
open BioStream.Micado.Bridge

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

let createFlow ( entities : Entity list ) =
    let rec acc (entities : Entity list) segments punches =
        match entities with
        | [] -> Flow (segments, punches)
        | entity::entities ->
            match entity with
            | :? Punch as punch -> acc entities segments (punch::punches)
            | :? Polyline as polyline ->
                match Flow.from_polyline polyline with
                | None -> Editor.writeLine "warning: a flow polyline could not be converted to a flow segment"
                          acc entities segments punches
                | Some segment -> acc entities (segment::segments) punches
            | _ -> Editor.writeLine "warning: unrecognized flow entity"
                   acc entities segments punches
    acc entities [] []

let createControl ( entities : Entity list ) =
    let rec acc ( entities : Entity list ) valves punches others =
        match entities with
        | [] -> Control (valves, punches, others)
        | entity::entities ->
            match entity with
            | :? Valve as valve -> acc entities (valve::valves) punches others
            | :? Punch as punch -> acc entities valves (punch::punches) others
            | :? RestrictedEntity as other -> acc entities valves punches (other::others)
            | _ -> Editor.writeLine "warning: unrecognized control entity"
                   acc entities valves punches others
    acc entities [] [] []
    
let create(chipEntities : ChipEntities) =
    { flow = createFlow chipEntities.FlowEntities ; control = createControl chipEntities.ControlEntities }