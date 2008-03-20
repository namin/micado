#light

module BioStream.Micado.Core.Instructions

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream.Micado.User
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

open System.Collections.Generic

/// returns the index of the segment from the given array
/// that is closest to the given point
/// considering only the first n segments
let private computeClosestSegmentIndexUpTo n (segments : FlowSegment array) (point : Point2d) =
    segments 
 |> Array.mapi (fun i f -> if i<n then (f.getDistanceTo point), i else System.Double.MaxValue, i)
 |> Array.fold1_right min
 |> snd

/// returns the index of the segment from the given array
/// that is closest to the given point
let private computeClosestSegmentIndex (segments : FlowSegment array) (point : Point2d) =
    computeClosestSegmentIndexUpTo segments.Length segments point
 //   segments 
 //|> Array.mapi (fun i f -> (f.getDistanceTo point), i )
 //|> Array.fold1_right min
 //|> snd
  
/// returns the index of the segment from the given array
/// that is closest to the given punch
let private compute_punch2segmentIndex (segments : FlowSegment array) (punch : Punch) =
    computeClosestSegmentIndex segments punch.Center
    
/// returns an array indexed by the segments
/// for each segment, the value is a list of indices of the punches closest to that segment
let private compute_segment2punchIndices (segments : FlowSegment array) (punches : Punch array) =
    let pi2si = compute_punch2segmentIndex segments << fun (pi) -> (punches.[pi])
    let si2pis = Array.create segments.Length []
    Seq.iter (fun (pi) -> let si = pi2si pi
                          si2pis.[si] <- pi::si2pis.[si]) 
             {0..(punches.Length-1)}
    si2pis

let private computeFlowIntersectionPoints (segments : FlowSegment array) =
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
         
let private computeAllNodes (punches : Punch array) table =
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
    dictionaryOfIndexValues2arrayOfKeys (n,map), map
    
let private computeAllEdges (segments : FlowSegment array) (punches : Punch array) table =
    let addPunch si =
        let f = segments.[si]
        fun nodes pi ->
            let center = punches.[pi].Center
            if f.Segment.StartPoint.GetDistanceTo(center) < f.Segment.EndPoint.GetDistanceTo(center)
            then center :: nodes
            else List.append nodes [center]
    let punchesOfSegment =
        let segment2punchIndices = compute_segment2punchIndices segments punches
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
             
let private compute_node2edges n edge2flowSegment point2node =
    edge2flowSegment
 |> Array.mapi (fun e (f : FlowSegment) -> 
                    let s = point2node.[f.Segment.StartPoint]
                    let t = point2node.[f.Segment.EndPoint]
                    [(s,e);(t,e)])
 |> Seq.concat
 |> arraySetOfSeq n

let private compute_node2props propFun (edge2flowSegment : FlowSegment array) point2node node2edges =
    let edge2prop s e =
        let f = edge2flowSegment.[e]
        let a,b = point2node.[f.Segment.StartPoint], point2node.[f.Segment.EndPoint]
        let t = if a=s then b else a
        propFun s t e
    node2edges
 |> Array.mapi (fun s es -> es |> Set.map (edge2prop s))
 
let private compute_node2neighbors edge2flowSegment point2node node2edges =
    compute_node2props (fun s t e -> t) edge2flowSegment point2node node2edges

type IFlowRepresentation =
    inherit IGrid
    abstract EdgeCount : int
    abstract ToFlowSegment : int -> FlowSegment
    abstract ClosestEdge : Point2d -> int
    abstract NodeEdges : int -> Set<int>

let flowRepresentation (flow : Flow) =
    let intersectionTable = computeFlowIntersectionPoints flow.Segments
    let node2point, point2node = computeAllNodes flow.Punches intersectionTable
    let edge2flowSegment = computeAllEdges flow.Segments flow.Punches intersectionTable
    let node2edges = compute_node2edges node2point.Length edge2flowSegment point2node
    let node2neighbors = compute_node2neighbors edge2flowSegment point2node node2edges
    let punchCount = flow.Punches.Length
    { new IFlowRepresentation with
        /// the first flow.Punches.Length nodes are punch nodes, s.t.
        /// the ith punch maps to the ith node
        member v.NodeCount = node2point.Length
        member v.Neighbors node = Set.to_seq node2neighbors.[node]
        member v.ToPoint node = node2point.[node]
        member v.EdgeCount = edge2flowSegment.Length
        member v.ToFlowSegment edge = edge2flowSegment.[edge] 
        member v.ClosestEdge point = computeClosestSegmentIndex edge2flowSegment point // could be optimized
        member v.NodeEdges node = node2edges.[node]
    }
    
let addValvesToFlowRepresentation (valves : Valve array) (rep : IFlowRepresentation) =
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
        let e = computeClosestSegmentIndexUpTo e' edge2flowSegment vc
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
    let node2neighbors = compute_node2neighbors edge2flowSegment point2node node2edges
    { new IFlowRepresentation with
        /// the last valves.Length nodes are the valve nodes, s.t.
        /// the ith valve maps to the (NodeCount-1-i)th node
        member v.NodeCount = nodeCount
        member v.Neighbors node = Set.to_seq node2neighbors.[node]
        member v.ToPoint node = node2point.[node]
        member v.EdgeCount = edgeCount
        member v.ToFlowSegment edge = edge2flowSegment.[edge] 
        member v.ClosestEdge point = computeClosestSegmentIndex edge2flowSegment point // could be optimized
        member v.NodeEdges node = node2edges.[node]
    }
        