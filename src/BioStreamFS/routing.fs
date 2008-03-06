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

open MgCS2

type IGrid =
    inherit Graph.IGraph
    abstract ToPoint : int -> Point2d

type IRoutingGrid =
    inherit IGrid
    abstract Sources : int array
    abstract Targets : int array
    abstract InverseNeighbors : int -> int seq // needed for traceback

let deltas = [1;-1]
    
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

let connectionSegment = segmentPolyline (Settings.ConnectionWidth)
    
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
        | _, None -> let within ac bc pc = (min ac bc) <= pc && pc <= (max ac bc)
                     let onSegment = within a.X b.X p.X && within a.Y b.Y p.Y
                     (onSegment, prevLeft)
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
    [<OverloadID("interiorIndicesFlowSegment")>]
    member v.interiorIndices (flowSegment : FlowSegment) =
        flowSegment.to_polyline Settings.FlowExtraWidth |> v.interiorIndices     
    member v.neighborsWithSlope (slope : SegmentSlope) index =
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

let arrayOfRevList lst =
    lst |> List.rev |> Array.of_list
        
type ChipGrid ( chip : Chip ) =
    let g = new SimpleGrid (Settings.Resolution, chip.BoundingBox)
    let ig = g :> IGrid
    let c = new CalculatorGrid (g)
    let lines = chip.ControlLayer.UnconnectedLines
    let punches = chip.ControlLayer.UnconnectedPunches
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
    let extraNeighbors edges index =
        match Map.tryfind index edges with
        | None -> Seq.empty
        | Some lst -> Seq.of_list lst
    let filteredNeighbors removedEdges index =
        if index >= ig.NodeCount
        then Seq.empty
        else ig.Neighbors index
          |> fun (seq) ->
                match Map.tryfind index removedEdges with 
                | None -> seq
                | Some lst -> seq |> Seq.filter (fun (x) -> not (List.mem x lst)) 
    let computeNeighbors edges removedEdges index =
        { 
          yield! extraNeighbors edges index
          yield! filteredNeighbors removedEdges index
        }
    let neighbors = computeNeighbors edges removedEdges
    let inverseMap map =
        let add value' map' key' =
            match Map.tryfind key' map' with
            | None -> Map.add key' [value'] map'
            | Some lst -> Map.add key' (value'::lst) map'
        let addEntry key values map' =
            List.fold_left (add key) map' values
        Map.fold addEntry map Map.empty
    let inverseEdges = inverseMap edges
    let inverseRemovedEdges = inverseMap removedEdges
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
let minCostFlowRouting ( grid :> IRoutingGrid ) =
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
        let rec tracebackWith (slopeChanges, trace) (node,level,point,slope) =
            if level = 0
            // consistent with minCostFlowRouting
            // - don't include the sourceNode in the trace
            //   (it has no incoming edges, so it doesn't matter whether we mark it used or not)
            // - reverse the trace so that the real target is first
            then (slopeChanges, List.rev trace)
            else
            let cells = previousCells (node,level,point,slope)
            let (slopeDelta, cell) = 
                    cells 
                    |> Seq.filter (fun (cell) -> 
                                    let _, _, _,slope' = cell
                                    slope' = slope)
                    |> fun (preferredCells) ->
                       if Seq.nonempty preferredCells
                       then (0, Seq.hd preferredCells)
                       else (1, Seq.hd cells)
            tracebackWith (slopeChanges+slopeDelta, (node :: trace)) cell
        let keepBestTrace =
            Seq.fold1 (fun (slopeChanges, trace) (slopeChanges', trace') ->
                        if slopeChanges < slopeChanges'
                        then (slopeChanges, trace)
                        else (slopeChanges', trace'))
        let tracebackFrom target =
            let level = node2level.[target]
            let point = grid.ToPoint target
            let slope = Tilted
            Seq.map (tracebackWith (0, [target])) (previousCells (target,level,point,slope))
         |> keepBestTrace
        let tracebackAll targets =
            Seq.map tracebackFrom targets
         |> keepBestTrace
        tracebackAll reachedTargets |> fun (_, trace) -> trace
    let sources = grid.Sources
    let solution = Array.copy initialSolution
    let markTrace mark =
        List.iter (fun (node) -> node2used.[node] <- mark)
    let useTrace = markTrace true
    let freeTrace = markTrace false
    let reTrace sourceIndex =
        let sourceNode = sources.[sourceIndex]
        let oldTrace = solution.[sourceIndex]
        freeTrace oldTrace
        let newTrace = findTrace sourceNode
        useTrace newTrace
        solution.[sourceIndex] <- newTrace
        newTrace = oldTrace
    let sourceIndices = {0..sources.Length-1}
    let reTraceAll() =
        Seq.for_all (fun (stable) -> stable) (Seq.map reTrace sourceIndices)
    let rec reTraceAllUntilStable n =
        let stable = reTraceAll()
        if not stable
        then reTraceAllUntilStable (n+1)
        else n
    do Array.iter useTrace solution
    [<OverloadID("iterate")>]
    member v.iterate() = reTraceAll() |> ignore
                         v.Solution
    [<OverloadID("iterateN")>]
    member v.iterate(n) = Seq.iter (fun (i) -> reTraceAll() |> ignore) {0..n-1}
                          v.Solution
    member v.stabilize() = reTraceAllUntilStable 0
    member v.Solution with get() = Array.copy solution
        
let presentConnections (grid :> IGrid) connections =
    let trace2points trace =
        let onlyTurningPoints points =
            let rec helper acc slope point rest =
                match rest with
                | [] -> point :: acc
                | point' :: rest' ->
                    let slope' = segmentSlope point point'
                    if slope=Tilted or slope <> slope'
                    then helper (point :: acc) slope' point' rest'
                    else helper acc slope' point' rest'
            helper [] Tilted (List.hd points) (List.tl points)
        trace |> List.map grid.ToPoint |> onlyTurningPoints
    let points2entities points =
        let polyline = new Polyline()
        let addVertex point = polyline.AddVertexAt(polyline.NumberOfVertices, point, 0.0, Settings.ConnectionWidth, Settings.ConnectionWidth)
        List.iter addVertex points
        { yield (polyline :> Entity) }
    connections |> Array.map trace2points |> Seq.of_array |> Seq.map_concat points2entities
   