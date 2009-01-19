#light

/// Conversion from polylines to flows
module BioStream.Micado.Core.Flow

open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.Geometry

/// converts a polyline to a flow segment
/// which is only possible if the polyline has four sides and is closed
let from_polyline (polyline : Polyline) =
    let convertible (polyline : Polyline) =
        polyline.IsOnlyLines && polyline.Closed && (polyline.NumberOfVertices = 4)
    let convert (polyline : Polyline) =
        let segs = Array.init 4 polyline.GetLineSegment2dAt
        let lens = segs |> Array.map (fun (x : LineSegment2d) -> x.Length)
        let makeFlow ia ib =
              new FlowSegment (new LineSegment2d(segs.[ia].MidPoint, segs.[ib].MidPoint), 
                               (max lens.[ia] lens.[ib]), 
                               polyline)
        let minLen ia ib = min lens.[ia] lens.[ib]
        if minLen 0 2 < minLen 1 3
        then makeFlow 0 2 
        else makeFlow 1 3
    if not (convertible polyline)
    then None
    else Some (convert polyline)