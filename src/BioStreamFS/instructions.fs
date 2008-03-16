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

/// returns the index of the segment from the given array
/// that is closest to the given punch
let private compute_punch2segmentIndex (segments : FlowSegment array) (punch : Punch) =
    let point = punch.Center
    segments 
 |> Array.mapi (fun i f -> (f.getDistanceTo point), i )
 |> Array.fold1_right min
 |> snd
 
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

let map2array (n,map) =
    let a = Array.zero_create n
    Map.iter (fun k i -> a.[i] <- k) map
    a
     
let private computeAllNodes (punches : Punch array) table =
    let addNode (n, map) p =
        match Map.tryfind p map with
        | Some _ -> (n, map)
        | None -> (n+1, Map.add p n map)
    let map = punches |> Array.mapi (fun i punch -> punch.Center, i) |> Map.of_array
    let n = punches.Length
    let nodes = seq { for i in [0..(Array2.length1 table)-1] do
                        for j in [i..(Array2.length2 table)-1] do
                          for Some p in [table.[i,j]] do
                            yield p }
    let n,map = Seq.fold addNode (n,map) nodes
    map2array (n,map), map
    
let private computeAllEdges (segments : FlowSegment array) (punches : Punch array) table punch2segmentIndex =
    let addPunch si =
        let f = segments.[si]
        fun nodes pi ->
            let center = punches.[pi].Center
            if f.Segment.StartPoint.GetDistanceTo(center) < f.Segment.EndPoint.GetDistanceTo(center)
            then center :: nodes
            else List.append nodes [center]
    let punchesOfSegment =
        let pisi = punch2segmentIndex |> Array.mapi (fun pi si -> pi,si) |> List.of_array
        fun (si) ->
            pisi |> List.filter (fun (_,si') -> si' = si) |> List.map fst
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
 
type IFlowRepresentation =
    //inherit Graph.IGraph
    abstract EdgeCount : int
    abstract ToFlowSegment : int -> FlowSegment

let flowRepresentation (flow : Flow) =
    let punch2segmentIndex = flow.Punches |> Array.map (compute_punch2segmentIndex flow.Segments)
    let intersectionTable = computeFlowIntersectionPoints flow.Segments
    let node2point, point2node = computeAllNodes flow.Punches intersectionTable
    let edge2flowSegment = computeAllEdges flow.Segments flow.Punches intersectionTable punch2segmentIndex
    { new IFlowRepresentation with
        member v.EdgeCount = edge2flowSegment.Length
        member v.ToFlowSegment edge = edge2flowSegment.[edge] }
    
    