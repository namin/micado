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
    use tr = database().TransactionManager.StartTransaction()
    let entity = tr.GetObject(entId, OpenMode.ForRead, true)
    tr.Commit()
    entity :?> Entity

/// searches for the entity with the given handle in the active database
/// and returns it if found
let readEntityFromHandle handle =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    let search id =
        match tr.GetObject(id, OpenMode.ForRead, true) with
        | :? Entity as ent ->
            if ent.Handle.Value = handle
            then Some ent
            else None
        | _ -> None
    let results = Seq.choose search {for id in btr -> id}
    if Seq.nonempty results
    then Some (Seq.hd results)
    else None
            
/// writes the entity to the active database
/// returns the given entity (for chaining)
let writeEntity (entity : #Entity) =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use docLock = doc().LockDocument()
    let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectId = btr.AppendEntity(entity)
    tr.AddNewlyCreatedDBObject(entity, true)
    tr.Commit()
    objectId |> readEntityFromId

/// writes all the given entities to the active database
/// returning the sequence of written entities (for chaining)
let writeEntities (entities : Entity seq) =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use docLock = doc().LockDocument()
    let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectIds = [ for entity in entities do
                        yield btr.AppendEntity(entity)
                        do tr.AddNewlyCreatedDBObject(entity, true) ]
    tr.Commit()
    objectIds |> Seq.map readEntityFromId
        
/// erases an entity from the active database    
let eraseEntity (entity : #Entity) =
    use tr = database().TransactionManager.StartTransaction()
    let entity' = if entity.IsWriteEnabled 
                  then entity :> DBObject 
                  else tr.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
    entity'.Erase()
    tr.Commit()

/// erases all the given entities from the active database    
let eraseEntities (entities : Entity seq) =
    use tr = database().TransactionManager.StartTransaction()
    for entity in entities do
        let entity' = if entity.IsWriteEnabled 
                      then entity :> DBObject 
                      else tr.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
        entity'.Erase()
    tr.Commit()
    
/// collects all objects in database satisfying the given predicate
let collect f =
    let mutable objects = []
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        let dbObject = tr.GetObject(id, OpenMode.ForRead, true)
        if (f dbObject)
        then objects <- dbObject :: objects
    tr.Commit()
    objects
    
/// collects all entities in flow and control layers in the active database
let collectChipEntities () =
    let mutable flowEntities = []
    let mutable controlEntities = []
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    let btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        match tr.GetObject(id, OpenMode.ForRead, true) with
        | :? Entity as ent ->
            let isEntLayer layer = (ent.Layer = layer)
            if (Array.exists isEntLayer Settings.Current.FlowLayers)
            then flowEntities <- ent :: flowEntities
            else if (Array.exists isEntLayer Settings.Current.ControlLayers)
                 then controlEntities <- ent :: controlEntities
                 else ()
        | _ -> ()
    tr.Commit()
    { FlowEntities = flowEntities ; ControlEntities = controlEntities }
    