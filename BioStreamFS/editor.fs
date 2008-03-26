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

// returns the active document
let doc() = 
    Application.DocumentManager.MdiActiveDocument

/// returns the active editor
let editor() =
    doc().Editor

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
    options.Keywords.Add("Yes")
    options.Keywords.Add("No")
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
        if res.Status <> PromptStatus.Cancel &&
           (res = null
            || res.Status = PromptStatus.Error || res.ObjectId.IsNull || not res.ObjectId.IsValid)
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
/// returns the user-given name if the user complies
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

/// prompts the user to write an identifier name
/// re-prompts the user if he enters the empty string
/// returns the user-given name if the user complies
let promptIdNameNotEmpty message =
    let mutable res = promptIdName message
    while Option.is_some res && (Option.get res) = "" do
        writeLine "(cannot be empty)"
        res <- promptIdName message
    res
    
/// prompts the user to select a name from the set
/// returns the selected name if the user complies
let promptSelectIdName message keywords  =
    // for each keyword, the first set of continuous uppercase letters have to be unique
    // in order for AutoCAD to recognize it as a shortcut
    // (uniqueness matters for correctness of the mouse menu)
    let seenCuts = ref (Set.empty : Set<string>)
    let notSeen k = not ((!seenCuts).Contains k)
    let addSeen k = seenCuts := (!seenCuts).Add k 
    let seenWords = ref (Set.empty : Set<string>)
    let notConflicts (k : string) =
        let k' = k.ToUpper()
        Set.empty = Set.filter (fun (word : string) -> word.StartsWith k') (!seenWords)
    let addWord (s : string) = seenWords := (!seenWords).Add (s.ToUpper()) 
    let isUpper s =
        String.uppercase(s) = s
    let isUpperChar c =
        Char.uppercase(c) = c
    let capit (s : string) =
        let n = String.length s
        let up i = String.sub s 0 i
        let low i = String.sub s i (n-i)
        let upLow i = String.uppercase (up i), low i
        let k = {n .. -1 .. 1} |> Seq.map up |> Seq.filter isUpper |> Seq.hd
        let u,s' =
            if notSeen k && notConflicts k
            then k, s
            else
            let tries =
                {1..n} |> Seq.filter (fun i -> let u = up i in notSeen u && notConflicts u)
            if not (Seq.nonempty tries)
            then // the word was already seen in its entirety, which means that there are uppercase duplicates
                 let su = s.ToUpper()
                 let extra (i : int) = su ^ i.ToString()
                 let mutable i = 2
                 let mutable s' = extra i
                 while not (notConflicts s') do
                    i <- i + 1
                    s' <- extra i
                 s', s'
            else
            let mutable i = Seq.hd tries
            while i<n && isUpperChar(s.[i]) do
                i <- i+1
            let u,l = upLow i 
            u,u ^ l
        addSeen u
        addWord s'
        s'
    // we need the sorted keywords to ensure that capit always finds a part of the string that is not seen
    // (unless there are uppercase duplicates, in which case, it's hopeless)
    let sortedKeywords = keywords |> Seq.mapi (fun i k -> k,i) |> Seq.to_array
    Array.sort compare sortedKeywords
    let p = new Permutation(sortedKeywords.Length, sortedKeywords |> Array.mapi (fun dst (k,src) -> (src,dst)))
    let capitalizedKeywords = 
        sortedKeywords
     |> Array.map (fst >> String.capitalize >> capit)
     |> Array.permute p
    let originalKeyword kw =
        Seq.zip capitalizedKeywords keywords
     |> Seq.filter (fun (cw,oc) -> kw=cw)
     |> Seq.map snd
     |> Seq.hd
    let promptFor =
        let opts = new PromptKeywordOptions(message)
        opts.AllowArbitraryInput <- false
        Seq.iter opts.Keywords.Add capitalizedKeywords
        try
            editor().GetKeywords(opts)
        with _ -> null
    promptFor |> stringIfValid |> Option.map originalKeyword 
 |> Option.map (fun (w) -> writeLine w; w)
                
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

        let promptLine (controlLayer : Control) message =
            let rec prompt() =
                promptSelectEntity message |> Option.bind justLine
            and justLine entity =
                match controlLayer.searchLines entity with
                | None -> writeLine "The selected entity is not part of a control line."
                          prompt()
                | s -> s
            prompt()
            
        let numberLines (controlLayer : Control) =
            let n = controlLayer.Lines.Length
            let rec acc remaining numbered i =
                if i=n
                then let new2oldA = (Set.choose remaining) :: numbered |> Array.of_list |> Array.rev
                     let new2oldP = new Permutation(new2oldA)
                     let old2newP = new2oldP.Inverse
                     controlLayer.LineNumbering <- old2newP
                     writeLine ("Deducing control line #" ^ i.ToString() ^ ". Numbering completed.")
                     true
                else
                match promptLine controlLayer ("Select control line #" ^ i.ToString() ^ ":") with
                | None -> false
                | Some lineIndex ->
                    if not (remaining.Contains lineIndex)
                    then writeLine "Control line previously selected. Select another one."
                         acc remaining numbered i
                    else writeLine ""
                         acc (Set.remove lineIndex remaining) (lineIndex::numbered) (i+1)
            acc ({0..n-1} |> Set.of_seq) [] 1
            
    type BioStream.Micado.Common.Datatypes.Flow with
        member v.promptPunch message = Augmentations.promptFlowPunch v message
        
    type BioStream.Micado.Common.Datatypes.Control with
        member v.promptLine message = Augmentations.promptLine v message
        member v.numberLines() = Augmentations.numberLines v