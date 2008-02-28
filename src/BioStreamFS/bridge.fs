#light

/// Bridge for core functionalities to access plug-in specific operations
/// enabling alternative implementations (notably for unit testing)
module BioStream.Micado.Bridge

open Autodesk.AutoCAD.Geometry
open BioStream.Micado

type IBridge =
   abstract Editor_writeLine : string -> unit
   abstract Editor_drawVector : Point3d -> Point3d -> unit

let pluginBridge =
    { new IBridge with
        member x.Editor_writeLine message = Plugin.Editor.writeLine message
        member x.Editor_drawVector pointA pointB = Plugin.Editor.drawVector pointA pointB
    }
    
let unitBridge =
    { new IBridge with
        member x.Editor_writeLine message = printfn "%s" message
        member x.Editor_drawVector pointA pointB =
            printfn "drawing line from %s to %s" (pointA.ToString()) (pointB.ToString()) 
    }
    
let mutable bridge = pluginBridge

let UnitMode() = bridge <- unitBridge

module Editor =
    let writeLine message = bridge.Editor_writeLine message
    let drawVector pointA pointB = bridge.Editor_drawVector pointA pointB
