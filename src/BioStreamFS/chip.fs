#light

/// chip representation
module BioStream.Micado.Core.Chip

open BioStream
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core
open BioStream.Micado.Bridge

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

/// creates a flow representation from the given entities
/// only considers an entity if it's either a punch or a polyline convertible to a flow segment
/// prints a warning for each ignored entity
let createFlow =
    let rec acc segments punches (entities : Entity list)  =
        match entities with
        | [] -> Flow (segments, punches)
        | entity::entities ->
            match entity with
            | :? Punch as punch -> acc segments (punch::punches) entities
            | :? Polyline as polyline ->
                match Flow.from_polyline polyline with
                | None -> Editor.writeLine "warning: a flow polyline could not be converted to a flow segment"
                          acc segments punches entities 
                | Some segment -> acc (segment::segments) punches entities 
            | _ -> Editor.writeLine "warning: unrecognized flow entity"
                   acc segments punches entities 
    acc [] []

/// creates a control representation from the given entities
/// only considers an entity if it's either a valve, a punch, or a restricted entity
/// prints a warning for each ignored entity
let createControl =
    let rec acc valves punches others ( entities : Entity list ) =
        match entities with
        | [] -> Control (valves, punches, others)
        | entity::entities ->
            match entity with
            | :? Valve as valve -> acc (valve::valves) punches others entities 
            | :? Punch as punch -> acc valves (punch::punches) others entities 
            | :? RestrictedEntity as other -> acc valves punches (other::others) entities 
            | _ -> Editor.writeLine "warning: unrecognized control entity"
                   acc valves punches others entities 
    acc [] [] []

/// creates a chip representation from the given chip entities
let create(chipEntities : ChipEntities) =
    { FlowLayer = createFlow chipEntities.FlowEntities ; ControlLayer = createControl chipEntities.ControlEntities }