#light

/// flow representation
module BioStream.Micado.Core.Flow

open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core
open BioStream.Micado.Bridge

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

let drawSegmentEnds (startPoint, endPoint) =
    Editor.drawVector (Geometry.to3d startPoint) (Geometry.to3d endPoint)

let drawSegment (segment : LineSegment2d) =
    drawSegmentEnds (segment.StartPoint, segment.EndPoint)
                                                            
let midSegmentEnds width (normal : Vector2d) (point : Point2d) =
    let vec = normal.MultiplyBy(width)
    point.Subtract(vec), point.Add(vec)
    
let draw (flow : FlowSegment) =
    let normal = flow.segment.Direction.GetPerpendicularVector().GetNormal()
    let mainSegment = flow.segment.StartPoint, flow.segment.EndPoint
    let widthIndicatorSegments = 
        (List.map (midSegmentEnds (flow.width/2.0) normal)
                  [flow.segment.StartPoint; flow.segment.EndPoint])
    List.iter drawSegmentEnds (mainSegment::widthIndicatorSegments)
    
let from_polyline (polyline : Polyline) =
    let convertible (polyline : Polyline) =
        polyline.IsOnlyLines && polyline.Closed && (polyline.NumberOfVertices = 4)
    let convert (polyline : Polyline) =
        let segs = [|0..3|] |> Array.map polyline.GetLineSegment2dAt
        let lens = segs |> Array.map (fun (x : LineSegment2d) -> x.Length)
        let makeFlow ia ib = 
            { width = (max lens.[ia] lens.[ib]) ; 
              segment = new LineSegment2d(segs.[ia].MidPoint, segs.[ib].MidPoint) }
        let minLen ia ib = min lens.[ia] lens.[ib]
        if minLen 0 2 < minLen 1 3
        then makeFlow 0 2 
        else makeFlow 1 3
    if not (convertible polyline)
    then None
    else Some (convert polyline)