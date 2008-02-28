#light 

module BioStream.Micado.Plugin.Editor

open BioStream.Micado.Plugin
open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput

/// returns the active editor
let editor() =
    Application.DocumentManager.MdiActiveDocument.Editor

/// writes the given message to the active command line
let writeLine message =
    editor().WriteMessage(message ^ "\n")
        
/// prompts the user to select an entity
/// returns the selected entity if user complies
let promptSelectEntity message =
    let promptForEntity =
        try
            editor().GetEntity(new PromptEntityOptions(message))
        with _ -> null
    let idIfValid (ent : PromptEntityResult) =
        if ent = null
           || ent.Status = PromptStatus.Error || ent.ObjectId.IsNull || not ent.ObjectId.IsValid
        then
           if ent.Status <> PromptStatus.Cancel
           then writeLine "You did not select an entity.";
           None
        else
           Some ent.ObjectId
    promptForEntity |> idIfValid |>  Option.map Database.readEntityFromId
    
/// prompts the user to select a polyline
/// returns the selected polyline if the user complies
let promptSelectPolyline message =
    let justPolyline (entity : Entity) =
        match entity with
        | :? Polyline as polyline -> Some polyline
        | _ -> editor().WriteMessage("Selected entity is not a polyline.")
               None
    promptSelectEntity message |> Option.bind justPolyline
    