#light

/// Common data types
module BioStream.Micado.Common.Datatypes

open BioStream
open BioStream.Micado.Common
open BioStream.Micado.Core

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

/// implements the lazy property idiom
/// if optionValue is none, run computeValue and saving the returned value in optionValue and returning it
/// if optionValue is something, returns it
let lazyGet computeValue optionValue =
    match !optionValue with
    | Some value -> value
    | None -> 
        let value = computeValue()
        optionValue := Some value
        value
        
/// creates a hallow polyline with the given width, and starting and ending mid points
let segmentPolyline width (startPoint : Point2d) (endPoint : Point2d) =
    let polyline = new Polyline ()
    let addVertex point = polyline.AddVertexAt(polyline.NumberOfVertices, point, 0.0, 0.0, 0.0)
    if width=0.0
    then addVertex startPoint
         addVertex endPoint
    else let segment = new LineSegment2d (startPoint, endPoint)
         let normal = segment.Direction.GetPerpendicularVector()
         let s1,s2 = Geometry.midSegmentEnds width normal startPoint
         let e1,e2 = Geometry.midSegmentEnds width normal endPoint
         addVertex s1
         addVertex s2
         addVertex e2
         addVertex e1
         polyline.Closed <- true
    polyline

/// chip entities are just straight out of the database
type ChipEntities = 
    { FlowEntities : Entity list
      ControlEntities : Entity list }

type FlowSegmentAngle = Horizontal | Vertical | Tilted

/// a flow segment
type FlowSegment = 
    { Segment : LineSegment2d
      Width : double }
    member v.to_polyline extraWidth = segmentPolyline (v.Width+extraWidth) (v.Segment.StartPoint) (v.Segment.EndPoint)
    member private v.angle = ref None : FlowSegmentAngle option ref
    member private v.computeAngle() = 
        let around delta base angle =
            Geometry.angleWithin (base-delta) (base+delta) angle
        let angle = Geometry.rad2deg (v.Segment.Direction.Angle)
        let near base = (around 30 base angle) || (around 30 (base+180) angle)
        match angle with
        | _ when (near 0) -> Horizontal
        | _ when (near 90) -> Vertical
        | _ -> Tilted         
    member v.Angle = lazyGet v.computeAngle v.angle
    
/// flow layer of a chip
type Flow ( segments : FlowSegment list, punches : Punch list) =
    member v.Segments = segments
    member v.Punches = punches

/// acceptable entity type for any control entity other than valve and punch
type RestrictedEntity = Polyline

let entitiesIntersect (entity1 :> Entity) (entity2 :> Entity) =
    let points = new Point3dCollection()
    entity1.IntersectWith(entity2, Intersect.OnBothOperands, points, 0, 0)
    points.Count > 0
    
/// a control line represents a set of linked valves
/// with punches if connected,
/// others typically holds all the lines connecting the valves between them and to the punches
type ControlLine =
    { Valves : Valve list
      Punches : Punch list
      Others : RestrictedEntity list }
    member v.Connected = (v.Punches <> [])
    member v.intersectWith (entity :> Entity) =
        let intersectWithEntity (other :> Entity) = entitiesIntersect entity other
        List.exists intersectWithEntity v.Valves 
     || List.exists intersectWithEntity v.Punches 
     || List.exists intersectWithEntity v.Others
        
/// an entity intersection graph
/// has the entities as nodes
/// and an edge only between any two entities that intersect
let entityIntersectionGraph ( entities : Entity array ) =
    let entitiesIntersectByIndices i j = entitiesIntersect entities.[i] entities.[j]
    let n = entities.Length
    let table = Array2.create n n false
    for i in [0..n-1] do
        table.[i,i] <- true
        let fi = entitiesIntersectByIndices i
        for j in [i+1..n-1] do  
            let b = fi j
            table.[i,j] <- b
            table.[j,i] <- b
    Graph.create table

/// upcast an entity subtype to an entity
let to_entity (entity :> Entity) =
    entity :> Entity

// upcast an entire array of subtypes to entities
let to_entities a =
    Array.map to_entity a
    
/// control layer of a chip
type Control ( valves : Valve list, punches : Punch list, others : RestrictedEntity list ) =
    let lines = ref None : ControlLine array option ref
    let unconnectedLines = ref None : ControlLine array option ref
    let unconnectedPunches = ref None : Punch array option ref
    let obstacles = ref None : RestrictedEntity array option ref    
    member private v.computeLines() =
        let entities = Array.concat [(to_entities v.Valves);(to_entities v.Punches);(to_entities v.Others)]
        let graph = entityIntersectionGraph entities
        let components = Graph.ConnectedComponents graph
        let component2line =
            let rec acc valves punches others els  =
                match els with
                | [] -> { Valves=valves; Punches=punches; Others=others }
                | el::els -> match el with 
                                | el when el<v.Valves.Length -> 
                                    acc (v.Valves.[el]::valves) punches others els 
                                | el when el<v.Valves.Length+v.Punches.Length ->
                                    acc valves (v.Punches.[el-v.Valves.Length]::punches) others els 
                                | el ->
                                    acc valves punches (v.Others.[el-v.Valves.Length-v.Punches.Length]::others) els 
            
            acc [] [] []
        Seq.to_array (seq { for c in components do
                                let line = component2line (Set.elements c)
                                if line.Valves <> []
                                then yield line 
                           })
    member private v.computeUnconnectedLines() =
        Array.filter (fun (line : ControlLine) -> not line.Connected) v.Lines
    member private v.computeUnconnectedPunches() =
        Array.filter (fun (punch : Punch) -> 
                          not (Array.exists (fun (line : ControlLine) -> List.mem punch line.Punches) v.Lines))
                     v.Punches
    member private v.computeObstacles() =
        Array.filter (fun (other : RestrictedEntity) ->
                          not (Array.exists (fun (line : ControlLine) -> List.mem other line.Others) v.Lines))
                     v.Others
    member v.Valves = Array.of_list valves
    member v.Punches = Array.of_list punches
    member v.Others = Array.of_list others
    /// control lines
    member v.Lines with get() = lazyGet v.computeLines lines
    /// unconnected lines are control lines that do not have punches
    member v.UnconnectedLines with get() = lazyGet v.computeUnconnectedLines unconnectedLines
    /// unconnected punches are punches that do not belong to any control line
    member v.UnconnectedPunches with get() = lazyGet v.computeUnconnectedPunches unconnectedPunches
    /// obstacles are others that do not belong to any control line
    member v.Obstacles with get() = lazyGet v.computeObstacles obstacles
    /// all control lines outside the given one
    member v.otherLines (line : ControlLine ) =
        Array.filter (fun (otherLine) -> otherLine <> line) v.Lines
    /// whether the given entity intersect any control entity outside the given line
    member v.intersectOutside (line : ControlLine) (entity :> Entity) =
        let intersectWithEntity (other :> Entity) = entitiesIntersect entity other
        Array.exists (fun (otherLine : ControlLine) -> otherLine.intersectWith entity) (v.otherLines line)
     || Array.exists intersectWithEntity v.Obstacles
     || Array.exists intersectWithEntity v.UnconnectedPunches
        