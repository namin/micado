#light 

/// Access to drawing objects
/// Add-on to Autodesk.AutoCAD.DatabaseServices
module BioStream.Micado.Plugin.Database

open BioStream.Micado.User
open BioStream.Micado.Common.Datatypes

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.ApplicationServices

/// returns the active document
let doc() =
    Application.DocumentManager.MdiActiveDocument
    
/// returns the active database
let database() =
    doc().Database

/// reads the entity from the active database and returns it
/// @requires entId points to an entity in the active database
let readEntityFromId (entId : ObjectId) =
    let tm = database().TransactionManager
    use myT = tm.StartTransaction()
    let entity = tm.GetObject(entId, OpenMode.ForRead, true)
    myT.Commit()
    entity :?> Entity

/// writes the entity to the active database
/// returns the given entity (for chaining)
let writeEntity (entity :> Entity) =
    let db = database()
    let tm = db.TransactionManager
    use myT = tm.StartTransaction()
    use doclock = doc().LockDocument()
    let bt = tm.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tm.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectId = btr.AppendEntity(entity)
    tm.AddNewlyCreatedDBObject(entity, true)
    myT.Commit()
    objectId |> readEntityFromId

/// writes all the given entities to the active database
/// returning the sequence of written entities (for chaining)
let writeEntities (entities : Entity seq) =
    let db = database()
    let tm = db.TransactionManager
    use myT = tm.StartTransaction()
    use doclock = doc().LockDocument()
    let bt = tm.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tm.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectIds = [ for entity in entities do
                        yield btr.AppendEntity(entity)
                        do tm.AddNewlyCreatedDBObject(entity, true) ]
    myT.Commit()
    objectIds |> Seq.map readEntityFromId
        
/// erases an entity from the active database    
let eraseEntity (entity :> Entity) =
    let tm = database().TransactionManager
    use myT = tm.StartTransaction()
    let entity' = if entity.IsWriteEnabled 
                  then entity :> DBObject 
                  else tm.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
    entity'.Erase()
    myT.Commit()

/// erases all the given entities from the active database    
let eraseEntities (entities : Entity seq) =
    let tm = database().TransactionManager
    use myT = tm.StartTransaction()
    for entity in entities do
        let entity' = if entity.IsWriteEnabled 
                      then entity :> DBObject 
                      else tm.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
        entity'.Erase()
    myT.Commit()
    
/// collects all objects in database satisfying the given predicate
let collect f =
    let mutable objects = []
    let db = database()
    let tm = db.TransactionManager
    use myT = tm.StartTransaction()
    let bt = tm.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tm.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        let dbObject = tm.GetObject(id, OpenMode.ForRead, true)
        if (f dbObject)
        then objects <- dbObject :: objects
    myT.Commit()
    objects
    
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
            let isEntLayer layer = (ent.Layer = layer)
            if (Array.exists isEntLayer Settings.Current.FlowLayers)
            then flowEntities <- ent :: flowEntities
            else if (Array.exists isEntLayer Settings.Current.ControlLayers)
                 then controlEntities <- ent :: controlEntities
                 else ()
        | _ -> ()
    myT.Commit()
    { FlowEntities = flowEntities ; ControlEntities = controlEntities }
    