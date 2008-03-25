#light

module BioStream.Micado.Plugin.Export.GUI

open Autodesk.AutoCAD.ApplicationServices;
open Autodesk.AutoCAD.DatabaseServices;
open Autodesk.AutoCAD.EditorInput;
open Autodesk.AutoCAD.Geometry;
open Autodesk.AutoCAD.GraphicsInterface;
open Autodesk.AutoCAD.GraphicsSystem;

open System.Drawing;

open BioStream.Micado.Plugin

let visualStyleType = VisualStyleType.Wireframe2D;

let promptImageFilename() =
    let sfd = new System.Windows.Forms.SaveFileDialog()
    sfd.Filter <- "PNG images (*.png)|*.png"
    sfd.Title <- "Export Snapshot Image"
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