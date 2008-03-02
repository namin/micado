#light

/// Bridge for core functionalities to access plug-in specific operations
/// enabling alternative implementations (notably for unit testing)
module BioStream.Micado.Bridge

open Autodesk.AutoCAD.Geometry
open BioStream.Micado

/// IBridge defines all the bridge functionality
type IBridge =
   abstract Editor_writeLine : string -> unit
   abstract Editor_drawVector : Point3d -> Point3d -> unit

/// the actual functionality
let pluginBridge =
    { new IBridge with
        member x.Editor_writeLine message = Plugin.Editor.writeLine message
        member x.Editor_drawVector pointA pointB = Plugin.Editor.drawVector pointA pointB
    }

/// the mock functionality to be used for unit testing    
let unitBridge =
    { new IBridge with
        member x.Editor_writeLine message = printfn "%s" message
        member x.Editor_drawVector pointA pointB =
            printfn "drawing line from %s to %s" (pointA.ToString()) (pointB.ToString()) 
    }
    
let mutable bridge = pluginBridge

/// switch to the mock functionality
let UnitMode() = bridge <- unitBridge

/// Bridge Editor
module Editor =
    /// writes a message to the prompt
    let writeLine message = bridge.Editor_writeLine message
    /// draws a line segment from pthe first given point to the second given point
    let drawVector pointA pointB = bridge.Editor_drawVector pointA pointB
