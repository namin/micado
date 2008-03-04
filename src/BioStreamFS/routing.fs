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

type CalculatorGrid (g : SimpleGrid) =
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
    let allCoordinatesInBoundingBox (lr : Point2d) (ur : Point2d) =
        let (lrx, lry) = closestUpperRightCoordinates lr
        let (urx, ury) = closestLowerLeftCoordinates ur
        { for x in [lrx..urx] do
            for y in [lry..ury] do
                yield (x,y)
        }
    member v.surroundingIndices (point : Point2d) =
        point |> closestLowerLeftCoordinates |> surroundingCoordinates |> Seq.map g.coordinates2index
    member v.interiorIndices (polyline :> Polyline) =
        polyline.GeometricExtents
     |> fun (e) -> allCoordinatesInBoundingBox (Geometry.to2d e.MinPoint) (Geometry.to2d e.MaxPoint)
     |> Seq.filter (fun (c) -> c |> g.coordinates2point |> interiorPoint polyline)
     |> Seq.map g.coordinates2index
        
        
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
    let addPunch (n,nodes,edges) (punch : Punch) =
        let indices = c.surroundingIndices punch.Center 
        let nodes' = punch.Center :: nodes
        let edges' = Seq.fold (fun edges fromIndex -> addEdge fromIndex n edges) edges indices
        (n, (n+1, nodes', edges'))
    let addInterior lineIndex edges polyline =
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
    let toPoint index =
        if index < ig.NodeCount
        then ig.ToPoint index
        else nodes.[index-ig.NodeCount]
    let extraNeighbors index =
        match Map.tryfind index edges with
        | None -> Seq.empty
        | Some lst -> Seq.of_list lst
    let neighbors index =
        {yield! extraNeighbors index
         if index < ig.NodeCount
         then yield! ig.Neighbors index
        } 
    interface IGrid with
        member v.NodeCount =  nodeCount
        member v.Neighbors index = neighbors index
        member v.ToPoint index = toPoint index
        
let createChipGrid (chip : Chip) =
    new ChipGrid (chip)