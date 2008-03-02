#light

module BioStream.Micado.Core.Creation

open BioStream
open BioStream.Micado.User
open BioStream.Micado.Core
open Autodesk.AutoCAD.Geometry

let valve flowSegment point =
    new Valve()

let punch (centerPoint : Point2d) =
    let punchBarNumber = Settings.PunchBarNumber
    let punchBarWidth = Settings.PunchBarWidth
    let punchRadius = Settings.PunchRadius
    let barSepAngle = 2.0 * System.Math.PI / float(punchBarNumber)
    let radiusVector = new Vector2d(punchRadius, 0.0)
    let barIndices = [|0..punchBarNumber-1|]
    let tips = Array.map (fun i -> centerPoint.Add(radiusVector.RotateBy(float(i)*barSepAngle))) barIndices
    let punch = new Punch()
    punch.Center <- centerPoint
    let addVertexAt index point = punch.AddVertexAt(index, point, 0.0, punchBarWidth, punchBarWidth)
    Array.iter (fun i -> addVertexAt (2*i) centerPoint; addVertexAt (2*i+1) tips.[i]) barIndices
    punch