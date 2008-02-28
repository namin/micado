#light 

module BioStream.Micado.Plugin.Database

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