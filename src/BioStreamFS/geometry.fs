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
 
    
