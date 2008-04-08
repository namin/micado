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

let deconstructExtents (e : Extents3d) =
    let min2d = Geometry.to2d e.MinPoint
    let max2d = Geometry.to2d e.MaxPoint
    (min2d.X, min2d.Y, max2d.X, max2d.Y)

let findBoundingBox (entities : Entity list) =
    List.map (fun (entity : Entity) -> entity.GeometricExtents |> deconstructExtents) entities
 |> function
    | [] -> (0.0, 0.0, 0.0, 0.0)
    | lst -> lst |> List.fold1_left (fun (minX',minY',maxX',maxY') (minX,minY,maxX,maxY) -> (min minX minX', min minY minY', max maxX maxX', max maxY maxY'))
 |> fun (minX, minY, maxX, maxY) -> (new Point2d(minX, minY), new Point2d(maxX, maxY))

/// representation of a multi-layer soft litography chip in terms of a flow layer and a control layer
type Chip (chipEntities : ChipEntities) =
    let mutable disposed = false
    let flowLayer = createFlow chipEntities.FlowEntities
    let controlLayer = createControl chipEntities.ControlEntities
    let boundingBox = ref None : (Point2d * Point2d) option ref
    let computeBoundingBox() = 
        findBoundingBox (List.append chipEntities.FlowEntities chipEntities.ControlEntities)
    let cleanup() =
        if not disposed then 
            disposed <- true;
            (chipEntities :> System.IDisposable).Dispose();            
    member v.FlowLayer = flowLayer
    member v.ControlLayer = controlLayer
    member v.BoundingBox with get() = lazyGet computeBoundingBox boundingBox
    interface System.IDisposable with
        member x.Dispose() = cleanup() 
    
/// creates a chip representation from the given chip entities
let create(chipEntities : ChipEntities) = new Chip(chipEntities)

module FromDatabase =
    open BioStream.Micado.Plugin
    let create() = create (Database.collectChipEntities())