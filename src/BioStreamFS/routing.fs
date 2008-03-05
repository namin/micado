#light

module BioStream.Micado.Core.Routing

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream.Micado.User
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

type IGrid =
    inherit Graph.IGraph
    abstract ToPoint : int -> Point2d

let deltas = [1;-1]
    
type SimpleGrid ( resolution, boundingBox : Point2d * Point2d ) =
    let lowerLeft, upperRight = boundingBox
    let sizeX = upperRight.X - lowerLeft.X
    let sizeY = upperRight.Y - lowerLeft.Y
    let nX = int (System.Math.Ceiling(sizeX / resolution)) + 1
    let nY = int (System.Math.Ceiling(sizeY / resolution)) + 1
    let nodeCount = nX*nY
    let withinX x =
        0<=x && x<nX
    let withinY y =
        0<=y && y<nY
    let within (x,y) =
        withinX x && withinY y
    let index2coordinates index =
        let x = index % nX
        let y = (index-x) / nX
        (x,y)
    let coordinates2index (x,y) =
        x+y*nX
    let neighborCoordinates (x,y) =
        { for d in deltas do
            let x' = x+d
            if withinX x'
            then yield (x',y)
            let y' = y+d
            if withinY y'
            then yield (x,y')
        }
    let coordinates2point (x,y) =
        new Point2d (lowerLeft.X+float(x)*resolution, lowerLeft.Y+float(y)*resolution)
    member v.NX = nX
    member v.NY = nY
    member v.LowerLeft = lowerLeft
    member v.Resolution = resolution
    member v.index2coordinates = index2coordinates
    member v.coordinates2index = coordinates2index
    member v.neighborCoordinates = neighborCoordinates
    member v.coordinates2point = coordinates2point
    member v.withinX = withinX
    member v.withinY = withinY
    member v.within = within
    interface IGrid with
        member v.NodeCount = nodeCount
        member v.Neighbors index = Seq.map coordinates2index (neighborCoordinates (index2coordinates index))
        member v.ToPoint index = index |> index2coordinates |> coordinates2point

let connectionSegment = segmentPolyline (Settings.ConnectionWidth)

let squarePolyline width (center : Point2d) = 
    let polyline = new Polyline()
    let addVertex (du,dr) = 
        polyline.AddVertexAt(polyline.NumberOfVertices, 
                             center
                            +Geometry.upVector.MultiplyBy(float(du)*width)
                            +Geometry.rightVector.MultiplyBy(float(dr)*width),
                             0.0, 0.0, 0.0)
    Seq.iter addVertex [(1,1); (-1,1); (-1,-1); (1,-1)]
    polyline.Closed <- true
    polyline
    
/// whether the first given point is on the left side 
/// of the segment from the second given point to the third given:
/// returns None if the point is on the segment
let onLeftSide (p : Point2d) (a : Point2d) (b : Point2d) =
    /// constructs a 3D vector from f to t
    let vector (f : Point2d) (t : Point2d) = new Vector3d(t.X - f.X, t.Y - f.Y, 0.0)
    let vAB = vector a b
    let vAP = vector a p
    let vCross = vAB.CrossProduct(vAP)
    if vCross.Z = 0.0
    then None
    else Some (vCross.Z > 0.0)

/// whether the given point is inside the area delimited by the given polyline
let interiorPoint (polyline :> Polyline) (p : Point2d) =
    if not polyline.Closed
    then false
    else
    let pointOnLeftSide = onLeftSide p
    let pointOnInnerSide prevLeft a b =
        let curLeft = pointOnLeftSide a b
        match prevLeft, curLeft with
        | _, None -> // check that point is on segment
                     let within ac bc pc = (min ac bc) <= pc && pc <= (max ac bc)
                     (within a.X b.X p.X && within a.Y b.Y p.Y, prevLeft)
        | Some prevLeft', Some curLeft' when prevLeft' <> curLeft' -> (false, prevLeft)
        | _ -> (true, curLeft)
    let n = polyline.NumberOfVertices - (if polyline.Closed then 0 else 1)
    let polylinePoint i = polyline.GetPoint2dAt(i % polyline.NumberOfVertices)
    let rec checkSides fromSide prevLeft a =
        if fromSide = n
        then true
        else let b = polylinePoint (fromSide+1)
             let (ok, prevLeft) = pointOnInnerSide prevLeft a b
             if not ok
             then false
             else checkSides (fromSide+1) prevLeft b
    checkSides 0 None (polylinePoint 0)

/// converts the given polyline to a sequence of equivalent polylines:
/// if the polyline is closed, just return a singleton polyline which is expanded by the extra width;
/// if the polyine is not closed, for each side, returns a polyline segment with the segment width 
/// and additionally expanded by the extra width
let to_polylines extraWidth (polyline : Polyline) =
    if polyline.Closed 
    then if extraWidth = 0.0
         then { yield polyline }
         else
         let ies = [|0..polyline.NumberOfVertices-1|]
         let segments = Array.map polyline.GetLineSegment2dAt ies
         let get i = i % polyline.NumberOfVertices |> fun (i) -> if i<0 then i+polyline.NumberOfVertices else i 
         let expand i = (polyline.GetPoint2dAt i)
                       -(segments.[i].Direction*extraWidth)
                       +(segments.[get(i-1)].Direction*extraWidth)
         let points = Array.map expand ies
         let polyline' = new Polyline()
         let addVertex point = polyline'.AddVertexAt(polyline'.NumberOfVertices, point, 0.0, 0.0, 0.0)
         Array.iter addVertex points
         polyline'.Closed <- true
         { yield polyline' }
    else let to_segmentPolyline (width, segment : LineSegment2d) =
            let extraInDir d = segment.Direction*float(d)*extraWidth
            let a = segment.StartPoint + extraInDir (-1)
            let b = segment.EndPoint + extraInDir 1
            segmentPolyline (width+extraWidth) a b
         let allSides = {for i in [0..polyline.NumberOfVertices-2] -> polyline.GetStartWidthAt i, polyline.GetLineSegment2dAt i}
         {for side in allSides -> to_segmentPolyline side}
         
type CalculatorGrid (g : SimpleGrid) =
    let ig = g :> IGrid
    let closestCoordinates rounder (point : Point2d) =
        [| point.X - g.LowerLeft.X; point.Y - g.LowerLeft.Y |]
     |> Array.map (fun d -> int (rounder(d / g.Resolution)))
     |> fun (a) -> (a.[0], a.[1])
    let closestLowerLeftCoordinates = closestCoordinates System.Math.Floor
    let closestUpperRightCoordinates = closestCoordinates System.Math.Ceiling
    let surroundingCoordinates lowerLeftCoordinates =
        let x,y = lowerLeftCoordinates
        { for dx in [0;1] do
            for dy in [0;1] do
                let c = (x+dx,y+dy)
                if g.within c
                then yield c
        }
    let innerBoundingBox (llPt : Point2d) (urPt : Point2d) =
        (closestUpperRightCoordinates llPt, closestLowerLeftCoordinates urPt)
    let outerBoundingBox (llPt : Point2d) (urPt : Point2d) =
        (closestLowerLeftCoordinates llPt, closestUpperRightCoordinates urPt)
    let allCoordinatesInBoundingBox (ll, ur) =
        let (llx, lly) =  ll
        let (urx, ury) =  ur
        { for x in [llx..urx] do
            for y in [lly..ury] do
                yield (x,y)
        }
    let neighborCoordinatesWithSlope (slope : FlowSegmentSlope) (x,y) =
        match slope with
        | Tilted -> Seq.empty
        | Horizontal -> { for dx in deltas when g.withinX(x+dx) -> (x+dx,y) }
        | Vertical -> { for dy in deltas when g.withinY(y+dy) -> (x,y+dy) }
    member v.surroundingIndices (point : Point2d) =
        point |> closestLowerLeftCoordinates |> surroundingCoordinates |> Seq.map g.coordinates2index
    [<OverloadID("interiorIndicesPolyline")>]
    member v.interiorIndices (polyline : Polyline) =
        polyline.GeometricExtents
     |> fun (e) -> innerBoundingBox (Geometry.to2d e.MinPoint) (Geometry.to2d e.MaxPoint)
     |> allCoordinatesInBoundingBox
     |> Seq.filter (fun (c) -> c |> g.coordinates2point |> interiorPoint polyline)
     |> Seq.map g.coordinates2index 
    [<OverloadID("interiorIndicesFlowSegment")>]
    member v.interiorIndices (flowSegment : FlowSegment) =
        flowSegment.to_polyline Settings.FlowExtraWidth |> v.interiorIndices     
    member v.neighborsWithSlope (slope : FlowSegmentSlope) index =
        index |> g.index2coordinates |> neighborCoordinatesWithSlope slope |> Seq.map g.coordinates2index
    [<OverloadID("outerEdgesPolyline")>]
    member v.outerEdges (polyline : Polyline) =
        let outerBoundingBoxIndices (entity :> Entity) =
            entity.GeometricExtents
         |> fun (e) -> outerBoundingBox (Geometry.to2d e.MinPoint) (Geometry.to2d e.MaxPoint)
         |> allCoordinatesInBoundingBox
         |> Seq.map g.coordinates2index                                               
        let intersectWithPolyline = entitiesIntersect polyline
        let interiorOfPolyline = interiorPoint polyline
        { for a in outerBoundingBoxIndices polyline do
            let ptA = ig.ToPoint a
            for b in ig.Neighbors a do
                if a < b // avoid duplicates
                then let ptB = ig.ToPoint b
                     if intersectWithPolyline (connectionSegment ptA ptB) //(segmentPolyline0 ptA ptB)
                     then if not (interiorOfPolyline ptA)
                          then yield (b,a)
                          if not (interiorOfPolyline ptB)
                          then yield (a,b)
        }
    [<OverloadID("outerEdgesPunch")>]
    member v.outerEdges (punch : Punch) =
        let center = punch.Center
        let width = Settings.Punch2Line
        let (minX, minY), (maxX, maxY) =
            closestLowerLeftCoordinates center, closestUpperRightCoordinates center
        let bounds =
            let cornerPoint d = center
                               +Geometry.upVector.MultiplyBy(float(d)*width)
                               +Geometry.rightVector.MultiplyBy(float(d)*width)
            innerBoundingBox (cornerPoint (-1)) (cornerPoint (+1))
        let cs = allCoordinatesInBoundingBox bounds
        let outer1 c c' minC maxC = (c' < c && c <= minC) || (c' > c && c >= maxC)
        let outer (x,y) (x',y') = outer1 x x' minX maxX || outer1 y y' minY maxY
        { for c in cs do
            let i = g.coordinates2index c
            for c' in g.neighborCoordinates c do
                if outer c c'
                then yield (i,(g.coordinates2index c'))
        }
               
type ChipGrid ( chip : Chip ) =
    let g = new SimpleGrid (Settings.Resolution, chip.BoundingBox)
    let ig = g :> IGrid
    let c = new CalculatorGrid (g)
    let lines = chip.ControlLayer.UnconnectedLines
    let punches = chip.ControlLayer.UnconnectedPunches
    let arrayOfRevList lst =
        lst |> List.rev |> Array.of_list
    let addEdge fromIndex toIndex edges =
        match Map.tryfind fromIndex edges with
        | None -> Map.add fromIndex [toIndex] edges
        | Some lst -> if List.mem toIndex lst
                      then edges
                      else Map.add fromIndex (toIndex::lst) edges
    let addDoubleEdge fromIndex toIndex edges =
        addEdge fromIndex toIndex edges
     |> addEdge toIndex fromIndex
    let addPunch (n,nodes,edges) (punch : Punch) =
        let indices = c.surroundingIndices punch.Center 
        let nodes' = punch.Center :: nodes
        let edges' = Seq.fold (fun edges fromIndex -> addEdge fromIndex n edges) edges indices
        (n, (n+1, nodes', edges'))
    let addInterior lineIndex edges (polyline : Polyline) =
        (c.interiorIndices polyline)
     |> Seq.fold (fun edges toIndex -> addEdge lineIndex toIndex edges) edges      
    let addValve line lineIndex (n,nodes,edges) (valve : Valve) =
        let conflicts index =
            index |> ig.ToPoint |> connectionSegment valve.Center |> chip.ControlLayer.intersectOutside line
        let indices = c.surroundingIndices valve.Center |> Seq.filter (fun (i) -> not (conflicts i))
        let nodes' = valve.Center :: nodes
        let edges' = Seq.fold (fun edges toIndex -> addEdge n toIndex edges) 
                              (addEdge lineIndex n edges)
                              indices
        let edges'' = addInterior lineIndex edges' (valve :> Polyline)
        (n+1, nodes', edges'')
    let addLine (n,nodes,edges) (line : ControlLine) =
        let lineCenter = (List.hd line.Valves).Center // arbitrarily set the line point to the first valve center
        let (n',nodes',edges') = List.fold_left (addValve line n) (n+1,lineCenter::nodes,edges) (line.Valves)
        let edges'' = List.fold_left (addInterior n) edges' line.Others
        (n, (n', nodes', edges''))
    let accadd adder (lst, acc) el =
        let (eli, acc') = adder acc el
        (eli::lst, acc')
    let addAll adder acc els =
        let (rl, acc) = Array.fold_left (accadd adder) ([], acc) els
        (arrayOfRevList rl, acc)
    let punch2index, line2index, nodeCount, nodes, edges =
        let acc = (ig.NodeCount, [], Map.empty)
        let (punch2index, acc') = addAll addPunch acc punches
        let (line2index, acc'') = addAll addLine acc' lines
        let (nodeCount, nodes, edges) = acc''
        (punch2index, line2index, nodeCount, arrayOfRevList nodes, edges)
    let addFlowSegment removedEdges (flowSegment : FlowSegment) =
        match flowSegment.Slope with
        | Tilted -> removedEdges
        | slope -> let removeNeighbors removedEdges index = 
                       c.neighborsWithSlope slope index
                    |> Seq.fold (fun removedEdges neighbor -> addEdge index neighbor removedEdges |> addEdge neighbor index) removedEdges
                   c.interiorIndices flowSegment
                |> Seq.fold removeNeighbors removedEdges 
    let incomingEdges removedEdges (a,b) = addEdge b a removedEdges
    let outgoingEdges removedEdges (a,b) = addEdge a b removedEdges
    let doubleEdges removedEdges (a,b) = addDoubleEdge a b removedEdges
    let removeOfPolylines f (polylines : Polyline seq) removedEdges =
        polylines
     |> Seq.map_concat c.outerEdges
     |> Seq.fold f removedEdges
    let removeOfPunch f removedEdges (punch : Punch) =
        punch
     |> c.outerEdges
     |> Seq.fold f removedEdges
    let removeOfPunches f (punches : Punch list) removedEdges =
        Seq.fold (removeOfPunch f) removedEdges punches
    let removeEdgesOfLine removedEdges (line : ControlLine) =
        let valvePolylines (valves : Valve list) =
            valves |> Seq.map (fun (valve) -> valve :> Polyline) |> Seq.map_concat (to_polylines Settings.ValveExtraWidth)
        let otherPolylines(others : RestrictedEntity list) =
            others |> Seq.map_concat (to_polylines Settings.ControlLineExtraWidth)
        removedEdges
     |> removeOfPolylines incomingEdges (valvePolylines line.Valves)
     |> removeOfPolylines incomingEdges (otherPolylines line.Others)
     |> removeOfPunches   incomingEdges line.Punches
    let removedEdges = 
        List.fold_left addFlowSegment Map.empty chip.FlowLayer.Segments
     |> (chip.ControlLayer.Obstacles
         |> Seq.map_concat (to_polylines 0.0)
         |> removeOfPolylines doubleEdges)
     |> fun removedEdges ->
            chip.ControlLayer.Lines
         |> Array.fold_left removeEdgesOfLine removedEdges
     |> removeOfPunches outgoingEdges (List.of_array chip.ControlLayer.UnconnectedPunches)
     |> removeOfPunches incomingEdges chip.FlowLayer.Punches
    let toPoint index =
        if index < ig.NodeCount
        then ig.ToPoint index
        else nodes.[index-ig.NodeCount]
    let extraNeighbors index =
        match Map.tryfind index edges with
        | None -> Seq.empty
        | Some lst -> Seq.of_list lst
    let filteredNeighbors index =
        if index >= ig.NodeCount
        then Seq.empty
        else ig.Neighbors index
          |> fun (seq) ->
                match Map.tryfind index removedEdges with 
                | None -> seq
                | Some lst -> seq |> Seq.filter (fun (x) -> not (List.mem x lst)) 
    let neighbors index =
        { 
          yield! extraNeighbors index
          yield! filteredNeighbors index
        }
    interface IGrid with
        member v.NodeCount =  nodeCount
        member v.Neighbors index = neighbors index
        member v.ToPoint index = toPoint index
        
let createChipGrid (chip : Chip) =
    new ChipGrid (chip)