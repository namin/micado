#light

#I @"C:\Program Files\Autodesk\Acade 2008"
#r "acdbmgd.dll"
#r "acmgd.dll"

#I @"..\debug\"
#r "biostreamfs.dll"

open Autodesk.AutoCAD.Runtime

open BioStream.Micado.Core
open BioStream.Micado.Plugin

[<CommandMethod("micado_test_polyline2flow")>]
let test_polyline2flow() =
    Editor.promptSelectPolyline "Select a polyline" 
 |> Option.bind Flow.from_polyline
 |> Option.map Flow.draw 
 |> ignore