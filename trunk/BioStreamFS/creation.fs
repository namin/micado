#light

/// Creation of Valves and Punches
module BioStream.Micado.Core.Creation

open BioStream
open BioStream.Micado.User
open BioStream.Micado.Core
open BioStream.Micado.Common.Datatypes
open Autodesk.AutoCAD.Geometry

/// creates a hollow valve on the given flow segment as close to the given point as possible
/// using the user settings for its relative width and height
let valve (flowSegment : FlowSegment) (clickedPoint : Point2d) =
    let relativeWidth = Settings.Current.ValveRelativeWidth
    let relativeHeight = Settings.Current.ValveRelativeHeight
    let width = relativeWidth * flowSegment.Width
    let height = relativeHeight * flowSegment.Width
    let centerPoint = flowSegment.Segment.GetClosestPointTo(clickedPoint).Point
    let dWeight = flowSegment.Segment.Direction.GetPerpendicularVector() * width / 2.0
    let dHeight = flowSegment.Segment.Direction * height / 2.0
    let points = Array.permute (Permutation.of_array [| 0; 3; 1; 2|])
                               ([| for vW in [dWeight; dWeight.Negate()] do
                                     for vH in [dHeight; dHeight.Negate()] do
                                       yield centerPoint.Add(vW).Add(vH) |])
    let valve = new Valve()
    valve.Center <- centerPoint
    let addVertex = addVertexTo valve
    Array.iter addVertex points
    valve.Closed <- true
    valve

/// creates a punch centered on the given point
/// using the user settings for its number of bars, bar width and radius
let punch (centerPoint : Point2d) =
    let barNumber = Settings.Current.PunchBarNumber
    let barWidth = Settings.Current.PunchBarWidth
    let radius = Settings.Current.PunchRadius
    let barSepAngle = 2.0 * System.Math.PI / float(barNumber)
    let radiusVector = new Vector2d(radius, 0.0)
    let barIndices = [|0..barNumber-1|]
    let tips = Array.map (fun i -> centerPoint.Add(radiusVector.RotateBy(float(i)*barSepAngle))) barIndices
    let punch = new Punch()
    punch.Center <- centerPoint
    let addVertexAt index point = punch.AddVertexAt(index, point, 0.0, barWidth, barWidth)
    Array.iter (fun i -> addVertexAt (2*i) centerPoint; addVertexAt (2*i+1) tips.[i]) barIndices
    punch