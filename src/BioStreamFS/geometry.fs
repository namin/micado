#light

/// convenient add-ons on top of Autodesk.AutoCAD.Geometry
module BioStream.Micado.Core.Geometry

open Autodesk.AutoCAD.Geometry

let origin2d =
    new Point2d(0.0, 0.0)
    
let origin3d =
    new Point3d(0.0, 0.0, 0.0)

let zUnitVector =
    new Vector3d(0.0, 0.0, 1.0)
    
let xyPlane =
    new Plane(origin3d, zUnitVector)
    
let to3d (point2d : Point2d) =
    new Point3d(xyPlane, point2d)

let to2d (point3d : Point3d) =
    point3d.Convert2d(xyPlane)

let upVector = new Vector2d(0.0, 1.0)
let rightVector = new Vector2d(1.0, 0.0)
        
/// returns the two points, s.t.,
/// the given point is mid-way in between,
/// the distance between the given point and each returned point is the given width,
/// the direction between the given point and each returned point is either the given normal or its reverse                                                           
/// @requires normal is a normal vector
let midSegmentEnds width (normal : Vector2d) (point : Point2d) =
    let vec = normal.MultiplyBy(width)
    point.Subtract(vec), point.Add(vec)
    
/// converts an angle from radians to an integer degree from 0 to 360
let rad2deg (angle : double) =
    int (System.Math.Round (angle * (360.0 / (2.0 * System.Math.PI)))) % 360

/// given an angle in degrees,
/// returns the same angle normalized to an angle from 0 to 360 degrees    
let canonicalDegree angle =
    angle % 360 |> fun (angle) -> if angle < 0 then (angle+360) else angle

/// whether the third angle is in between the first and second angles.
/// the angles are all in degree
let angleWithin a b angle =
    let a = canonicalDegree a
    let b = canonicalDegree b
    if a>b
    then not (b<=angle && angle<a)
    else a<=angle && angle<b

/// returns four corners of a rectangle made
/// from a line segment of the given width
/// in an order suitable for inserting into a polyline
let rectangleCorners width (segment : LineSegment2d) =
    let normal = segment.Direction.GetPerpendicularVector()
    let s1,s2 = midSegmentEnds width normal segment.StartPoint
    let e1,e2 = midSegmentEnds width normal segment.EndPoint
    [s1;s2;e2;e1]

/// whether the first given point is on the left side 
/// of the segment from the second given point to the third given:
/// returns None if the point is on the segment
let pointOnLeftSide (p : Point2d) (a : Point2d) (b : Point2d) =
    /// constructs a vector from f to t
    let vector (f : Point2d) (t : Point2d) = new Vector2d(t.X - f.X, t.Y - f.Y)//, 0.0)
    let vAB = vector a b
    let vAP = vector a p
    //let vCross = vAB.CrossProduct(vAP)
    let vCrossZ = vAB.X*vAP.Y - vAB.Y*vAP.X
    if vCrossZ = 0.0
    then None
    else Some (vCrossZ > 0.0)

/// whether the given point lies 
/// on the segment from the second given point to the third given
let pointOnSegment (p : Point2d) (a : Point2d) (b : Point2d) =
    let within ac bc pc = (min ac bc) <= pc && pc <= (max ac bc)
    (pointOnLeftSide p a b) = None
 && within a.X b.X p.X
 && within a.Y b.Y p.Y 