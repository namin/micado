#light 

/// Access to drawing editor (user input + output)
/// Add-on to Autodesk.AutoCAD.EditorInput
module BioStream.Micado.Plugin.Editor

open BioStream.Micado.Plugin
open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput

open Autodesk.AutoCAD.Geometry
open BioStream.Micado.Core

open BioStream

/// returns the active editor
let editor() =
    Application.DocumentManager.MdiActiveDocument.Editor

/// white
let private defaultColor = 7
/// no highlighting
let private defaultHighlight = false
let mutable private color = [defaultColor]
let mutable private highlight = [defaultHighlight]

/// sets the color for ephemeral drawings
/// 1 Red
/// 2 Yellow
/// 3 Green
/// 4 Cyan
/// 5 Blue
/// 6 Magenta
/// 7 White or Black
let setColor c = color <- c :: color
/// sets whether ephemeral drawings should be highlighted (true) or not (false)
let setHighlight h = highlight <- h :: highlight

/// returns the tail of the list unless there's only one element
let pop1 lst = 
    match lst with
    | [] | [_] -> lst
    | a::rest -> rest
    
/// resets the color for ephemeral drawings to the previous or default value
let resetColor () = color <- pop1 color
/// resets whether ephemeral drawings should be highlighted or not, to the previous or default value
let resetHighlight () = highlight <- pop1 highlight

/// draws an ephemeral segment connecting the given points, in the active drawing,
let drawVector (pointA : Point3d) (pointB : Point3d) =
    editor().DrawVector( pointA, 
                         pointB, 
                         List.hd color,
                         List.hd highlight )

/// clear the ephemeral marks on the screen
let clearMarks() =
    BioStream.Micado.User.Commands.ClearMarks()
                
/// writes the given message to the active command line
let writeLine message =
    editor().WriteMessage(message ^ "\n")
    |> ignore

/// prompts the user to answer yes or not:
/// returns true if 'yes' and false if 'no',
/// the boolean parameter is the default similarly coded yes/no value
let promptYesOrNo defaultYes message =
    let options = new PromptKeywordOptions(message)
    options.Keywords.Add("yes")
    options.Keywords.Add("no")
    options.AllowArbitraryInput <- true
    let prompt =
        try
            editor().GetKeywords(options)
        with _ -> null
    let promptPartial x =
        prompt.StringResult.StartsWith(x)
    let promptNot x y =
        prompt = null || (not (promptPartial x) && not (promptPartial y))
    match defaultYes with
    | true -> promptNot "n" "N"
    | false -> promptNot "y" "Y"

/// prompts the user to select an entity
/// returns a tuple of the selected entity and the picked point if the user complies
let promptSelectEntityAndPoint message =
    let promptForEntity =
        try
            editor().GetEntity(new PromptEntityOptions(message))
        with _ -> null
    let ifValid (res : PromptEntityResult) =
        if res = null
           || res.Status = PromptStatus.Error || res.ObjectId.IsNull || not res.ObjectId.IsValid
        then
           writeLine "You did not select an entity.";
           None
        else
        if res.Status = PromptStatus.Cancel
        then None
        else Some (Database.readEntityFromId res.ObjectId, res.PickedPoint)
    promptForEntity |> ifValid
    
/// prompts the user to select an entity
/// returns the selected entity if user complies
let promptSelectEntity message =
    promptSelectEntityAndPoint message
 |> Option.map (function | (entity, point) -> entity)

/// returns the entity as a polyline if it's possible
let justPolyline (entity : Entity) =
    match entity with
    | :? Polyline as polyline -> Some polyline
    | _ -> editor().WriteMessage("Selected entity is not a polyline.")
           None

/// prompts the user to select a polyline
/// returns a tuple of the selected polyline and the picked point if the user complies
let promptSelectPolylineAndPoint message =
    promptSelectEntityAndPoint message
 |> Option.bind (function | (entity, point) -> justPolyline entity |> Option.map (fun poly -> (poly, point)))
 
/// prompts the user to select a polyline
/// returns the selected polyline if the user complies
let promptSelectPolyline message =
    promptSelectEntity message |> Option.bind justPolyline

/// returns the entity as a punch if it's possible
let justPunch (entity : Entity) =
    match entity with
    | :? Punch as punch -> Some punch
    | _ -> editor().WriteMessage("Selected entity is not a punch.")
           None
/// prompts the user to select a punch
/// returns the selected punch if the user complies
let promptSelectPunch message =
    promptSelectEntity message |> Option.bind justPunch
               
/// converts the polyline to a flow segment if possible
let justFlowSegment (polyline : Polyline) =
    Flow.from_polyline polyline
    |> function
       | None -> writeLine "The selected polyline could not be converted to a flow segment."
                 None
       | s -> s
                    
/// prompts the user to select a flow segment
/// returns the selected segment and the picked point if the user complies
let promptSelectFlowSegmentAndPoint message =
    promptSelectPolylineAndPoint message
 |> Option.bind (function | (polyline, point) -> justFlowSegment polyline |> Option.map (fun flow -> (flow, point)))

/// prompts the user to select a flow segment
/// returns the selected segment if the user complies
let promptSelectFlowSegment message =
    promptSelectPolyline message
 |> Option.bind justFlowSegment
 
/// prompts the user to select a point
/// returns the selected point if the user complies
let promptPoint message =
    let promptForPoint =
        try
            editor().GetPoint(new PromptPointOptions(message))
        with _ -> null
    let pointIfValid (res : PromptPointResult) =
        if res = null
           || res.Status = PromptStatus.Error
        then
           writeLine "You did not select a point.";
           None
        else
        if res.Status = PromptStatus.Cancel
        then None
        else Some res.Value
    promptForPoint |> pointIfValid

let stringIfValid (res : PromptResult) =
    if res = null || res.Status = PromptStatus.Error
    then writeLine "You did not enter a valid identifier.";
         None
    else
    if res.Status = PromptStatus.Cancel
    then None
    else Some res.StringResult

/// prompts the user to write an identifier name
/// rethrns the user-given name if the user complies
let promptIdName message =
    let promptFor =
        let opts = new PromptKeywordOptions(message)
        opts.AllowArbitraryInput <- true
        try
            editor().GetKeywords(opts)
        with _ -> null
    let maybeTruncate (name : string) =
        let i = name.IndexOf('_')
        if i = -1
        then name
        else
        writeLine "Character _ not allowed in identifier name. Truncating..."
        name.Substring(0,i)
    promptFor |> stringIfValid |> Option.map maybeTruncate

/// prompts the user to select a name from the set
/// returns the selected name if the user complies
let promptSelectIdName message keywords  =
    // the prompt doesn't allow me to select the keyword with the mouse
    // nor is auto-complete enabled
    let promptFor =
        //let opts = new PromptKeywordOptions(message)
        //opts.AllowArbitraryInput <- false
        //Seq.iter (opts.Keywords.Add) keywords
        //Seq.iter (fun (kw) -> opts.Keywords.Add(kw, kw, kw, true, true)) keywords
        try
            //editor().GetKeywords(opts)
            editor().GetKeywords(message, keywords)
        with _ -> null
    promptFor |> stringIfValid
                
module Extra =

    open BioStream.Micado.Common.Datatypes

    module Augmentations =
        let promptFlowPunch (flowLayer : Flow) =
            let justFlowPunch punch =
                match flowLayer.Punch2Index punch with
                | None -> writeLine "The selected punch is not a flow punch."
                          None    
                | s -> s        
            fun message ->
                promptSelectPunch message
             |> Option.bind justFlowPunch

    type BioStream.Micado.Common.Datatypes.Flow with
        member v.promptPunch message = Augmentations.promptFlowPunch v message