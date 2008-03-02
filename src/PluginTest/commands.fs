#light

#I @"C:\Program Files\Autodesk\Acade 2008"
#r "acdbmgd.dll"
#r "acmgd.dll"

#I @"..\debug\"
#r "biostreamfs.dll"

open Autodesk.AutoCAD.Runtime

open BioStream.Micado.Common
open BioStream.Micado.Core
open BioStream.Micado.Plugin

[<CommandMethod("micado_test_polyline2flow")>]
let test_polyline2flow() =
    Editor.promptSelectPolyline "Select a polyline"
 |> Option.bind (fun poly -> 
                    Flow.from_polyline poly 
                 |> function
                    | None -> Editor.writeLine "The polyline could not be converted to a flow segment."
                              None
                    | s -> s)
 |> Option.map Flow.draw 
 |> ignore
 
[<CommandMethod("micado_test_collectChipEntities")>]
let test_collectChipEntities() =
    let chipEntities = Database.collectChipEntities()
    Editor.writeLine (sprintf "Collected %d flow entities and %d control entities" 
                              chipEntities.FlowEntities.Length
                              chipEntities.ControlEntities.Length)

[<CommandMethod("micado_test_chip")>]                            
let test_chip() =
    let chip = Chip.create (Database.collectChipEntities())
    Editor.writeLine ( sprintf "Flow: %d segments, %d punches" 
                               (chip.FlowLayer.Segments.Length) (chip.FlowLayer.Punches.Length) )
    Editor.writeLine ( sprintf "Control: %d control lines, %d unconnected control lines, %d unconnected punches"
                               (chip.ControlLayer.Lines.Length) (chip.ControlLayer.UnconnectedLines.Length) (chip.ControlLayer.UnconnectedPunches.Length))
    