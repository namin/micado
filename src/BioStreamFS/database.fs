#light 

/// Access to drawing objects
/// Add-on to Autodesk.AutoCAD.DatabaseServices
module BioStream.Micado.Plugin.Database

open BioStream.Micado.User
open BioStream.Micado.Common.Datatypes

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.ApplicationServices

/// returns the active database
let database() =
    Application.DocumentManager.MdiActiveDocument.Database

/// reads the entity from the active database and returns it
/// @requires entId points to an entity in the active database
let readEntityFromId (entId : ObjectId) =
    let tm = database().TransactionManager
    use myT = tm.StartTransaction()
    let entity = tm.GetObject(entId, OpenMode.ForRead, true)
    myT.Commit()
    entity :?> Entity

/// collects all entities in flow and control layers in the active database
let collectChipEntities () =
    let mutable flowEntities = []
    let mutable controlEntities = []
    let db = database()
    let tm = db.TransactionManager
    use myT = tm.StartTransaction()
    let bt = tm.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tm.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        match tm.GetObject(id, OpenMode.ForRead, true) with
        | :? Entity as ent ->
            if (List.mem ent.Layer Settings.flowLayers)
            then flowEntities <- ent :: flowEntities
            else if (List.mem ent.Layer Settings.controlLayers)
                 then controlEntities <- ent :: controlEntities
                 else ()
        | _ -> ()
    myT.Commit()
    { FlowEntities = flowEntities ; ControlEntities = controlEntities }
    