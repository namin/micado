#light
    
module BioStream.Micado.Plugin.Commands

open BioStream.Micado.Plugin
open Autodesk.AutoCAD.Runtime

[<CommandMethod("HelloWorld")>]
let helloWorld() =
    Editor.writeLine "Hello World!"



