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

/// draws a flow segment as a line with the end tips indicating the width
let drawFlowSegment (flow : FlowSegment) =
    let normal = flow.Segment.Direction.GetPerpendicularVector()
    let mainSegment = flow.Segment.StartPoint, flow.Segment.EndPoint
    let widthIndicatorSegments = 
        (List.map (Geometry.midSegmentEnds (flow.Width/2.0) normal)
                  [flow.Segment.StartPoint; flow.Segment.EndPoint])
    List.iter drawSegmentEnds (mainSegment::widthIndicatorSegments)

/// draws an arrow as a line from the first given point to the second point
/// with a little head (proportional to the length of the arrow) 
/// pointing at the second point
let drawArrow (startPoint : Point2d) (endPoint : Point2d) =
    let p = 0.2 // head tip length / segment length
    let a = 30.0 * (System.Math.PI / 180.0) // angle of head tip relative to arrow line
    let segment = new LineSegment2d (startPoint, endPoint)
    drawSegment segment
    let tip = segment.Direction.Negate().MultiplyBy(p)
    let drawHeadTip d = drawSegmentEnds (endPoint, endPoint.Add(tip.RotateBy(float(d)*a)))
    drawHeadTip (+1)
    drawHeadTip (-1)
    
/// draws the grid as arrows connecting each pair of neighbors
let drawGrid ( grid :> Routing.IGrid ) =
    for i in {0..grid.NodeCount-1} do
        let drawTo = drawArrow (grid.ToPoint i)
        for j in grid.Neighbors i do
            drawTo (grid.ToPoint j)