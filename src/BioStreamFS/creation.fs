#light

module BioStream.Micado.Core.Creation

open BioStream
open BioStream.Micado.User
open BioStream.Micado.Core
open BioStream.Micado.Common.Datatypes
open Autodesk.AutoCAD.Geometry

let valve (flowSegment : FlowSegment) (clickedPoint : Point2d) =
    let relativeWidth = Settings.ValveRelativeWidth
    let relativeHeight = Settings.ValveRelativeHeight
    let width = relativeWidth * flowSegment.Width
    let height = relativeHeight * flowSegment.Width
    let centerPoint = flowSegment.Segment.GetClosestPointTo(clickedPoint).Point
    let dWeight = flowSegment.Segment.Direction.GetPerpendicularVector() * width / 2.0
    let dHeight = flowSegment.Segment.Direction * height / 2.0
    let points = Array.permute (new Permutation [| 0; 2; 3; 1|])
                               ([| for vW in [dWeight; dWeight.Negate()]
                                   for vH in [dHeight; dHeight.Negate()]
                                   -> centerPoint.Add(vW).Add(vH) |])
    let valve = new Valve()
    valve.Center <- centerPoint
    let addVertex point = valve.AddVertexAt(valve.NumberOfVertices, point, 0.0, 0.0, 0.0)
    Array.iter addVertex points
    valve.Closed <- true
    valve
     
let punch (centerPoint : Point2d) =
    let barNumber = Settings.PunchBarNumber
    let barWidth = Settings.PunchBarWidth
    let radius = Settings.PunchRadius
    let barSepAngle = 2.0 * System.Math.PI / float(barNumber)
    let radiusVector = new Vector2d(radius, 0.0)
    let barIndices = [|0..barNumber-1|]
    let tips = Array.map (fun i -> centerPoint.Add(radiusVector.RotateBy(float(i)*barSepAngle))) barIndices
    let punch = new Punch()
    punch.Center <- centerPoint
    let addVertexAt index point = punch.AddVertexAt(index, point, 0.0, barWidth, barWidth)
    Array.iter (fun i -> addVertexAt (2*i) centerPoint; addVertexAt (2*i+1) tips.[i]) barIndices
    punch