#light

/// automatic routing of valves (as control lines) to punches
module BioStream.Micado.Core.Routing

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream.Micado.User
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

open MgCS2

/// A routing grid
/// specifies that a set of sources and a set of targets, with the goal of connecting each distinct source to a distinct target.
/// In order to support Lee's algorithm, a routing grid should allow traceback.    
type IRoutingGrid =
    inherit IGrid
    abstract Sources : int array
    abstract Targets : int array
    /// Needed for traceback:
    /// for all v & all w: w in v.Neighbors <=> v in w.InverseNeighbors
    abstract InverseNeighbors : int -> int seq

let deltas = [1;-1]

/// A simple grid
/// is just a manhattan grid
/// covering the given bounding box
/// and where two adjacent // lines are separated by resolution.
type SimpleGrid ( resolution, boundingBox : Point2d * Point2d ) =
    let lowerLeft, upperRight = boundingBox
    let sizeX = upperRight.X - lowerLeft.X
    let sizeY = upperRight.Y - lowerLeft.Y
    let nX = int (System.Math.Ceiling(sizeX / resolution)) + 1
    let nY = int (System.Math.Ceiling(sizeY / resolution)) + 1
    let nodeCount = nX*nY
    let withinC nC c =
        0<=c && c<nC
    let withinX = withinC nX
    let withinY = withinC nY
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

/// A connection segment is a line whose thickness is specified by user setting ConnectionWidth
let connectionSegment startPoint endPoint = segmentPolyline (Settings.Current.ConnectionWidth) startPoint endPoint

let polylinePoints (polyline : #Polyline) = 
    Array.map (fun (i) -> polyline.GetPoint2dAt(i)) [|0..polyline.NumberOfVertices-1|]

let polylineSegments (polyline : #Polyline) =
    Array.map (fun (i) -> polyline.GetLineSegment2dAt(i)) 
              [|0..polyline.NumberOfVertices-(if polyline.Closed then 1 else 2)|]

/// whether the given point lies on a segment of the polyline    
let pointOnPolyline (p : Point2d) (polyline : #Polyline) =
    Seq.exists (fun (seg : LineSegment2d) -> Geometry.pointOnSegment p seg.StartPoint seg.EndPoint) 
               (polylineSegments polyline)

/// whether the given point is inside the area delimited by the given polyline
/// also returns true if the given point is on the polyline
/// always returns false if polyline is not closed
let interiorPoint (polyline : #Polyline) (p : Point2d) =
    if not polyline.Closed
    then false
    else
    if pointOnPolyline p polyline
    then true
    else
    let rayIntersectsSegment (segment : LineSegment2d) =
        let a = segment.StartPoint
        let b = segment.EndPoint
        (((b.Y <= p.Y) && (p.Y < a.Y)) ||
         ((a.Y <= p.Y) && (p.Y < b.Y))) &&
        (p.X < (a.X - b.X) * (p.Y - b.Y) / (a.Y - b.Y) + b.X)
    let count = Seq.length (Seq.filter rayIntersectsSegment (polylineSegments polyline))
    count % 2 = 1

/// whether the given point is inside the area delimited by the given polyline
/// also returns true if the given point is on the polyline
/// always returns false if polyline is not closed
/// assumes polygon is convex
let interiorPointConvex (polyline : #Polyline) (p : Point2d) =
    if not polyline.Closed
    then false
    else
    let pointOnLeftSide = Geometry.pointOnLeftSide p
    let pointOnSegment = Geometry.pointOnSegment p
    let pointOnInnerSide prevLeft a b =
        let curLeft = pointOnLeftSide a b
        match prevLeft, curLeft with
        | _, None -> (pointOnSegment a b, prevLeft)
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
                        - (segments.[i].Direction*extraWidth)
                        + (segments.[get(i-1)].Direction*extraWidth)
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

/// A calculator grid provides some calculation methods on top of a simple grid         
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
    let neighborCoordinatesWithSlope (slope : SegmentSlope) (x,y) =
        match slope with
        | Tilted -> Seq.empty
        | Horizontal -> { for dx in deltas when g.withinX(x+dx) -> (x+dx,y) }
        | Vertical -> { for dy in deltas when g.withinY(y+dy) -> (x,y+dy) }
    let allInteriorCoordinates (polyline : Polyline) =
        polyline.GeometricExtents
     |> fun (e) -> innerBoundingBox (Geometry.to2d e.MinPoint) (Geometry.to2d e.MaxPoint)
     |> allCoordinatesInBoundingBox
     |> Seq.filter (fun (c) -> c |> g.coordinates2point |> interiorPoint polyline)
    member v.surroundingIndices (point : Point2d) =
        point |> closestLowerLeftCoordinates |> surroundingCoordinates |> Seq.map g.coordinates2index
    [<OverloadID("interiorIndicesPolyline")>]
    member v.interiorIndices (polyline : Polyline) =
        polyline
     |> allInteriorCoordinates
     |> Seq.map g.coordinates2index 
     |> Seq.to_list // turn it to a list, because the polyline might be disposed by the time the sequence is accessed
    [<OverloadID("interiorIndicesFlowSegment")>]
    member v.interiorIndices (flowSegment : FlowSegment) =
        use polyline = flowSegment.to_polyline Settings.Current.FlowExtraWidth
        v.interiorIndices polyline     
    member v.neighborsWithSlope (slope : SegmentSlope) index =
        index |> g.index2coordinates |> neighborCoordinatesWithSlope slope |> Seq.map g.coordinates2index
    [<OverloadID("outerEdgesPolyline")>]
    member v.outerEdges (polyline : Polyline) =
        let outerBoundingBoxIndices (entity : #Entity) =
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
                     use segment = (connectionSegment ptA ptB) //(segmentPolyline0 ptA ptB)
                     if intersectWithPolyline segment
                     then if not (interiorOfPolyline ptA)
                          then yield (b,a)
                          if not (interiorOfPolyline ptB)
                          then yield (a,b)
        }
    [<OverloadID("outerEdgesPunch")>]
    member v.outerEdges (punch : Punch) =
        let center = punch.Center
        let width = Settings.Current.Punch2Line
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
        
let arrayOfRevList lst =
    lst |> List.rev |> Array.of_list        

let inverseMapList map =
    let add value' map' key' =
        match Map.tryfind key' map' with
        | None -> Map.add key' [value'] map'
        | Some lst -> Map.add key' (value'::lst) map'
    let addEntry key values map' =
        List.fold_left (add key) map' values
    Map.fold addEntry map Map.empty

let inverseMapSet map =
    let add value' key' map' =
        match Map.tryfind key' map' with
        | None -> Map.add key' (Set.Singleton value') map'
        | Some set -> Map.add key' (Set.add value' set) map'
    let addEntry key values map' =
        Set.fold (add key) values map'
    Map.fold addEntry map Map.empty

let disposeAllNew s =
    Seq.iter (fun (e :> DBObject) -> if e.IsNewObject then e.Dispose()) s
    
/// A chip grid represents a routing grid that has the connectivity of the chip
/// following the design rules:
/// Obstacles cannot be crossed.
/// Control lines cannot be entered.
/// Flow lines cannot be followed in parallel.
/// Foreign punches cannot be approached too closely, so their connectivity is like a black hole.
type ChipGrid ( chip : Chip ) =
    let g = new SimpleGrid (Settings.Current.Resolution, chip.BoundingBox)
    let ig = g :> IGrid
    let c = new CalculatorGrid (g)
    //let lines = chip.ControlLayer.UnconnectedLines
    //let punches = chip.ControlLayer.UnconnectedPunches
    let addEdge fromIndex toIndex edges =
        match Map.tryfind fromIndex edges with
        | None -> Map.add fromIndex (Set.Singleton toIndex) edges
        | Some set -> if Set.mem toIndex set
                      then edges
                      else Map.add fromIndex (Set.add toIndex set) edges
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
            use segment = index |> ig.ToPoint |> connectionSegment valve.Center
            segment |> chip.ControlLayer.intersectOutside line
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
        let (punch2index, acc') = addAll addPunch acc chip.ControlLayer.UnconnectedPunches
        let (line2index, acc'') = addAll addLine acc' chip.ControlLayer.UnconnectedLines
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
    let removeOfPolylines f (polylines : Polyline list) removedEdges =
        polylines
     |> Seq.map_concat c.outerEdges
     |> Seq.fold f removedEdges
    let removeOfPunch f removedEdges (punch : Punch) =
        punch
     |> c.outerEdges
     |> Seq.fold f removedEdges
    let removeOfPunches f punches removedEdges =
        Seq.fold (removeOfPunch f) removedEdges punches
    let removeEdgesOfLine removedEdges (line : ControlLine) =
        let valvePolylines =
            line.Valves |> Seq.map (fun (valve) -> valve :> Polyline) |> Seq.map_concat (to_polylines Settings.Current.ValveExtraWidth)
         |> List.of_seq
        let otherPolylines =
            line.Others |> Seq.map_concat (to_polylines Settings.Current.ControlLineExtraWidth)
         |> List.of_seq
        let removedEdges' =
            removedEdges
         |> removeOfPolylines incomingEdges valvePolylines
         |> removeOfPolylines incomingEdges otherPolylines
         |> removeOfPunches   incomingEdges (line.Punches :> Punch seq)
        disposeAllNew valvePolylines
        disposeAllNew otherPolylines
        removedEdges'
    let removedEdges = 
        let removedEdges' = Array.fold_left addFlowSegment Map.empty chip.FlowLayer.Segments
        let obstaclePolylines = 
            chip.ControlLayer.Obstacles |> Seq.map_concat (to_polylines 0.0)
         |> List.of_seq
        let removedEdges'' = removeOfPolylines doubleEdges obstaclePolylines removedEdges'
        disposeAllNew obstaclePolylines
        let removedEdges''' =
            Array.fold_left removeEdgesOfLine removedEdges'' chip.ControlLayer.Lines
        removedEdges'''
     |> removeOfPunches outgoingEdges (chip.ControlLayer.UnconnectedPunches :> Punch seq)
     |> removeOfPunches incomingEdges (chip.FlowLayer.Punches :> Punch seq)
    let toPoint index =
        if index < ig.NodeCount
        then ig.ToPoint index
        else nodes.[index-ig.NodeCount]
    let extraNeighbors edges index =
        match Map.tryfind index edges with
        | None -> Seq.empty
        | Some set -> Set.to_seq set
    let filteredNeighbors removedEdges index =
        if index >= ig.NodeCount
        then Seq.empty
        else ig.Neighbors index
          |> fun (seq) ->
                match Map.tryfind index removedEdges with 
                | None -> seq
                | Some set -> seq |> Seq.filter (fun (x) -> not (Set.mem x set)) 
    let computeNeighbors edges removedEdges index =
        { 
          yield! extraNeighbors edges index
          yield! filteredNeighbors removedEdges index
        }
    let neighbors = computeNeighbors edges removedEdges
    let inverseEdges = inverseMapSet edges
    let inverseRemovedEdges = inverseMapSet removedEdges
    let inverseNeighbors = computeNeighbors inverseEdges inverseRemovedEdges
    interface IRoutingGrid with
        member v.NodeCount =  nodeCount
        member v.Neighbors index = neighbors index
        member v.ToPoint index = toPoint index
        member v.Sources with get() = Array.copy line2index
        member v.Targets with get() = Array.copy punch2index
        member v.InverseNeighbors index = inverseNeighbors index
        
let createChipGrid (chip : Chip) =
    new ChipGrid (chip)
    
/// tries to find a routing solution,
/// in which each source is routed to a target, 
/// minimizing the total wiring length
/// but not, notably, the number of vias,
/// based on the paper
/// Hua Xiang, Xiaoping Tang, and Martin D. F. Wong. Min-cost Flow Based Algorithm for Simultaneous Pin Assignment and Routing, IEEE Transactions on Computer-Aided Design of Integrated Circuits and Systems, Vol. 22, No. 7, pp 870-878, July, 2003. 
let minCostFlowRouting ( grid : #IRoutingGrid ) =
    let sources = grid.Sources
    let targets = grid.Targets
    let nodeCount = grid.NodeCount
    // the indices of the vertices start at 1
    let node2incomingVertex (node : int) =
        uint32(node + 1) : uint32
    let node2outgoingVertex (node : int) = 
        uint32(nodeCount + node + 1) : uint32
    let outgoingVertex2node (vertex : uint32) =
        int(vertex) - nodeCount - 1
    let incomingVertex2node (vertex : uint32) =
        int(vertex) - 1
    let addEdge source (outgoingEdges, (numberOfEdges, edge2source, edge2target, edge2capacity, edge2cost)) target =
        (numberOfEdges :: outgoingEdges, 
         (numberOfEdges+1, source :: edge2source, target :: edge2target, 1.0 :: edge2capacity, 1.0 :: edge2cost))
    let addNode (node2outgoingEdges, acc) node =
        let outgoingVertex = node2outgoingVertex node
        let outgoingEdges, (numberOfEdges, edge2source, edge2target, edge2capacity, edge2cost) =
            Seq.fold (addEdge outgoingVertex)
                     ([], acc)
                     ((grid.Neighbors node) |> Seq.map node2incomingVertex)
        (outgoingEdges :: node2outgoingEdges, 
         (numberOfEdges+1, (node2incomingVertex node) :: edge2source, outgoingVertex :: edge2target, 1.0 :: edge2capacity, 0.0 :: edge2cost))
    let numberOfVertices = nodeCount*2 + 2
    let super_source_vertex = uint32(numberOfVertices - 1)
    let super_target_vertex = uint32(numberOfVertices)
    let addSuperSourceEdges acc =
        Seq.fold (fun (numberOfEdges, edge2source, edge2target, edge2capacity, edge2cost) source ->
                    (numberOfEdges+1, super_source_vertex :: edge2source, source :: edge2target, 1.0 :: edge2capacity, 0.0 :: edge2cost))
                 acc
                 (sources |> Seq.map node2incomingVertex)
    let addSuperTargetEdges node2outgoingEdges acc =  
        Seq.fold (fun (numberOfEdges, edge2source, edge2target, edge2capacity, edge2cost) target ->
                    node2outgoingEdges.[target] <- numberOfEdges :: node2outgoingEdges.[target]
                    (numberOfEdges+1,  (node2outgoingVertex target) :: edge2source, super_target_vertex :: edge2target, 1.0 :: edge2capacity, 0.0 :: edge2cost))
                 acc
                 targets           
    let node2outgoingEdges, acc =
        Seq.fold addNode ([], (0, [], [], [], [])) {0..nodeCount-1}
    let node2outgoingEdges = arrayOfRevList node2outgoingEdges
    let acc = addSuperSourceEdges acc
    let acc = addSuperTargetEdges node2outgoingEdges acc    
    let numberOfEdges, edge2source, edge2target, edge2capacity, edge2cost = acc
    let edge2source = arrayOfRevList edge2source
    let edge2target = arrayOfRevList edge2target
    let edge2capacity = arrayOfRevList edge2capacity
    let edge2cost = arrayOfRevList edge2cost
    let vertex2deficit = Array.create numberOfVertices 0.0
    vertex2deficit.[int(super_source_vertex)-1] <- - float(sources.Length)
    vertex2deficit.[int(super_target_vertex)-1] <- + float(sources.Length)
    let traceConnection x sourceNode =
        let rec helper acc outgoingVertex =
            let node = outgoingVertex2node outgoingVertex
            let edge = List.find (fun (edge) -> x.[edge] = 1.0) node2outgoingEdges.[node]
            let incomingVertex' = edge2target.[edge]
            if incomingVertex' = super_target_vertex
            then acc
            else let node' = incomingVertex' |> incomingVertex2node
                 let outgoingVertex' =  node' |> node2outgoingVertex
                 helper (node' :: acc) outgoingVertex'
        // the source node is purposefully not part of the trace
        // as it corresponds to a super node linking to all possible starting points for the source
        helper [] (sourceNode |> node2outgoingVertex)
    let traceAllConnections x =
        Array.map (traceConnection x) sources
    let findMinCostFlow() =
        let solver = new MgMCFSolver(uint32(numberOfVertices), uint32(numberOfEdges), edge2capacity, edge2cost, vertex2deficit, edge2source, edge2target)
        solver.SolveMCF();
        if not (solver.HasSolution())
        then None
        else let x = Array.create numberOfEdges 0.0
             solver.MCFGetX(x)
             Some x
    let solve() =
        match findMinCostFlow() with
        | None -> None
        | Some x -> Some (traceAllConnections x)
    solve()

let segmentSlope (a : Point2d) (b : Point2d) =
    match a.X=b.X, a.Y=b.Y with
    | true, _ -> Horizontal
    | _, true -> Vertical
    | _, _ -> Tilted

let sameSlope slope slope' =
    match slope, slope' with
    | Vertical, Vertical -> true
    | Horizontal, Horizontal -> true
    | _, _ -> false

/// Improves a starting solution using Lee's algorithm iteratively
type IterativeRouting (grid : IRoutingGrid, initialSolution) =
    let isTarget =
        let targets = 
            grid.Targets |> Array.map (fun (target) -> target, true) |> Map.of_array
        fun (node) ->
            Map.mem node targets
    let node2used = Array.create grid.NodeCount false
    let findTrace sourceNode =
        let node2level = Array.create grid.NodeCount (-1)
        node2level.[sourceNode] <- 0
        // bug fix: I changed from Seq to List
        // because Seq doesn't interact well with the side-effects in node2level
        // Seq will cause the neighbors filter to be evaluated _after_ 
        // the setting of the neighbors' node2level causing them to be all ignored
        let exploreNode node =
            let level = node2level.[node]+1
            let neighbors = grid.Neighbors node |> List.of_seq |> List.filter (fun (node') -> (not node2used.[node']) && (node2level.[node'] = -1))
            List.iter (fun (node') ->  node2level.[node'] <- level) neighbors
            neighbors
        let rec exploreBFS queue =
            if queue = []
            then failwith "did not find any target"
            else
            let reachedTargets = List.filter isTarget queue
            if reachedTargets <> []
            then reachedTargets
            else
            exploreBFS (List.map_concat exploreNode queue)           
        let reachedTargets = exploreBFS [sourceNode]
        let previousCells (node, level, point, slope) =
            grid.InverseNeighbors node 
         |> Seq.filter (fun (node') -> node2level.[node'] = level-1)
         |> Seq.map (fun (node') ->
                        let point' = grid.ToPoint node'
                        let slope' = segmentSlope point' point
                        (node',level-1,point',slope'))
        // non-greedy traceback doesn't appear terribly useful
        // still, I am keeping it (though turning it off) just in case
        let maxNonGreedyTurns = 0
        let keepBestTrace =
            Seq.fold1 (fun (slopeChanges, trace) (slopeChanges', trace') ->
                        if slopeChanges < slopeChanges'
                        then (slopeChanges, trace)
                        else (slopeChanges', trace'))
        // note: not tail-recursive
        let rec tracebackWith (slopeChanges, nonGreedyTurns, trace) (node,level,point,slope) =
            if level = 0
            // consistent with minCostFlowRouting
            // - don't include the sourceNode in the trace
            //   (it has no incoming edges, so it doesn't matter whether we mark it used or not)
            // - LATER, reverse the trace so that the real target is first
            then (slopeChanges, trace)
            else
            let cells = previousCells (node,level,point,slope)
            let preferredCells = if slope = Tilted
                                 then Seq.empty
                                 else cells |> Seq.filter (fun (_, _, _, slope') -> sameSlope slope slope')
            let preferredExists = Seq.nonempty preferredCells
            let trace' = node :: trace
            let traces =
                if preferredExists && nonGreedyTurns >= maxNonGreedyTurns
                then preferredCells
                  |> Seq.map (tracebackWith (slopeChanges, nonGreedyTurns, trace'))
                else cells 
                  |> Seq.map (fun (cell') ->
                                let _,_,_,slope' = cell'
                                let noSlopeChange = (sameSlope slope' slope)
                                let slopeChanges' =
                                    slopeChanges + (if noSlopeChange then 0 else 1)
                                let nonGreedyTurns' =
                                    nonGreedyTurns
                                  + (if noSlopeChange or not preferredExists then 0 else 1)
                                tracebackWith (slopeChanges', nonGreedyTurns', trace') cell')
            traces |> keepBestTrace         
        let tracebackFrom target =
            let level = node2level.[target]
            let point = grid.ToPoint target
            let slope = Tilted
            tracebackWith (0, 0, [target]) (target,level,point,slope)
        let tracebackAll targets =
            Seq.map tracebackFrom targets
         |> keepBestTrace
         |> (fun (slopeChanges, trace) -> (slopeChanges, List.rev trace))
        tracebackAll reachedTargets
    let sources = grid.Sources
    let solution = Array.copy initialSolution
    let solutionSlopeChanges = Array.create sources.Length 0
    let markTrace mark =
        List.iter (fun (node) -> node2used.[node] <- mark)
    let useTrace = markTrace true
    let freeTrace = markTrace false
    let reTrace sourceIndex =
        let sourceNode = sources.[sourceIndex]
        let oldTrace = solution.[sourceIndex]
        let oldSlopeChanges = solutionSlopeChanges.[sourceIndex]
        freeTrace oldTrace
        let newSlopeChanges, newTrace = findTrace sourceNode
        useTrace newTrace
        solution.[sourceIndex] <- newTrace
        solutionSlopeChanges.[sourceIndex] <- newSlopeChanges
        newSlopeChanges = oldSlopeChanges
    // changed to lists to make sure reTraceAll()
    // doesn't stop at the first sight of an unstable retrace
    let sourceIndices = [0..sources.Length-1]
    let reTraceAll() =
        List.for_all (fun (stable) -> stable) (List.map reTrace sourceIndices)
    let rec reTraceAllUntilStable n =
        let stable = reTraceAll()
        let n' = n+1
        if not stable
        then reTraceAllUntilStable n'
        else n'
    do Array.iter useTrace solution
    [<OverloadID("iterate")>]
    /// iterates the solution once
    /// returning an indicator of whether the solution is stable
    member v.iterate() = reTraceAll()
    [<OverloadID("iterateN")>]
    /// iterates the solution the given number of times or until stable
    /// returning an indicator of whether the solution is stable after the final iteration
    member v.iterate(n) = Seq.exists (fun (i) -> reTraceAll()) {0..n-1} // this will stop as soon as it's stable
    /// iterates the solution until it's stable
    /// returning the number of iterations
    member v.stabilize() = reTraceAllUntilStable 0
    /// the current solution
    member v.Solution with get() = Array.copy solution

/// Transforms a solution into a sequence of entities to be added to the drawing        
let presentConnections (grid : #IGrid) connections =
    let trace2points trace =
        let onlyTurningPoints points =
            let rec helper acc slope point rest =
                match rest with
                | [] -> point :: acc
                | point' :: rest' ->
                    let slope' = segmentSlope point point'
                    if sameSlope slope slope'
                    then helper acc slope' point' rest'
                    else helper (point :: acc) slope' point' rest'
            helper [] Tilted (List.hd points) (List.tl points)
        trace |> List.map grid.ToPoint |> onlyTurningPoints
    let points2entities points =
        let polyline = new Polyline()
        let addVertex point = polyline.AddVertexAt(polyline.NumberOfVertices, point, 0.0, Settings.Current.ConnectionWidth, Settings.Current.ConnectionWidth)
        List.iter addVertex points
        { yield (polyline :> Entity) }
    connections |> Array.map trace2points |> Seq.of_array |> Seq.map_concat points2entities
   