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

/// returns the name of the current layer
let currentLayer() =
    let layerId = HostApplicationServices.WorkingDatabase.Clayer
    use tr = database().TransactionManager.StartTransaction()
    let layerRecord = tr.GetObject(layerId, OpenMode.ForRead, true) :?> LayerTableRecord
    tr.Commit()
    layerRecord.Name
        
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
    use bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    use btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    let mutable result = None
    for id in btr do
        if Option.is_none result
        then match tr.GetObject(id, OpenMode.ForRead, true) with
             | :? Entity as entity when entity.Handle.Value = handle ->
                result <- Some entity
             | dbObject ->
                dbObject.Dispose()
    tr.Commit()
    result
            
/// writes the entity to the active database
/// returns the entity object id
/// (the original entity is disposed of)
let writeEntity (entity : #Entity) =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use docLock = doc().LockDocument()
    use bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    use btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectId = btr.AppendEntity(entity)
    tr.AddNewlyCreatedDBObject(entity, true)
    tr.Commit()
    entity.Dispose()
    objectId //|> readEntityFromId

/// writes the entity to the active database
/// returns the entity as freshly read from the database
/// (the original entity is disposed of)
let writeEntityAndReturn entity =
    writeEntity entity |> readEntityFromId
    
/// writes all the given entities to the active database
/// returning a sequence of the entities object id
/// (the original entities are disposed of)
let writeEntities entities =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use docLock = doc().LockDocument()
    use bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    use btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) :?> BlockTableRecord
    let objectIds = [ for entity in entities do
                        yield btr.AppendEntity(entity)
                        do tr.AddNewlyCreatedDBObject(entity, true) ]
    tr.Commit()
    entities |> Seq.iter (fun e -> e.Dispose())
    objectIds //|> Seq.map readEntityFromId

/// writes all the given entities to the active database
/// returning a sequence of the entities as freshly read from the database
/// (the original entities are disposed of)
let writeEntitiesAndReturn entities =
    writeEntities entities |> Seq.map readEntityFromId
    
/// erases an entity from the active database    
let eraseEntity (entity : #Entity) =
    use tr = database().TransactionManager.StartTransaction()
    if entity.IsWriteEnabled 
    then entity.Erase() 
    else use entity' = tr.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
         entity'.Erase()
    tr.Commit()

/// erases all the given entities from the active database    
let eraseEntities entities =
    use tr = database().TransactionManager.StartTransaction()
    for entity in entities do
        let entity = entity :> Entity
        if entity.IsWriteEnabled
        then entity.Erase()
        else use entity' = tr.GetObject(entity.ObjectId, OpenMode.ForWrite, true)
             entity'.Erase()
    tr.Commit()
    
/// collects all objects in database satisfying the given predicate
let collect f =
    let mutable objects = []
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    use btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        let dbObject = tr.GetObject(id, OpenMode.ForRead, true)
        if (f dbObject)
        then objects <- dbObject :: objects
        else dbObject.Dispose()
    tr.Commit()
    objects
    
/// collects all entities in flow and control layers in the active database
let collectChipEntities () =
    let mutable flowEntities = []
    let mutable controlEntities = []
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    use bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) :?> BlockTable
    use btr = tr.GetObject(bt.[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) :?> BlockTableRecord
    for id in btr do
        match tr.GetObject(id, OpenMode.ForRead, true) with
        | :? Entity as ent ->
            let isEntLayer layer = (ent.Layer = layer)
            if (Array.exists isEntLayer Settings.Current.FlowLayers)
            then flowEntities <- ent :: flowEntities
            else if (Array.exists isEntLayer Settings.Current.ControlLayers)
                 then controlEntities <- ent :: controlEntities
                 else ent.Dispose()
        | dbObject -> dbObject.Dispose()
    tr.Commit()
    new ChipEntities(flowEntities, controlEntities)

/// Registered Developer Symbol for Micado    
let rds = "MIDO"

/// Gets the id of a nested dictionary starting with the given dictionary id.
/// May optionally creates the dictionaries if they don't exist.
/// The lookup sequence is: read given dictionary -> search for rds dictionary -> search for app dictionary.
let getDictionaryId createIfNotExisting app dictId =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let readDict id = tr.GetObject(id, OpenMode.ForRead) :?> DBDictionary
    let createNestedDict key (outerDict : DBDictionary, upgradeOpen) =
        let nestedDict = new DBDictionary()
        if upgradeOpen then outerDict.UpgradeOpen()
        outerDict.SetAt(key, nestedDict) |> ignore
        tr.AddNewlyCreatedDBObject(nestedDict, true)
        nestedDict
    let readNestedDict key (outerDict : DBDictionary) = readDict (outerDict.GetAt(key))
    let getNestedDict (key : string) (outerDict : DBDictionary, upgradeOpen) =
        match outerDict.Contains(key), createIfNotExisting with
        | false, false -> None
        | false, true -> Some (createNestedDict key (outerDict,upgradeOpen), false)
        | true, _ -> Some (readNestedDict key outerDict,true)
    let appDictId = (readDict dictId, true)
                 |> getNestedDict rds
                 |> Option.bind (getNestedDict app)
                 |> Option.map fst
                 |> Option.map (fun dict -> dict.ObjectId)
    tr.Commit()
    appDictId

/// Gets the id of a nested dictionary starting with the named objects dictionary.
let getNamedObjectsDictionaryId createIfNotExisting app =
    getDictionaryId createIfNotExisting app (database().NamedObjectsDictionaryId)
        
/// Gets the id of a nested dictionary starting with the extension dictionary for the given object.
let getExtensionDictionaryId createIfNotExisting app (dbObject : #DBObject) =
    match dbObject.ExtensionDictionary <> ObjectId.Null, createIfNotExisting with
    | true, _ -> getDictionaryId createIfNotExisting app (dbObject.ExtensionDictionary)
    | false, false -> None
    | false, true ->
        let db = database()
        use tr = db.TransactionManager.StartTransaction()
        let dbObjectW = tr.GetObject(dbObject.ObjectId, OpenMode.ForWrite, true)
        dbObjectW.CreateExtensionDictionary()
        tr.Commit()
        getDictionaryId createIfNotExisting app (dbObjectW.ExtensionDictionary)

/// Writes the given buffer in an Xrecord under the given key in the given dictionary.
let writeDictionaryEntry dictId (key : string) rb =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let dict = tr.GetObject(dictId, OpenMode.ForWrite) :?> DBDictionary
    let xrec, xrecIsNew =
        match dict.Contains(key) with
        | true ->
            let obj = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite)
            match obj with
            | :? Xrecord as xrec -> xrec, false
            | _ -> 
                // Since we only store XRecords here, this shouldn't happen.
                obj.Erase()
                new Xrecord(), true
       | false -> new Xrecord(), true
    xrec.XlateReferences <- true
    xrec.Data <- rb
    if xrecIsNew then
        dict.SetAt(key, xrec) |> ignore
        tr.AddNewlyCreatedDBObject(xrec, true)
    tr.Commit()

/// Returns a map from key to entry (where an entry is read using the given reader) under the given dictionary.
/// An entry may be skipped if it's not an Xrecord or if the reader returns none, in which case the given skipper is notified.   
let readDictionary skipper dictId reader =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let dict = tr.GetObject(dictId, OpenMode.ForRead) :?> DBDictionary
    let mutable map = Map.empty
    for kv in dict do
        let key = kv.Key
        match tr.GetObject(kv.Value, OpenMode.ForRead) with
        | :? Xrecord as xrec ->
            match reader tr key (xrec.Data.AsArray()) with
            | None -> skipper key
            | Some value -> map <- Map.add key value map
        | _ -> skipper key
    tr.Commit()
    map

/// Reads one dictionary entry under the given dictionary.
let readDictionaryEntry dictId reader (key : string) =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let dict = tr.GetObject(dictId, OpenMode.ForRead) :?> DBDictionary
    let result = 
        if not (dict.Contains(key))
        then None
        else
        match tr.GetObject(dict.GetAt(key), OpenMode.ForRead) with
        | :? Xrecord as xrec -> reader tr key (xrec.Data.AsArray())
        | _ -> None
    tr.Commit()
    result
    
/// Deletes the entries associated with the given keys in the given dictionary.
let deleteDictionaryEntries dictId keys =
    let db = database()
    use tr = db.TransactionManager.StartTransaction()
    let dict = tr.GetObject(dictId, OpenMode.ForWrite) :?> DBDictionary
    for key in keys do
        let obj = tr.GetObject(dict.GetAt(key),OpenMode.ForWrite)
        obj.Erase();
    tr.Commit()