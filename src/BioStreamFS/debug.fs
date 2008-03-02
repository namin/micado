#light

// debugging routines
module BioStream.Micado.Core.Debug

open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core
open BioStream.Micado.Bridge

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

/// draws a line segment from given start point to the given end point
let drawSegmentEnds (startPoint, endPoint) =
    Editor.drawVector (Geometry.to3d startPoint) (Geometry.to3d endPoint)

/// draws a segment as line
let drawSegment (segment : LineSegment2d) =
    drawSegmentEnds (segment.StartPoint, segment.EndPoint)

/// returns the two points, s.t.,
/// the given point is mid-way in between,
/// the distance between the given point and each returned point is the given width,
/// the direction between the given point and each returned point is either the given normal or its reverse                                                           
/// @requires normal is a normal vector
let midSegmentEnds width (normal : Vector2d) (point : Point2d) =
    let vec = normal.MultiplyBy(width)
    point.Subtract(vec), point.Add(vec)

/// draws a flow segment as a line with the end tips indicating the width
let drawFlowSegment (flow : FlowSegment) =
    let normal = flow.Segment.Direction.GetPerpendicularVector().GetNormal()
    let mainSegment = flow.Segment.StartPoint, flow.Segment.EndPoint
    let widthIndicatorSegments = 
        (List.map (midSegmentEnds (flow.Width/2.0) normal)
                  [flow.Segment.StartPoint; flow.Segment.EndPoint])
    List.iter drawSegmentEnds (mainSegment::widthIndicatorSegments)