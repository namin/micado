#light

/// Common data types
module BioStream.Micado.Common.Datatypes

open BioStream
open BioStream.Micado.Common

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

type ChipEntities = 
    { FlowEntities : Entity list
      ControlEntities : Entity list }
      
type FlowSegment = 
    { Segment : LineSegment2d
      Width : double }
      
type Flow ( segments : FlowSegment list, punches : Punch list) =
    member v.Segments = segments
    member v.Punches = punches

type RestrictedEntity = Curve

type ControlLine =
    { Valves : Valve list
      Punches : Punch list
      Others : RestrictedEntity list }
    member v.Connected = (v.Punches <> [])

let lazyGet computeValue optionValue =
    match !optionValue with
    | Some value -> value
    | None -> 
        let value = computeValue()
        optionValue := Some value
        value

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

let to_entity (entity :> Entity) =
    entity :> Entity

let to_entities a =
    Array.map to_entity a
    
type Control ( valves : Valve list, punches : Punch list, others : RestrictedEntity list ) =
    let lines = ref None : ControlLine array option ref
    let unconnectedLines = ref None : ControlLine array option ref
    let unconnectedPunches = ref None : Punch array option ref
    member private v.computeLines() =
        let entities = Array.concat [(to_entities v.Valves);(to_entities v.Punches);(to_entities v.Others)]
        let graph = entityIntersectionGraph entities
        let components = Graph.ConnectedComponents graph
        let component2line els =
            let rec acc els valves punches others =
                match els with
                | [] -> { Valves=valves; Punches=punches; Others=others }
                | el::els -> match el with 
                                | el when el<v.Valves.Length -> 
                                    acc els (v.Valves.[el]::valves) punches others
                                | el when el<v.Valves.Length+v.Punches.Length ->
                                    acc els valves (v.Punches.[el-v.Valves.Length]::punches) others
                                | el ->
                                    acc els valves punches (v.Others.[el-v.Valves.Length-v.Punches.Length]::others)
            
            acc els [] [] []
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
    member v.Valves = Array.of_list valves
    member v.Punches = Array.of_list punches
    member v.Others = Array.of_list others
    member v.Lines with get() = lazyGet v.computeLines lines
    member v.UnconnectedLines with get() = lazyGet v.computeUnconnectedLines unconnectedLines
    member v.UnconnectedPunches with get() = lazyGet v.computeUnconnectedPunches unconnectedPunches

type Chip =
    { FlowLayer : Flow
      ControlLayer : Control }