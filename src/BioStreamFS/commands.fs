#light

module BioStream.Micado

#I @"C:\Program Files\Autodesk\Acade 2008"
#r "acdbmgd.dll"
#r "acmgd.dll"

open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.EditorInput
open Autodesk.AutoCAD.Runtime

[<CommandMethod("HelloWorld")>]
let helloWorld() =
    let editor = Application.DocumentManager.MdiActiveDocument.Editor
    editor.WriteMessage("Hello World!")