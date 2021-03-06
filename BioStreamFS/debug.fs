#light

/// Debugging routines
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
    let mainSegment = flow.Segment
    let widthIndicatorSegments = List.of_array flow.WidthIndicatorSegments
    List.iter drawSegment (mainSegment::widthIndicatorSegments)

/// draws an arrow as a line from the first given point to the second point
/// with a little head (proportional to the length of the arrow) 
/// pointing at the second point
let drawArrow (startPoint : Point2d) (endPoint : Point2d) =
    let p = 0.3 // head tip length / segment length
    let a = 30.0 * (System.Math.PI / 180.0) // angle of head tip relative to arrow line
    let segment = new LineSegment2d (startPoint, endPoint)
    drawSegment segment
    let tip = segment.Direction.Negate().MultiplyBy(p*segment.Length)
    let drawHeadTip d = drawSegmentEnds (endPoint, endPoint.Add(tip.RotateBy(float(d)*a)))
    drawHeadTip (+1)
    drawHeadTip (-1)
    
/// draws the grid as arrows connecting each pair of neighbors
let drawGrid ( grid : #IGrid ) =
    for i in {0..grid.NodeCount-1} do
        let drawTo = drawArrow (grid.ToPoint i)
        for j in grid.Neighbors i do
            drawTo (grid.ToPoint j)

/// draws the point as a cross where each bar has the given length            
let drawPoint length ( point : Point2d ) =
    drawSegmentEnds (Geometry.midSegmentEnds length Geometry.upVector point)
    drawSegmentEnds (Geometry.midSegmentEnds length Geometry.rightVector point)

/// returns the length of the longest segment of the given polyline
let maxSegmentLength (polyline : #Polyline) =
    {0..polyline.NumberOfVertices-1-(if polyline.Closed then 0 else 1)}
 |> Seq.map (fun (i) -> polyline.GetLineSegment2dAt(i).Length)
 |> Seq.reduce min 

let drawExtents (extents : Extents2d) =
    let minPt = extents.MinPoint
    let maxPt = extents.MaxPoint
    let upPt = new Point2d(minPt.X, maxPt.Y)
    let downPt = new Point2d(maxPt.X, minPt.Y)
    Editor.drawVector (minPt |> Geometry.to3d) (maxPt |> Geometry.to3d)
    Editor.drawVector (upPt |> Geometry.to3d) (downPt |> Geometry.to3d)
    
let drawUsed (ic : Instructions.InstructionChip) (u : Instructions.Used) =
    let chip = ic.Chip
    let rep = ic.Representation
    u.Edges 
 |> Set.iter (rep.ToFlowSegment >> drawFlowSegment);
    u.Valves
 |> Set.iter (fun vi -> 
                let valve = chip.ControlLayer.Valves.[vi]
                let node = ic.OfNodeType (Instructions.ValveNode vi) 
                drawPoint (maxSegmentLength valve) (rep.ToPoint node))       
 
let rec drawFlowBox (ic : Instructions.InstructionChip) flowBox =
    let chip = ic.Chip
    let rep = ic.Representation
    let drawUsed = drawUsed ic
    let drawAttachments (a : Instructions.Attachments.Attachments) =
        let aLength = chip.FlowLayer.Segments.[0].Width * 0.8 // so we can still see the white of valves
        (match a.InputAttachment with
        | Some node ->
            Editor.setColor 3 // Green
            drawPoint aLength (rep.ToPoint node)
            Editor.resetColor()
        | None -> ());
        (match a.OutputAttachment with
        | Some node ->
            Editor.setColor 6 // Magenta
            drawPoint aLength (rep.ToPoint node)
            Editor.resetColor()
        | None -> ())
    match flowBox with
    | Instructions.FlowBox.Primitive (a, (u,_)) ->
        drawUsed u
        drawAttachments a
    | Instructions.FlowBox.Extended (a, (u,_), f) ->
        drawFlowBox ic f
        drawUsed u
        drawAttachments a
    | Instructions.FlowBox.Pumping f ->
        drawFlowBox ic f
    | Instructions.FlowBox.Seq (a, fs) | Instructions.FlowBox.And (a, fs) ->
        Seq.iter (drawFlowBox ic) fs
        drawAttachments a
    | Instructions.FlowBox.Or (a, fs, o) ->
        Editor.setColor 2 // Yellow
        Seq.iter (drawFlowBox ic) fs
        drawAttachments a
        Editor.resetColor()