#light

/// Common data types
module BioStream.Micado.Common.Datatypes

open BioStream
open BioStream.Micado.Common
open BioStream.Micado.Core

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

open System.Collections.Generic
    
type IGrid =
    inherit Graph.IGraph
    abstract ToPoint : int -> Point2d
    
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
            
let addVertexTo (polyline : #Polyline) (point : Point2d) =
    polyline.AddVertexAt(polyline.NumberOfVertices, point, 0.0, 0.0, 0.0)
            
/// creates a hallow polyline with the given width, and starting and ending mid points
let segmentPolyline width (startPoint : Point2d) (endPoint : Point2d) =
    let polyline = new Polyline()
    let addVertex = addVertexTo polyline
    if width=0.0
    then addVertex startPoint
         addVertex endPoint
    else let segment = new LineSegment2d (startPoint, endPoint)
         List.iter addVertex (Geometry.rectangleCorners width segment)
         polyline.Closed <- true
    polyline

let segmentPolyline0 = segmentPolyline 0.0
    
/// chip entities are just straight out of the database
type ChipEntities = 
    { FlowEntities : Entity list
      ControlEntities : Entity list }

type SegmentSlope = Horizontal | Vertical | Tilted

/// a flow segment
type FlowSegment (segment : LineSegment2d, width : double) =
    let pointComparisonFunction = ref None : (Point2d -> Point2d -> int) option ref
    let computePointComparisonFunction() =
        let angle = segment.Direction.Angle
        let rotation = Matrix2d.Rotation(-angle, Geometry.origin2d)
        fun (p1 : Point2d) (p2 : Point2d) ->
            let p1' = p1.TransformBy(rotation)
            let p2' = p2.TransformBy(rotation)
            (if (p1'.X = p2'.X)
             then p1'.Y - p2'.Y
             else p1'.X - p2'.X)
         |> fun (diff) -> compare diff 0.0
    let slope = ref None : SegmentSlope option ref
    let computeSlope() = 
        let around delta base angle =
            Geometry.angleWithin (base-delta) (base+delta) angle
        let angle = Geometry.rad2deg (segment.Direction.Angle)
        let near base = (around 30 base angle) || (around 30 (base+180) angle)
        match angle with
        | _ when (near 0) -> Horizontal
        | _ when (near 90) -> Vertical
        | _ -> Tilted
    let widthIndicatorSegments = ref None : LineSegment2d array option ref
    let computeWidthIndicatorSegments() =
        let normal = segment.Direction.GetPerpendicularVector()
        Array.map (Geometry.midSegmentEnds (width/2.0) normal)
                  [|segment.StartPoint; segment.EndPoint|]
     |> Array.map (fun (s,e) -> new LineSegment2d(s,e))          
    member v.Segment = segment
    member v.Width = width
    member v.PointComparisonFunction with get() = lazyGet computePointComparisonFunction pointComparisonFunction
    member v.getDistanceTo (point : Point2d) =
        let d = v.Segment.GetDistanceTo(point)
        let ds = v.Segment.StartPoint.GetDistanceTo(point)
        let de = v.Segment.EndPoint.GetDistanceTo(point)
        (if ds>d && de>d
         then d-v.Width/2.0
         else let endpoint = if ds<=d then v.Segment.StartPoint else v.Segment.EndPoint
              let pointVec = (point - endpoint).GetNormal()
              let segmentVec = v.Segment.Direction.GetPerpendicularVector()
              if pointVec = segmentVec || pointVec = segmentVec.Negate()
              then d-v.Width/2.0
              else d)
     |> max 0.0
    member f1.intersectWith (f2 : FlowSegment) =
        let fe1 = Geometry.extendSegment (f2.Width/2.0) f1.Segment
        let fe2 = Geometry.extendSegment (f1.Width/2.0) f2.Segment
        let points = fe1.IntersectWith(fe2)
        if points <> null && points.Length>0
        then Some points.[0]
        else // perhaps f1 and f2 are // flow segments that extend one another?
        let fmin, fmax = if f1.Width < f2.Width then f1, f2 else f2, f1
        match Array.map (fun (segment) -> fmin.Segment.IntersectWith(segment)) fmax.WidthIndicatorSegments with
        | [|points;null|] when points <> null -> Some fmax.Segment.StartPoint
        | [|null;points|] when points <> null -> Some fmax.Segment.EndPoint
        | _ -> None // if all null ignore
                    // if all intersects, 
                    // then ignore fmax because fmin takes it into account
                    // though, arguably, with perhaps a smaller width
                    // this limitation means that the user shouldn't draw a flow segment 
                    // with a greater width within a flow line with a smaller width
                    // removing this limitation would require fundamentally changing how intersections are dealt with
    member v.to_polyline extraWidth = segmentPolyline (v.Width/2.0+extraWidth) (v.Segment.StartPoint) (v.Segment.EndPoint)
    member v.Slope with get() = lazyGet computeSlope slope
    member v.WidthIndicatorSegments with get() = lazyGet computeWidthIndicatorSegments widthIndicatorSegments

let dictionaryArray (s : #('a seq)) =
    let d = new Dictionary<'a,int>()
    let add i key =
        if not (d.ContainsKey(key))
        then d.Add(key, i)
    Seq.iteri add s
    d
        
/// flow layer of a chip
type Flow ( segments : FlowSegment list, punches : Punch list) =
    let punchDictionaryArray = ref None
    let computePunchDictionaryArray() = dictionaryArray punches
    let getPunchDictionaryArray() = lazyGet computePunchDictionaryArray punchDictionaryArray
    let punch2index punch =
        if getPunchDictionaryArray().ContainsKey(punch)
        then Some (getPunchDictionaryArray().[punch])
        else None
    member v.Segments = Array.of_list segments
    member v.Punches = Array.of_list punches
    member v.Punch2Index punch = punch2index punch

/// acceptable entity type for any control entity other than valve and punch
type RestrictedEntity = Polyline

let entitiesIntersect (entity1 : #Entity) (entity2 : #Entity) =
    use points = new Point3dCollection()
    entity1.IntersectWith(entity2, Intersect.OnBothOperands, points, 0, 0)
    points.Count > 0

let containsEntity (els : # ('a seq)) (el : 'a when 'a :> DBObject) =
    let id : ObjectId = el.ObjectId
    Seq.exists (fun (el' : #DBObject) -> id.Equals(el'.ObjectId)) els
    
/// a control line represents a set of linked valves
/// with punches if connected,
/// others typically holds all the lines connecting the valves between them and to the punches
type ControlLine =
    { Valves : Valve list
      Punches : Punch list
      Others : RestrictedEntity list }
    member v.Connected = (v.Punches <> [])
    member v.intersectWith (entity : #Entity) =
        let intersectWithEntity (other : #Entity) = entitiesIntersect entity other
        List.exists intersectWithEntity v.Valves 
     || List.exists intersectWithEntity v.Punches 
     || List.exists intersectWithEntity v.Others
    member v.Representative = List.hd v.Valves
    [<OverloadID("ContainsValve")>]
    member v.Contains (valve : Valve) =
        containsEntity v.Valves valve
    [<OverloadID("ContainsPunch")>]
    member v.Contains (punch : Punch) =
        containsEntity v.Punches punch
    [<OverloadID("ContainsOther")>]
    member v.Contains (other : RestrictedEntity) =
        containsEntity v.Others other   
    [<OverloadID("ContainsEntity")>]
    member v.Contains (entity : Entity) =
        match entity with
        | :? Valve as valve -> v.Contains valve
        | :? Punch as punch -> v.Contains punch
        | :? RestrictedEntity as other -> v.Contains other
        | _ -> false
                
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
let to_entity (entity : #Entity) =
    entity :> Entity

// upcast an entire array of subtypes to entities
let to_entities a =
    Array.map to_entity a
    
/// control layer of a chip
type Control ( valves : Valve list, punches : Punch list, others : RestrictedEntity list ) =
    let valves = Array.of_list valves
    let punches = Array.of_list punches
    let others = Array.of_list others
    let doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument
    let lines = ref None : ControlLine array option ref
    let unconnectedLines = ref None : ControlLine array option ref
    let unconnectedPunches = ref None : Punch array option ref
    let obstacles = ref None : RestrictedEntity array option ref
    let lineNumbering = ref None : Permutation option ref
    let valve2line = ref None : int array option ref
    member v.computeValve2Line() =
        let toLineIndex valve = v.searchLines valve |> Option.get
        Array.map toLineIndex v.Valves
    member v.computeLineNumbering() =
        let getLineIndex i (line : ControlLine) =
            let index = line.Representative.Index
            if index = -1 then i else index
        let lines = v.Lines
        let p = lines |> Array.mapi getLineIndex
        try
            new Permutation(p)
        with
            | :? FailureException -> Permutation.Identity (lines.Length)
    member v.setLineNumbering(lineNumbering' : Permutation) =
        lineNumbering := Some lineNumbering'
        let lines = v.Lines
        let db = doc.Database
        use tr = db.TransactionManager.StartTransaction()
        use docLock = doc.LockDocument()
        for i = 0 to lines.Length-1 do
            let id = lines.[i].Representative.ObjectId
            use valve = tr.GetObject(id, OpenMode.ForWrite) :?> Valve
            valve.Index <- lineNumbering'.[i]
        tr.Commit()
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
    member v.Valves = valves
    member v.Punches = punches
    member v.Others = others
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
    member v.intersectOutside (line : ControlLine) (entity : #Entity) =
        let intersectWithEntity (other : #Entity) = entitiesIntersect entity other
        Array.exists (fun (otherLine : ControlLine) -> otherLine.intersectWith entity) (v.otherLines line)
     || Array.exists intersectWithEntity v.Obstacles
     || Array.exists intersectWithEntity v.UnconnectedPunches
    member v.LineNumbering with get() = lazyGet v.computeLineNumbering lineNumbering
                            and set(value : Permutation) = v.setLineNumbering(value)
    member v.searchLines(entity : Entity) =
        let lines = v.Lines
        let results = {0..lines.Length-1} |> Seq.filter (fun i -> lines.[i].Contains entity)
        if not (Seq.nonempty results)
        then None
        else Some (Seq.hd results)
    member v.Valve2Line with get() = lazyGet v.computeValve2Line valve2line
    member v.ToOpenLines (openValves : Set<int>) =
        let openLines = Array.create v.Lines.Length false
        for vi in openValves do
            openLines.[v.Valve2Line.[vi]] <- true
        openLines
        