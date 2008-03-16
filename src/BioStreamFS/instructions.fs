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
/// that is closest to the given punch
let private compute_punch2segmentIndex (segments : FlowSegment array) (punch : Punch) =
    let point = punch.Center
    segments 
 |> Array.mapi (fun i f -> (f.getDistanceTo point), i )
 |> Array.fold1_right min
 |> snd

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
                          for Some p in [table.[i,j]] do
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
            for Some p in [table.[si,sj]] do
                yield p ]
     |> List.sort segments.[si].PointComparisonFunction
    let nodesOfSegment si =
        List.fold_left (addPunch si) (pointsOfSegment si) (punchesOfSegment si)
    let edgesOfSegment si =
        let f = segments.[si]
        nodesOfSegment si
     |> Seq.pairwise
     |> Seq.map (fun (a,b) -> {Segment = new LineSegment2d(a,b); Width = f.Width})
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

/// given a sequence of index,value pairs
/// where each index may appear multiple times
/// constructs an indexed array with all the values for each index (as a list)
let arrayListOfSeq n seq =
    let a = Array.create n []
    let addEntry (index,value) =
        a.[index] <- value::a.[index]
    seq |> Seq.iter addEntry
    a
         
let private compute_node2edges n edge2flowSegment point2node =
    edge2flowSegment
 |> Array.mapi (fun e f -> 
                    let s = point2node.[f.Segment.StartPoint]
                    let t = point2node.[f.Segment.EndPoint]
                    [(s,e);(t,e)])
 |> Seq.concat
 |> arrayListOfSeq n

let private compute_node2props propFun (edge2flowSegment : FlowSegment array) point2node node2edges =
    let edge2prop s e =
        let f = edge2flowSegment.[e]
        let a,b = point2node.[f.Segment.StartPoint], point2node.[f.Segment.EndPoint]
        let t = if a=s then b else a
        propFun s t e
    node2edges
 |> Array.mapi (fun s es -> es |> List.map (edge2prop s))
 
let private compute_node2neighbors edge2flowSegment point2node node2edges =
    compute_node2props (fun s t e -> t) edge2flowSegment point2node node2edges
 
type IFlowRepresentation =
    inherit IGrid
    abstract EdgeCount : int
    abstract ToFlowSegment : int -> FlowSegment

let flowRepresentation (flow : Flow) =
    let intersectionTable = computeFlowIntersectionPoints flow.Segments
    let node2point, point2node = computeAllNodes flow.Punches intersectionTable
    let edge2flowSegment = computeAllEdges flow.Segments flow.Punches intersectionTable
    let node2edges = compute_node2edges node2point.Length edge2flowSegment point2node
    let node2neighbors = compute_node2neighbors edge2flowSegment point2node node2edges
    { new IFlowRepresentation with
        member v.NodeCount = node2point.Length
        member v.Neighbors node = Seq.of_list node2neighbors.[node]
        member v.ToPoint node = node2point.[node]
        member v.EdgeCount = edge2flowSegment.Length
        member v.ToFlowSegment edge = edge2flowSegment.[edge] }
    
    