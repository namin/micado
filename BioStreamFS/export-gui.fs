#light

module BioStream.Micado.Plugin.Export.GUI

open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput
open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.GraphicsInterface
open Autodesk.AutoCAD.GraphicsSystem

open System.IO
open System.Drawing

open BioStream.Micado.Plugin
open BioStream.Micado.Core

let visualStyleType = VisualStyleType.Wireframe2D;

let promptImageFilename() =
    let sfd = new System.Windows.Forms.SaveFileDialog()
    sfd.Filter <- "PNG images (*.png)|*.png"
    sfd.Title <- "Export Snapshot Image"
    if sfd.ShowDialog() <> System.Windows.Forms.DialogResult.OK
    then None
    else Some sfd.FileName

let promptJavaDataFilename() =
    let sfd = new System.Windows.Forms.SaveFileDialog()
    sfd.Filter <- "data files (*.dat)|*.dat";
    sfd.Title <- "Export Java Data";
    if sfd.ShowDialog() <> System.Windows.Forms.DialogResult.OK
    then None
    else Some sfd.FileName
            
/// adapted from an entry in the blog Through The Interface:
/// Taking a snapshot of the AutoCAD model (take 2),
/// http://through-the-interface.typepad.com/through_the_interface/2007/04/taking_a_snapsh_1.html
let exportImage imageFilename =
    let doc = Editor.doc()
    let db = doc.Database
    let gsm = doc.GraphicsManager
    let vpn = System.Convert.ToInt32(Application.GetSystemVariable("CVPORT"))
    use view = new View()
    gsm.SetViewFromViewport(view, vpn)
    view.VisualStyle <- new VisualStyle(visualStyleType)
    use dev = gsm.CreateAutoCADOffScreenDevice()
    dev.OnSize(gsm.DisplaySize)
    // Set the render type and the background color
    dev.DeviceRenderType <- RendererType.Default
    dev.BackgroundColor <- Color.White
    // Add the view to the device and update it
    dev.Add(view) |> ignore
    dev.Update()
    use model = gsm.CreateAutoCADModel()
    use tr = db.TransactionManager.StartTransaction() in
        // Add the modelspace to the view
        // It's a container but also a drawable
        let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) :?> BlockTable
        let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead) :?> BlockTableRecord
        view.Add(btr, model) |> ignore;
        tr.Commit()
    use bitmap = view.GetSnapshot(view.Viewport) in
        bitmap.Save(imageFilename)
    Editor.writeLine("Snapshot image saved to " ^ imageFilename ^ ".")
    let rect = view.Viewport
    let mat = view.WorldToDeviceMatrix
    // Clean up
    view.EraseAll()
    dev.Erase(view) |> ignore
    (rect, mat)
    
let exportJavaData (ic : Instructions.InstructionChip) 
                   (instructions : Instructions.Instruction array) 
                   (dataFilename : string) 
                   (rect : Rectangle,mat) =
    let toPixelCoordinates (point : Point2d) =
        let p3 = point |> Geometry.to3d
        let t3 = p3.TransformBy(mat)
        let x = int(System.Math.Round(t3.X))
        let y = int(System.Math.Round(float(rect.Height) - t3.Y))
        "{" ^ x.ToString() ^ "," + y.ToString() ^ "},"
    let toPoints (extents : Extents2d) =
        let corners = Instructions.rectangleCorners extents
        let all = Array.zero_create (corners.Length*2)
        for i = 0 to corners.Length-1 do
            all.[2*i] <- corners.[i]
            all.[2*i+1] <- Geometry.averagePoint corners.[i] corners.[(i+1) % corners.Length]
        all     
    let chip = ic.Chip
    let control = chip.ControlLayer
    let tw = new StreamWriter(dataFilename)
    tw.WriteLine("// rectangle size: " ^ rect.Size.ToString())
    tw.WriteLine("// number of control lines: " ^ control.Lines.Length.ToString())
    tw.WriteLine("// number of instructions: " ^ instructions.Length.ToString())
    (*tw.WriteLine("// Mapping: {" ^
                 System.String.Join(",", 
                                    Array.init control.LineNumbering.Length (control.LineNumbering.Item >> (fun i -> i.ToString()))) ^ 
                 "}")*)
    tw.WriteLine("// BEGIN port locations")
    for ui = 0 to control.Lines.Length-1 do
        tw.WriteLine("// index " ^ (ui+1).ToString())
        tw.WriteLine("{")
        let i = control.LineNumbering.Inverse.[ui]
        for valve in control.Lines.[i].Valves do
            tw.WriteLine(toPixelCoordinates(valve.Center))
        tw.WriteLine("},")
    for i = 0 to instructions.Length-1 do
        tw.WriteLine("// instruction " ^ i.ToString())
        tw.WriteLine("{")
        for pt in toPoints(instructions.[i].Extents) do
            tw.WriteLine(toPixelCoordinates(pt))
        tw.WriteLine("},")
    tw.WriteLine("// END port locations")
    tw.WriteLine("// BEGIN instructions")
    for i = 0 to instructions.Length-1 do
        tw.WriteLine("// instruction " ^ i.ToString())
        tw.WriteLine("// " ^ instructions.[i].Name)
        tw.WriteLine("// " ^ if instructions.[i].Partial then "partial" else "complete")
        tw.Write("{")
        let openLines = control.ToOpenLines(instructions.[i].Used.Valves) |> Array.permute control.LineNumbering
        let stringOpenLines = openLines |> Array.map (fun b -> if b then "true" else "false")
        tw.Write(System.String.Join(",", stringOpenLines))
        tw.WriteLine("},")
    tw.WriteLine("// END instructions")
    tw.WriteLine("// BEGIN instruction pumps")
    instructions |> Array.iteri (fun i _ -> tw.WriteLine("// instruction " ^ i.ToString()); tw.WriteLine("null,"))
    tw.WriteLine("// END instruction pumps")
    tw.Close()
    Editor.writeLine("Java data saved to " ^ dataFilename ^ ".")

let prompt ic instructions =
    match promptImageFilename() with
    | None -> false
    | Some imageFilename ->
        match promptJavaDataFilename() with
        | None -> false
        | Some dataFilename ->
            let imageProps = exportImage imageFilename
            exportJavaData ic instructions dataFilename imageProps
            true