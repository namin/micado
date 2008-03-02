#light

// debugging routines
module BioStream.Micado.Core.Debug

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
    
let drawFlowSegment (flow : FlowSegment) =
    let normal = flow.Segment.Direction.GetPerpendicularVector().GetNormal()
    let mainSegment = flow.Segment.StartPoint, flow.Segment.EndPoint
    let widthIndicatorSegments = 
        (List.map (midSegmentEnds (flow.Width/2.0) normal)
                  [flow.Segment.StartPoint; flow.Segment.EndPoint])
    List.iter drawSegmentEnds (mainSegment::widthIndicatorSegments)