#light

module BioStream.Micado.Core.FlowRepresentation

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream.Micado.User
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

open System.Collections.Generic

/// returns an element of the tuple (a,b) that is different from s
let differentFrom s (a,b) =
    if a=s then b else a    

/// returns an element of the tuple (a,b) that the same as s
let sameAs s (a,b) =
    if a=s 
    then s 
    else 
    if b=s
    then b
    else failwith "sameAs: no element of (a,b) is the same as s"


/// Various utilities related to transforming collections
module Utils =
    /// given a sequence of key,value pairs
    /// where each key may appear multiple times
    /// constructs a map from each key to all its values (as a list)
    let mapListOfSeq seq =
        let addEntry map (key,value) =
            match Map.tryfind key map with
            | None -> Map.add key [value] map
            | Some lst -> Map.add key (value::lst) map
        seq |> Seq.fold addEntry Map.empty

    /// given a sequence, seq, of index,value pairs
    /// where each index may appear multiple times
    /// constructs an indexed array of size n with all the values for each index
    /// (generic operation in terms of the type of the value collection:
    /// the argument empty specifies the empty collection
    /// the argument add specifies how to cons a new value into a collection)
    let arrayCollectionOfSeq empty add n seq =
        let a = Array.create n empty
        let addEntry (index,value) =
            a.[index] <- (add value a.[index])
        seq |> Seq.iter addEntry
        a

    let arrayListOfSeq n seq = arrayCollectionOfSeq [] (fun el lst -> el::lst) n seq

    let arraySetOfSeq n seq = arrayCollectionOfSeq Set.empty Set.add n seq

    /// given an array size and a map from a 'key to an index
    /// creates an indexed array of the keys
    /// assumes each index appears exactly once as a value in the map
    let mapOfIndexValues2arrayOfKeys (n,map) =
        let a = Array.zero_create n
        Map.iter (fun k i -> a.[i] <- k) map
        a

    let dictionaryOfIndexValues2arrayOfKeys (n,(map : Dictionary<'a,int>)) =
        let a = Array.zero_create n
        for kv in map do
            a.[kv.Value] <- kv.Key
        a

/// All the computing helpers for building flow representations
module Compute =  
    /// returns the index of the segment from the given array
    /// that is closest to the given point
    /// considering only the first n segments
    let closestSegmentIndexUpTo n (segments : FlowSegment array) (point : Point2d) =
        segments 
     |> Array.mapi (fun i f -> if i<n then (f.getDistanceTo point), i else System.Double.MaxValue, i)
     |> Array.fold1_right min
     |> snd

    /// returns the index of the segment from the given array
    /// that is closest to the given point
    let closestSegmentIndex (segments : FlowSegment array) (point : Point2d) =
        closestSegmentIndexUpTo segments.Length segments point
        
    /// returns the index of the segment from the given array
    /// that is closest to the given punch
    let punch2segmentIndex (segments : FlowSegment array) (punch : Punch) =
        closestSegmentIndex segments punch.Center
    
    /// returns an array indexed by the segments
    /// for each segment, the value is a list of indices of the punches closest to that segment
    let segment2punchIndices (segments : FlowSegment array) (punches : Punch array) =
        let pi2si = punch2segmentIndex segments << fun (pi) -> (punches.[pi])
        let si2pis = Array.create segments.Length []
        Seq.iter (fun (pi) -> let si = pi2si pi
                              si2pis.[si] <- pi::si2pis.[si]) 
                 {0..(punches.Length-1)}
        si2pis

    let flowIntersectionPoints (segments : FlowSegment array) =
        let n = segments.Length
        let table = Array2.create n n None
        for i in [0..n-1] do
            let fi = segments.[i]
            for j in [i+1..n-1] do  
                let fj = segments.[j]
                let p = fi.intersectWith(fj)
                table.[i,j] <- p
                table.[j,i] <- p
        table
         
    let allNodes (punches : Punch array) table =
        let map = new Dictionary<Point2d, int>()
        let addNode n p =
            if map.ContainsKey(p)
            then n
            else map.Add(p, n)
                 n+1
        punches |> Array.iteri (fun i punch -> map.Add(punch.Center, i))
        let nodes = seq { for i in [0..(Array2.length1 table)-1] do
                          for j in [i..(Array2.length2 table)-1] do
                          let Some p = table.[i,j]
                          yield p }
        let n = Seq.fold addNode punches.Length nodes
        Utils.dictionaryOfIndexValues2arrayOfKeys (n,map), map
    
    let allEdges (segments : FlowSegment array) (punches : Punch array) table =
        let addPunch si =
            let f = segments.[si]
            fun nodes pi ->
                let center = punches.[pi].Center
                if f.Segment.StartPoint.GetDistanceTo(center) < f.Segment.EndPoint.GetDistanceTo(center)
                then center :: nodes
                else List.append nodes [center]
        let punchesOfSegment =
            let segment2punchIndices = segment2punchIndices segments punches
            fun (si) -> segment2punchIndices.[si]
        let pointsOfSegment si =
            [ for sj in [0..(Array2.length2 table)-1] do
              let Some p = table.[si,sj]
              yield p ]
           |> List.sort segments.[si].PointComparisonFunction
        let nodesOfSegment si =
            List.fold_left (addPunch si) (pointsOfSegment si) (punchesOfSegment si)
        let edgesOfSegment si =
            let f = segments.[si]
            nodesOfSegment si
         |> Seq.pairwise
         |> Seq.map (fun (a,b) -> new FlowSegment(new LineSegment2d(a,b),f.Width))
        let allEdges =
            {0..(segments.Length-1)}
         |> Seq.map edgesOfSegment
         |> Seq.concat
        allEdges |> Array.of_seq
             
    let node2edges n edge2flowSegment point2node =
        edge2flowSegment
     |> Array.mapi (fun e (f : FlowSegment) ->
                        let s = point2node.[f.Segment.StartPoint]
                        let t = point2node.[f.Segment.EndPoint]
                        [(s,e);(t,e)])
     |> Seq.concat
     |> Utils.arraySetOfSeq n

    let node2props propFun (edge2flowSegment : FlowSegment array) point2node node2edges =
        let edge2prop s e =
            let f = edge2flowSegment.[e]
            let t = differentFrom s (point2node.[f.Segment.StartPoint], point2node.[f.Segment.EndPoint])
            propFun s t e
        node2edges
     |> Array.mapi (fun s es -> es |> Set.map (edge2prop s))
 
    let node2neighbors edge2flowSegment point2node node2edges =
        node2props (fun s t e -> t) edge2flowSegment point2node node2edges

/// Flow Representation
type IFlowRepresentation =
    inherit IGrid
    /// number of edges
    abstract EdgeCount : int
    /// given an edge, returns the flow segment
    abstract ToFlowSegment : int -> FlowSegment
    /// given a point, returns the closest edge to that point
    abstract ClosestEdge : Point2d -> int
    /// given a node, returns all the edges connected to that node
    abstract NodeEdges : int -> Set<int>
    /// given a point, returns the node exactly at that point
    /// failing if the point doesn't map to a node
    abstract OfPoint : Point2d -> int

/// returns the length of an edge
let edge2length (rep : IFlowRepresentation) edge =
    (rep.ToFlowSegment edge).Segment.Length

/// given a segment, returns its endpoints as nodes
let segment2nodes (rep : IFlowRepresentation) (segment : LineSegment2d) = 
    let aPt,bPt = segment.StartPoint, segment.EndPoint
    let a,b = rep.OfPoint aPt, rep.OfPoint bPt
    (a,b)

/// given a node and a segment of which the given node is an endpoint,
/// returns the other endpoint of the segment as a node
let nodeNsegment2otherNode (rep : IFlowRepresentation) s (segment : LineSegment2d) =
    differentFrom s (segment2nodes rep segment)

/// given a node and an edge of which the given node is an endpoint,
/// returns a tuple (edge, other node, length of edge)    
let nodeNedge2extension (rep : IFlowRepresentation) s edge =
    let segment = (rep.ToFlowSegment edge).Segment
    let t = nodeNsegment2otherNode rep s segment
    let length = segment.Length
    (edge, t, length)

/// given an edge, returns its two nodes
let edge2nodes (rep : IFlowRepresentation) edge =
    segment2nodes rep (rep.ToFlowSegment edge).Segment

module Path =

    type IPath =
        abstract member Length : float
        abstract member Edges : int list
        abstract member Nodes : int list
        abstract member StartNode : int
      
    let create startNode =
        let nodes = [startNode]
        {new IPath with
            member p.Length = 0.0
            member p.Edges = []
            member p.Nodes = nodes
            member p.StartNode = startNode
        }
        
    let extend (path : IPath) (edge, node, edgeLength) =
        let length = path.Length + edgeLength
        let edges = edge :: path.Edges
        let nodes = node :: path.Nodes
        let startNode = path.StartNode
        { new IPath with
            member p.Length = length
            member p.Edges = edges
            member p.Nodes = nodes
            member p.StartNode = startNode
        }
    
    let pathComparer =
        { new System.Collections.IComparer with
            member v.Compare(ox, oy) =
                match ox, oy with
                | (:? IPath as px), (:? IPath as py) -> compare px.Length py.Length
                | _ -> failwith "pathComparer can only compare paths!"
        }
    
module Search =

    open BioStream.Micado.Utils.PriorityQueue
    
    let reachedSomeGoal (goalNodes : Set<int>) (path : Path.IPath) =
        goalNodes.Contains (List.hd path.Nodes)

    let findShortestPath (rep : IFlowRepresentation) (removedEdges : Set<int>) =
        let filterEdges =
            if Set.is_empty removedEdges
            then fun edges -> edges
            else
            fun edges -> edges |> Set.filter (fun edge -> not (removedEdges.Contains edge)) 
        fun (startNodes : Set<int>) (goalNodes : Set<int>) ->
            let pathIsDone = reachedSomeGoal goalNodes
            let node2distance = Array.create rep.NodeCount System.Double.MaxValue
            let queue = new BinaryPriorityQueue(Path.pathComparer)
            let explore (path : Path.IPath) node = 
                let extendPath edge = 
                    nodeNedge2extension (rep : IFlowRepresentation) node edge
                 |> Path.extend path
                node2distance.[node] <- path.Length
                for edge in filterEdges(rep.NodeEdges node) do
                    queue.Push(extendPath edge) |> ignore                     
            Set.iter (Path.create >> queue.Push >> ignore) startNodes
            let mutable shortestPath = None
            while shortestPath = None && (queue.Count > 0) do
                let path = queue.Pop() :?> Path.IPath
                let node = List.hd (path.Nodes)
                if node2distance.[node] > path.Length
                then if pathIsDone path
                     then shortestPath <- Some path
                     else explore path node
            shortestPath
                   
/// creates a flow representation:        
/// the first flow.Punches.Length nodes are punch nodes, s.t.
/// the ith punch maps to the ith node        
let create (flow : Flow) =
    let intersectionTable = Compute.flowIntersectionPoints flow.Segments
    let node2point, point2node = Compute.allNodes flow.Punches intersectionTable
    let edge2flowSegment = Compute.allEdges flow.Segments flow.Punches intersectionTable
    let node2edges = Compute.node2edges node2point.Length edge2flowSegment point2node
    let node2neighbors = Compute.node2neighbors edge2flowSegment point2node node2edges
    let punchCount = flow.Punches.Length
    { new IFlowRepresentation with
        member v.NodeCount = node2point.Length
        member v.Neighbors node = Set.to_seq node2neighbors.[node]
        member v.ToPoint node = node2point.[node]
        member v.EdgeCount = edge2flowSegment.Length
        member v.ToFlowSegment edge = edge2flowSegment.[edge] 
        member v.ClosestEdge point = Compute.closestSegmentIndex edge2flowSegment point // could be optimized
        member v.NodeEdges node = node2edges.[node]
        member v.OfPoint point = point2node.[point]
    }

/// adds the given valves to the given flow representation:
/// the last valves.Length nodes are the valve nodes, s.t.
/// the ith valve maps to the (NodeCount-1-i)th node
let addValves (valves : Valve array) (rep : IFlowRepresentation) =
    let nodeCount = rep.NodeCount + valves.Length
    let edgeCount = rep.EdgeCount + valves.Length
    let node2point = Array.zero_create nodeCount
    let point2node = new Dictionary<Point2d, int>()
    let node2edges = Array.zero_create nodeCount
    let addNode node point edges =
        node2point.[node] <- point
        node2edges.[node] <- edges
        if not (point2node.ContainsKey point)
        then point2node.Add(point, node)
    for node = 0 to rep.NodeCount-1 do
        addNode node (rep.ToPoint node) (rep.NodeEdges node)
    let edge2flowSegment = Array.zero_create edgeCount
    for edge = 0 to rep.EdgeCount-1 do
        edge2flowSegment.[edge] <- rep.ToFlowSegment edge
    let addValve vi (valve : Valve) =
        let vc = valve.Center
        let vn = rep.NodeCount + vi
        let e' = rep.EdgeCount + vi
        let e = Compute.closestSegmentIndexUpTo e' edge2flowSegment vc
        let f = edge2flowSegment.[e]
        let vp = f.Segment.GetClosestPointTo(vc).Point
        let sp, tp = f.Segment.StartPoint, f.Segment.EndPoint
        let sn, tn = point2node.[sp], point2node.[tp]
        let ef = new FlowSegment(new LineSegment2d(sp, vp), f.Width)
        let ef' = new FlowSegment(new LineSegment2d(vp, tp), f.Width)
        edge2flowSegment.[e] <- ef
        edge2flowSegment.[e'] <- ef'
        node2edges.[tn] <- (node2edges.[tn].Remove e).Add e'
        addNode vn vp (Set.of_list [e;e'])
    Array.iteri addValve valves
    let node2neighbors = Compute.node2neighbors edge2flowSegment point2node node2edges
    { new IFlowRepresentation with
        member v.NodeCount = nodeCount
        member v.Neighbors node = Set.to_seq node2neighbors.[node]
        member v.ToPoint node = node2point.[node]
        member v.EdgeCount = edgeCount
        member v.ToFlowSegment edge = edge2flowSegment.[edge] 
        member v.ClosestEdge point = Compute.closestSegmentIndex edge2flowSegment point // could be optimized
        member v.NodeEdges node = node2edges.[node]
        member v.OfPoint point = point2node.[point]
    }
        