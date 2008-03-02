#light

/// Common data types
module BioStream.Micado.Common.Datatypes

open BioStream
open BioStream.Micado.Common

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

/// chip entities are just straight out of the database
type ChipEntities = 
    { FlowEntities : Entity list
      ControlEntities : Entity list }

/// a flow segment
type FlowSegment = 
    { Segment : LineSegment2d
      Width : double }

/// flow layer of a chip
type Flow ( segments : FlowSegment list, punches : Punch list) =
    member v.Segments = segments
    member v.Punches = punches

/// acceptable entity type for any control entity other than valve and punch
type RestrictedEntity = Curve

/// a control line represents a set of linked valves
/// with punches if connected,
/// others typically holds all the lines connecting the valves between them and to the punches
type ControlLine =
    { Valves : Valve list
      Punches : Punch list
      Others : RestrictedEntity list }
    member v.Connected = (v.Punches <> [])

/// an entity intersection graph
/// has the entities as nodes
/// and an edge only between any two entities that intersect
let entityIntersectionGraph ( entities : Entity array ) =
    let entitiesIntersect i j =
        let enti = entities.[i]
        let entj = entities.[j]
        let points = new Point3dCollection()
        enti.IntersectWith(entj, Intersect.OnBothOperands, points, 0, 0)
        points.Count > 0
    let n = entities.Length
    let table = Array2.create n n false
    for i in [0..n-1] do
        table.[i,i] <- true
        let fi = entitiesIntersect i
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
    //member private v.lines = lazy v.computeLines()                
    member private v.computeUnconnectedLines() =
        Array.filter (fun (line : ControlLine) -> not line.Connected) v.Lines
    //member private v.unconnectedLines = lazy v.computeUnconnectedLines()
    member private v.computeUnconnectedPunches() =
        Array.filter (fun (punch : Punch) -> 
                          not (Array.exists (fun (line : ControlLine) -> List.mem punch line.Punches) v.Lines))
                     v.Punches
    //member private v.unconnectedPunches = lazy v.computeUnconnectedPunches()
    member private v.computeObstacles() =
        Array.filter (fun (other : RestrictedEntity) ->
                          not (Array.exists (fun (line : ControlLine) -> List.mem other line.Others) v.Lines))
                     v.Others
    //member private v.obstacles = lazy v.computeObstacles()
    member v.Valves = Array.of_list valves
    member v.Punches = Array.of_list punches
    member v.Others = Array.of_list others
    /// control lines
    member v.Lines with get() = lazyGet v.computeLines lines //v.lines.Force()
    /// unconnected lines are control lines that do not have punches
    member v.UnconnectedLines with get() = lazyGet v.computeUnconnectedLines unconnectedLines//v.unconnectedLines.Force()
    /// unconnected punches are punches that do not belong to any control line
    member v.UnconnectedPunches with get() = lazyGet v.computeUnconnectedPunches unconnectedPunches //v.unconnectedPunches.Force()
    /// obstacles are others that do not belong to any control line
    member v.Obstacles with get() = lazyGet v.computeObstacles obstacles //v.obstacles.Force()
