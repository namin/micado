#light

module BioStream.Micado.Core.Routing

open BioStream.Micado.Common
open BioStream.Micado.Core.Chip
open BioStream.Micado.User

open Autodesk.AutoCAD.Geometry

type IGrid =
    inherit Graph.IGraph
    abstract ToPoint : int -> Point2d

let deltas = [1;-1]
    
type Grid ( resolution, boundingBox : Point2d * Point2d ) =
    let lowerLeft, upperRight = boundingBox
    let sizeX = upperRight.X - lowerLeft.X
    let sizeY = upperRight.Y - lowerLeft.Y
    let nX = int (System.Math.Ceiling(sizeX / resolution)) + 1
    let nY = int (System.Math.Ceiling(sizeY / resolution)) + 1
    let index2coordinates index =
        let x = index % nX
        let y = (index-x) / nX
        (x,y)
    let coordinates2index (x,y) =
        x+y*nX
    let NeighborCoordinates (x,y) =
        { for d in deltas do
            let x' = x+d
            if 0<=x' && x'<nX
            then yield (x',y)
            let y' = y+d
            if 0<=y' && y'<nY
            then yield (x,y')
        }
    let coordinates2point (x,y) =
        new Point2d (lowerLeft.X+float(x)*resolution, lowerLeft.Y+float(y)*resolution)
    interface IGrid with
        member v.NodeCount = nX*nY
        member v.Neighbors index = Seq.map coordinates2index (NeighborCoordinates (index2coordinates index))
        member v.ToPoint index = index |> index2coordinates |> coordinates2point

let ChipGrid (chip : Chip) =
    Grid (Settings.Resolution, chip.BoundingBox)