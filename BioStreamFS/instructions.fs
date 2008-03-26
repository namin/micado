#light

module BioStream.Micado.Core.Instructions

open BioStream.Micado.Core
open BioStream.Micado.Common
open BioStream.Micado.Common.Datatypes
open BioStream.Micado.Core.Chip
open BioStream

open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.DatabaseServices

open System.Diagnostics

open System.Collections.Generic

type SegmentIndex = int
type NodeIndex = int
type Ordering = HorizontalOrdering | VerticalOrdering

type Used = 
    {Edges : Set<int>; Valves : Set<int> }
    member u1.append (u2 : Used) = 
        {Edges = Set.union u1.Edges u2.Edges;
         Valves = Set.union u1.Valves u2.Valves}
let used edges valves = {Edges = edges; Valves = valves}
let usedEmpty = used Set.empty Set.empty

module Attachments =
    type Kind =
        Complete 
      | Input of NodeIndex
      | Output of NodeIndex 
      | Intermediary of NodeIndex * NodeIndex
        
    type Attachments (inputAttachment : NodeIndex option, outputAttachment : NodeIndex option) =
        
        let kind =
            match inputAttachment, outputAttachment with
            | None, None -> Complete
            | None, Some output -> Input output
            | Some input, None -> Output input
            | Some input, Some output -> Intermediary (input, output)
                            
        member v.InputAttachment = inputAttachment
        member v.OutputAttachment = outputAttachment
        member v.Kind = kind
    
    let create inputAttachment outputAttachment =
        Attachments (inputAttachment, outputAttachment)
        
    let sameKind k1 k2 =
        match k1,k2 with
        | Complete, Complete
        | Input _, Input _
        | Output _, Output _
        | Intermediary _, Intermediary _ -> true
        | _ -> false
            
module FlowBox =
    type Attachments = Attachments.Attachments
    
    type FlowBox =
        Primitive of Attachments * Used
      | Extended of Attachments * Used * FlowBox
      | Or of Attachments * FlowBox array * Ordering
      | And of Attachments * FlowBox array
      | Seq of Attachments * FlowBox array
      
    let attachment flowBox =
        match flowBox with
        | Primitive (a, _)
        | Extended (a, _, _)
        | Or (a, _, _)
        | And (a, _)
        | Seq (a, _) ->
            a

    let attachmentKind flowBox = (attachment flowBox).Kind
                
    let rec mentionedEdgesLax flowBox =
        match flowBox with
        | Primitive (_, u) -> 
            u.Edges
        | Extended (_, u, f) -> 
            //Set.union u.Edges (mentionedEdges f)
            mentionedEdgesLax f
        | Or (_, fs, _) | And (_, fs) | Seq (_, fs) ->
            fs 
         |> Array.map mentionedEdgesLax
         |> Set.Union   

    let rec mentionedUsed flowBox =
        match flowBox with
        | Primitive (_, u) -> 
            u
        | Extended (_, u, f) -> 
            u.append(mentionedUsed f)
        | Or (_, fs, _) | And (_, fs) | Seq (_, fs) ->
            fs 
         |> Seq.map mentionedUsed
         |> Seq.fold1 (fun u u' -> u.append(u'))
        
module InstructionBox =

    type InstructionBox =
        Single of Used
      | Multi of Ordering * InstructionBox array

    let rec appendUsed u instructionBox =
        match instructionBox with
        | Single u' -> 
            Single (u'.append u)
        | Multi (o,bs) -> 
            Multi (o, bs |> Array.map (appendUsed u))

    let rec wrapAround outer inner =
        match outer, inner with
        | Single u, _ ->
            appendUsed u inner
        | _, Single u ->
            appendUsed u outer
        | Multi (out_o, out_bs), _ ->
            let bs = Array.map (fun outer -> wrapAround outer inner) out_bs
            Multi (out_o, bs)
                
    let rec of_FlowBox flowBox =
        match flowBox with
        | FlowBox.Primitive (_, u) -> 
            Single u
        | FlowBox.Extended (_, u, f) ->
            appendUsed u (of_FlowBox f)
        | FlowBox.Or (_, fs, o) ->
            Multi (o, (fs |> Array.map of_FlowBox))
        | FlowBox.And (_, fs) | FlowBox.Seq (_, fs) ->
            fs |> Array.map of_FlowBox |> Array.fold1_right wrapAround

let toExtents2d (entity : Entity) =
    let ext3d = entity.GeometricExtents
    let minPt = ext3d.MinPoint |> Geometry.to2d
    let maxPt = ext3d.MaxPoint |> Geometry.to2d
    new Extents2d(minPt, maxPt)

let rectangleCorners (ext : Extents2d) =
    let minPt = ext.MinPoint
    let maxPt = ext.MaxPoint
    let pts = [|minPt; new Point2d(maxPt.X, minPt.Y); maxPt; new Point2d(minPt.X, maxPt.Y)|]
    pts    

let rectangle (ext : Extents2d) =
    let pts = rectangleCorners ext
    let polyline = new Polyline()
    let addVertex = addVertexTo polyline
    Seq.iter addVertex pts
    polyline.Closed <- true
    polyline
                 
type Instruction (partial : bool, root: string, indices : int array, entity : Entity, used : Used) =
    let prettyIndices =
        if indices.Length=0
        then ""
        else
        "_" ^ (System.String.Join("_", indices |> Array.map (fun i -> i.ToString())))
    let name = root ^ prettyIndices 
    member v.Used = used
    member v.Entity = entity
    member v.Name = name
    member v.Root = root
    member v.Indices = indices
    member v.Extents = toExtents2d entity
    member v.Partial = partial

/// The XML Serialization is loosely inspired by Expert F#, Chapter 9, 
/// section Using XML As a Concrete Language Format (starting p.212)
module Serialization =
    open System.IO
    open System.Xml
    open BioStream.Micado.Plugin // for Database.readEntityFromHandle
    
    let promptSaveFilename() =
        let sfd = new System.Windows.Forms.SaveFileDialog()
        sfd.Filter <- "micado data (*.xml)|*.xml"
        sfd.Title <- "Export Boxes & Instructions"
        if sfd.ShowDialog() <> System.Windows.Forms.DialogResult.OK
        then None
        else Some sfd.FileName

    let promptOpenFilename() =
         let opf = new System.Windows.Forms.OpenFileDialog();
         opf.Filter <- "micado data (*.xml)|*.xml|All files (*.*)|*.*"
         opf.Title <- "Import Boxes & Instructions"
         if opf.ShowDialog() <> System.Windows.Forms.DialogResult.OK
         then None
         else Some opf.FileName
            
    let exportInts (tw : StreamWriter) seq =
        tw.Write("[")
        tw.Write(System.String.Join(";", seq |> Seq.map (fun (i : int) -> i.ToString()) |> Seq.to_array))
        tw.Write("]")
        
    let importInts (text : string) =
        text.Split([|'[';']';';'|]) 
     |> Array.map (fun s -> s.Trim()) 
     |> Array.filter (fun s -> s <> "")
     |> Array.map System.Int32.Parse
    
    let importIntSet = importInts >> Set.of_array
    
    let exportEntity (tw : StreamWriter) (entity : Entity) =
        tw.Write(entity.Handle.Value.ToString())

    let importEntity (text : string) =
        let handle = System.Int64.Parse(text)
        match Database.readEntityFromHandle handle with
        | None -> null
        | Some entity -> entity
    
    let exportPartial (tw : StreamWriter) partial =
        tw.Write(if partial then "partial" else "complete")

    let importPartial (text : string) =
        match text with
        | "partial" -> true
        | "complete" -> false
        | _ -> failwith "invalid instruction partial tag"
                
    let exportInstruction (tw : StreamWriter) (instruction : Instruction) =
        exportPartial tw instruction.Partial
        tw.Write(",")
        tw.Write(instruction.Root)
        tw.Write(",")
        exportInts tw instruction.Indices
        tw.Write(",")
        exportEntity tw instruction.Entity
        tw.Write(",")
        exportInts tw instruction.Used.Edges
        tw.Write(",")
        exportInts tw instruction.Used.Valves

    let importInstruction (text : string) =
        match text.Split([|','|]) with
        | [| partialText; root; indicesText; entityText; edgesText; valvesText |] ->
            let partial = importPartial partialText
            let indices = importInts indicesText
            let entity = importEntity entityText
            let edges = importIntSet edgesText
            let valves = importIntSet valvesText
            new Instruction (partial, root, indices, entity, used edges valves)
        | _ ->
            failwith "invalid instruction format"
    
    let exportInstructions (tw : StreamWriter) instructions =
        instructions |> Seq.iter (exportInstruction tw >> (fun () -> tw.WriteLine("")))
            
    let importInstructions (text : string) =
        text.Split([|'\r';'\n'|]) 
     |> Array.filter (fun s -> s <> "") 
     |> Array.map importInstruction

    let extractIntOption attrName (attribs : XmlAttributeCollection) =
        let item = attribs.GetNamedItem(attrName)
        if item = null
        then None
        else Some (Int32.of_string(item.Value))

    let extractAttachments (attribs : XmlAttributeCollection) =
        let input = extractIntOption "input" attribs
        let output = extractIntOption "output" attribs
        Attachments.create input output
    
    let extractIntSet attrName (attribs : XmlAttributeCollection) =
        importIntSet (attribs.GetNamedItem(attrName).Value)
            
    let extractUsed (attribs : XmlAttributeCollection) =
        let edges = extractIntSet "edges" attribs
        let valves = extractIntSet "valves" attribs
        used edges valves
        
    let extractOrdering (attribs : XmlAttributeCollection) =
        match attribs.GetNamedItem("ordering").Value with
        | "horizontal" -> HorizontalOrdering
        | "vertical" -> VerticalOrdering
        | _ -> failwith "unrecognized ordering"
          
    let rec extractBox (node : XmlNode) =
        let attribs = node.Attributes
        let childNodes = node.ChildNodes
        let attachments = extractAttachments(attribs)
        let childBoxes() =
            [| for child in childNodes -> extractBox(child) |]
        match node.Name with
        | "Primitive" ->
            FlowBox.Primitive(attachments, extractUsed(attribs))
        | "Extended" ->
            FlowBox.Extended(attachments, extractUsed(attribs), extractBox (childNodes.Item(0)))           
        | "Or" ->
            FlowBox.Or(attachments, childBoxes(), extractOrdering(attribs))
        | "And" ->
            FlowBox.And(attachments, childBoxes())
        | "Seq" ->
            FlowBox.Seq(attachments, childBoxes())
        | s ->
            failwith ("unrecognized box type " ^ s)
            
    let extractName (attribs : XmlAttributeCollection) =
        attribs.GetNamedItem("name").Value
    
    let extractBoxes (node : XmlNode) =
        [for node in node.ChildNodes do
            if node.Name = "Box"
            then yield extractName node.Attributes, extractBox (node.ChildNodes.Item(0))
        ]
    
    let extractInstructions (node : XmlNode) =
        importInstructions node.InnerXml
        
    let extractBoxesAndInstructions (doc : XmlNode) =
        let mutable boxes = None
        let mutable instructions = None
        for node in doc.ChildNodes do
            match node.Name with
            | "Boxes" -> boxes <- Some (extractBoxes(node))
            | "Instructions" -> instructions <- Some (extractInstructions(node))
            | _ -> ()
        Option.get boxes, Option.get instructions

    //  Primitive of Attachments * Used
    //| Extended of Attachments * Used * FlowBox
    //| Or of Attachments * FlowBox array * Ordering
    //| And of Attachments * FlowBox array
    //| Seq of Attachments * FlowBox array  
    let rec exportBox (tw : StreamWriter) box =
        let writeAttachment name att =
            if Option.is_some att
            then tw.Write(name ^ "='" ^ ((Option.get att) : int).ToString() ^ "' ")
        let writeAttachments (a : Attachments.Attachments) =
            writeAttachment "input" a.InputAttachment
            writeAttachment "output" a.OutputAttachment
        let writeIntSet name set =
            tw.Write(name ^ "='")
            exportInts tw set
            tw.Write("' ")
        let writeUsed (u : Used) =
            writeIntSet "edges" u.Edges
            writeIntSet "valves" u.Valves
        let writeChildren childBoxes =
            Seq.iter (exportBox tw) childBoxes
        let writeAndSeq tag a childBoxes =
            tw.Write("<" ^ tag ^ " ")
            writeAttachments a
            tw.WriteLine(">")
            writeChildren childBoxes
            tw.WriteLine("</" ^ tag ^ ">")
        match box with
        | FlowBox.Primitive (a,u) ->
            tw.Write("<Primitive ")
            writeAttachments a   
            writeUsed u
            tw.WriteLine("/>")
        | FlowBox.Extended(a, u, childBox) ->
            tw.Write("<Extended ")
            writeAttachments a
            writeUsed u
            tw.WriteLine(">")
            exportBox tw childBox
            tw.WriteLine("</Extended>")         
        | FlowBox.Or(a, childBoxes, ordering) ->
            tw.Write("<Or ")
            writeAttachments a
            tw.Write("ordering='")
            tw.Write(match ordering with
                     | HorizontalOrdering -> "horizontal"
                     | VerticalOrdering -> "vertical")
            tw.Write("'")
            tw.WriteLine(">")
            writeChildren childBoxes
            tw.WriteLine("</Or>")
         | FlowBox.And(a, childBoxes) ->
            writeAndSeq "And" a childBoxes
         | FlowBox.Seq(a, childBoxes) ->
            writeAndSeq "Seq" a childBoxes
            
    let exportNameBox (tw : StreamWriter) (name,box) =
        tw.WriteLine("<Box name='" ^ name ^ "'>")
        exportBox tw box
        tw.WriteLine("</Box>")
                
    let exportBoxes (tw : StreamWriter) boxes =
        boxes |> Seq.iter (exportNameBox tw)
        
    let export boxes instructions =
        match promptSaveFilename() with
        | None -> false
        | Some filename ->
            use tw = new StreamWriter(filename)
            tw.WriteLine("<Micado>")
            tw.WriteLine("<Boxes>")
            exportBoxes tw boxes
            tw.WriteLine("</Boxes>")
            tw.WriteLine("<Instructions>")
            exportInstructions tw instructions
            tw.WriteLine("</Instructions>")
            tw.WriteLine("</Micado>")
            Editor.writeLine ("Micado boxes & instructions exported to " ^ filename ^ ".")
            true
    
    let import() =
        match promptOpenFilename() with
        | None -> None
        | Some filename ->
            let text = use rw = new StreamReader(filename) in rw.ReadToEnd()
            let doc = new XmlDocument()
            doc.LoadXml(text)
            try
                Some (extractBoxesAndInstructions (doc.ChildNodes.Item(0)))
            with
                | Failure(msg) ->
                    Editor.writeLine ("Could not parse file: " ^ msg)
                    None                        
module Convert =
    open BioStream.Micado.Plugin // needed for Database.writeEntity

    let rec to_instructions partial root box (extents : Extents2d) indices =
        match box with
        | InstructionBox.Single used ->
            {yield Instruction(partial, 
                               root, 
                               indices |> List.rev |> Array.of_list, 
                               extents |> rectangle |> Database.writeEntity, 
                               used)}
        | InstructionBox.Multi (ordering, boxes) ->
            let n = boxes.Length
            let varC,fixC,varfix, actualIndex =
                let getX = (fun (pt : Point2d) -> pt.X)
                let getY = (fun (pt : Point2d)-> pt.Y)
                match ordering with
                | HorizontalOrdering ->
                    getX, getY, (fun x y -> new Point2d(x, y)), (fun i -> i)
                | VerticalOrdering ->
                    getY, getX, (fun y x -> new Point2d(x, y)), (fun i -> n-1-i)
            let minPt, maxPt = extents.MinPoint, extents.MaxPoint
            let distance = (varC maxPt) - (varC minPt)
            let sep,one = if n<=1 then 0.0, distance else (0.1*distance)/float(n-1), (0.9*distance)/float(n)
            let minFixC, maxFixC = fixC minPt, fixC maxPt
            let baseVarC = varC minPt
            let ext i' =
                let i = actualIndex i'
                let minVarC = baseVarC + (float(i)*(sep + one))
                let maxVarC = minVarC + one
                new Extents2d(varfix minVarC minFixC, varfix maxVarC maxFixC)
            {for i in 0..(n-1) do
                yield! to_instructions partial root boxes.[i] (ext i) (i::indices)
            }
        
    let box2instructions partial root box entity =
        match box with
        | InstructionBox.Single used ->
            {yield Instruction(partial, root, [||], entity, used)} 
        | _ -> to_instructions partial root box (toExtents2d entity) []
    
    let flowBox2instructions root flowBox entity =
        let partial = FlowBox.attachmentKind flowBox <> Attachments.Complete
        box2instructions partial root (InstructionBox.of_FlowBox flowBox) entity
        
type NodeType = 
    | ValveNode of int
    | PunchNode of int
    | IntersectionNode of int
    
type InstructionChip (chip : Chip) =
    let rep = 
        FlowRepresentation.create chip.FlowLayer
     |> FlowRepresentation.addValves chip.ControlLayer.Valves
    let punches = chip.FlowLayer.Punches
    let valves = chip.ControlLayer.Valves
    let nPunches = punches.Length
    let nValves = valves.Length
    let valveStartingNodeIndex = rep.NodeCount - nValves
    let node2type node =
        if node < nPunches
        then PunchNode node
        else
        if node >= valveStartingNodeIndex
        then ValveNode (node - valveStartingNodeIndex)
        else
        IntersectionNode (node - nPunches)
    let punch2node pi = pi
    let valve2node vi = vi + valveStartingNodeIndex
    let intersection2node ii = ii + nPunches
    let type2node nt =
        match nt with
        | ValveNode vi -> valve2node vi
        | PunchNode pi -> punch2node pi
        | IntersectionNode ii -> intersection2node ii
    member v.Chip = chip
    member v.Representation = rep
    member v.ToNodeType node = node2type node
    member v.OfNodeType nt = type2node nt
    
module Augmentations =
    let ifValve (ic : InstructionChip) node =
        match ic.ToNodeType node with
        | ValveNode vi -> Some vi
        | _ -> None
        
    let addIfValve (ic : InstructionChip) node set =
        match ic.ToNodeType node with
        | ValveNode vi -> Set.add vi set
        | _ -> set
    
    let isValve (ic : InstructionChip) =
        ifValve ic >> Option.is_some
        
    type InstructionChip with
        member ic.ifValve node = ifValve ic node
        member ic.addIfValve node set = addIfValve ic node set
        member ic.isValve node = isValve ic node

    
module Search =    

    let pair2set (a,b) = Set.empty |> Set.add a |> Set.add b    

    let edgesNvalvesOfPath (ic : InstructionChip) (path : FlowRepresentation.Path.IPath) =
        let edges = path.Edges |> Set.of_list
        let valves = path.Nodes |> List.choose ic.ifValve |> Set.of_list
        edges, valves
        
    let edgeToedge (ic : InstructionChip) inputEdge outputEdge =
        let rep = ic.Representation
        let boundaryEdges = Set.empty |> Set.add inputEdge |> Set.add outputEdge
        let inputNodes = FlowRepresentation.edge2nodes rep inputEdge
        let outputNodes = FlowRepresentation.edge2nodes rep outputEdge
        let maybePath = 
            FlowRepresentation.Search.findShortestPath rep boundaryEdges (pair2set inputNodes) (pair2set outputNodes)
        match maybePath with
        | None -> None
        | Some path ->
            // use sameAs instead of differentFrom
            // to avoid including valves or turns that weren't consciously on path
            let inputNode = FlowRepresentation.sameAs (path.StartNode) inputNodes
            let outputNode = FlowRepresentation.sameAs (List.hd path.Nodes) outputNodes
            let edges, valves = edgesNvalvesOfPath ic path 
            // for the same reason, don't include the boundaryEdges:
            // |> Set.union boundaryEdges
            // already in path.Nodes, since using sameAs: 
            // |> ic.addIfValve inputNode |> ic.addIfValve outputNode
            Some (inputNode, outputNode, edges, valves)

    let nodeTonode (ic : InstructionChip) removedEdges inputNode outputNode =
        let rep = ic.Representation
        let maybePath =
            FlowRepresentation.Search.findShortestPath rep removedEdges (Set.singleton inputNode) (Set.singleton outputNode)
        match maybePath with
        | None -> None
        | Some path ->
            let edges, valves = edgesNvalvesOfPath ic path
            Some (inputNode, outputNode, edges, valves)            
    
exception NoPathFound of string
            
module Build =
    
    let inputBox (ic : InstructionChip) pi =
        let punchNode = ic.OfNodeType (PunchNode pi)
        FlowBox.Primitive (Attachments.create None (Some punchNode), 
                           usedEmpty)
    
    let outputBox (ic : InstructionChip) pi =
        let punchNode = ic.OfNodeType (PunchNode pi)
        FlowBox.Primitive (Attachments.create (Some punchNode) None, 
                           usedEmpty)
    
    let pathBox (ic : InstructionChip) (inputClick, inputEdge) (outputClick, outputEdge) =
        if inputEdge=outputEdge
        then let edge = inputEdge
             let f = ic.Representation.ToFlowSegment edge
             let s,t = f.Segment.StartPoint, f.Segment.EndPoint
             let ds,dt = s.GetDistanceTo(inputClick), t.GetDistanceTo(outputClick)
             let inputPoint,outputPoint = if ds<dt then s,t else t,s
             let inputNode,outputNode = ic.Representation.OfPoint inputPoint, ic.Representation.OfPoint outputPoint
             let edges = Set.singleton edge
             let valves = Set.empty |> ic.addIfValve inputNode |> ic.addIfValve outputNode
             FlowBox.Primitive (Attachments.create (Some inputNode) (Some outputNode), 
                                used edges valves)
        else
        match Search.edgeToedge ic inputEdge outputEdge with
        | Some (inputNode, outputNode, edges, valves) ->
            FlowBox.Primitive (Attachments.create (Some inputNode) (Some outputNode), 
                               used edges valves)
        | None -> raise(NoPathFound("cannot find path between input flow with output flow"))     
    
    let SeqBox (ic : InstructionChip) (boxes : # (FlowBox.FlowBox seq)) =
        // extend the boxes so that the output of one is the input of the next
        let extendBox ((es, previousBox), (es', currentBox)) =
            let removedEdges = Set.union es es'
            let inputNode = 
                match (FlowBox.attachment previousBox).OutputAttachment with
                | Some node -> node
                | None -> invalid_arg "cannot attach intermediary from output"
            let currentAttachments = FlowBox.attachment currentBox
            let outputNode =
                match currentAttachments.InputAttachment with
                | Some node -> node
                | None -> invalid_arg "cannot attach intermediary to input"
            match Search.nodeTonode ic removedEdges inputNode outputNode with
            | Some (inputNode, outputNode, edges, valves) ->
                FlowBox.Extended (Attachments.create (Some inputNode) (currentAttachments.OutputAttachment), 
                                  used edges valves, 
                                  currentBox)
            | None -> raise(NoPathFound("cannod find path between intermediaries"))
        if not (Seq.nonempty boxes)
        then invalid_arg "cannot make empty sequence"
        let extendedBoxes =
            boxes
         |> Seq.map (fun box -> FlowBox.mentionedEdgesLax box, box)
         |> fun (s) -> 
             Seq.append (s |> Seq.hd |> snd |> Seq.singleton)
                        (s |> Seq.pairwise |> Seq.map extendBox)
         |> Array.of_seq
        let inputAttachment = (FlowBox.attachment extendedBoxes.[0]).InputAttachment
        let outputAttachment = (FlowBox.attachment extendedBoxes.[extendedBoxes.Length-1]).OutputAttachment
        FlowBox.Seq (Attachments.create inputAttachment outputAttachment, 
                     extendedBoxes)

    // extend the given box so that it fits into the given attachments        
    let extendBox ic (a : Attachments.Attachments) box =
        Debug.Assert (Attachments.sameKind (a.Kind) (FlowBox.attachmentKind box), 
                      "extendBox: given attachments and box attachments must be of the same kind")        
        if a.Kind = Attachments.Complete
        then box
        else
        let mentionedEdges = FlowBox.mentionedEdgesLax box
        let boxA = FlowBox.attachment box
        let edges, valves = ref Set.empty, ref Set.empty
        let addSearch inputNode outputNode =
            match Search.nodeTonode ic mentionedEdges inputNode outputNode with
            | Some (inputNode, outputNode, edges', valves') ->
              edges  := Set.union !edges edges'
              valves := Set.union !valves valves'
            | None -> raise(NoPathFound("cannot find path to extend box"))
        if Option.is_some a.InputAttachment
        then addSearch (Option.get a.InputAttachment) (Option.get boxA.InputAttachment)
        if Option.is_some a.OutputAttachment
        then addSearch (Option.get boxA.OutputAttachment) (Option.get a.OutputAttachment)
        FlowBox.Extended (a, used !edges !valves, box)
    
    let extendBoxes ic (a : Attachments.Attachments) boxes =
        if a.Kind = Attachments.Complete
        then boxes
        else boxes |> Array.map (extendBox ic a) 
    
    let AndBox (ic : InstructionChip) a boxes =
        FlowBox.And (a, extendBoxes ic a boxes)
        
    let OrBox (ic : InstructionChip) a boxes ordering =
        FlowBox.Or (a, extendBoxes ic a boxes, ordering)
        
module Interactive =

    open BioStream.Micado.Plugin
    open BioStream.Micado.Plugin.Editor.Extra

    let promptEdge (flowRep : FlowRepresentation.IFlowRepresentation) message =
        Editor.promptPoint message
     |> Option.map (Geometry.to2d >> (fun (point) -> point, flowRep.ClosestEdge point))
        
    type BioStream.Micado.Core.FlowRepresentation.IFlowRepresentation with
        member v.promptEdge message = promptEdge v message
    
    let promptAnyPunchBox (ic : InstructionChip) makeBox message =
        ic.Chip.FlowLayer.promptPunch message
     |> Option.map (makeBox ic)

    let arrayOfRevList = Routing.arrayOfRevList
    
    let promptAnyPunchBoxes (ic : InstructionChip) makeBox baseMessage =
        let message (i : int) = baseMessage ^ " " ^ "#" ^ i.ToString()
        let rec acc pickedSet boxes i =
            match ic.Chip.FlowLayer.promptPunch (message i) with
            | None -> arrayOfRevList boxes
            | Some pi ->
                if Set.mem pi pickedSet
                then Editor.writeLine "Punch already selected."
                     acc pickedSet boxes i
                else
                Editor.writeLine ""
                let box = makeBox ic pi
                acc (Set.add pi pickedSet) (box::boxes) (i+1)
        acc Set.empty [] 1
        
    let promptInputBox ic =
        promptAnyPunchBox ic Build.inputBox "Select an input flow punch: "

    let promptOutputBox ic =
        promptAnyPunchBox ic Build.outputBox "Select an output flow punch: "
        
    let promptPathBox (ic : InstructionChip) =
        let promptInputEdge() = ic.Representation.promptEdge "Select an input point on the flow: "
        let promptOutputEdge() = ic.Representation.promptEdge "Select an outpoint point on the flow: "
        match promptInputEdge() with
        | Some inputEdge ->
            match promptOutputEdge() with
            | Some outputEdge -> 
               try
                Some (Build.pathBox ic inputEdge outputEdge)
               with 
                | NoPathFound(msg) -> 
                    Editor.writeLine ("Error: " ^ msg ^ ".")
                    None
            | None -> None
        | None -> None

    let promptSeqBox (ic : InstructionChip) (boxes : FlowBox.FlowBox array) =
        try
            Some (Build.SeqBox ic boxes)
        with
            | InvalidArgument(msg) ->
                Editor.writeLine ("Invalid argument: " ^ msg ^ ".")
                None
            | NoPathFound(msg) ->
                Editor.writeLine("Error: " ^ msg ^ ".")
                None
    
    let promptOneAttachment (ic : InstructionChip) message =
        match ic.Representation.promptEdge message with
        | None -> None
        | Some (click, edge) ->
            let a,b = FlowRepresentation.edge2nodes ic.Representation edge
            let clickDistance = ic.Representation.ToPoint >> click.GetDistanceTo
            let chosenNode = // avoid valve nodes, and otherwise choose node closest to clicked point
                match ic.isValve a, ic.isValve b with
                | true, true
                | false, false ->
                    if clickDistance a < clickDistance b then a else b
                | false, true -> a
                | true, false -> b
            Some chosenNode
                    
    let promptAttachments (ic : InstructionChip) (boxes : FlowBox.FlowBox array) =
        let bas = boxes |> Array.map FlowBox.attachmentKind
        let kind = bas.[0]
        let allSameKind = bas |> Seq.for_all (Attachments.sameKind kind)
        if not allSameKind
        then Editor.writeLine "Error: All boxes must be of the same kind." 
             None
        else
        try
            let ia, oa =
                let noNeedInputAtt() =
                    Editor.writeLine "(no need input attachment)"
                    None
                let noNeedOutputAtt() =
                    Editor.writeLine "(no need output attachment)"
                    None
                let raiseIfNone x = // none would indicate a cancellation on the user's part
                    if Option.is_none x
                    then invalid_arg "cancel"
                    else x
                let promptInputAtt() =
                    promptOneAttachment ic "Choose input attachment: " |> raiseIfNone
                let promptOutputAtt() =
                    promptOneAttachment ic "Choose output attachment: " |> raiseIfNone
                match kind with
                | Attachments.Complete            -> noNeedInputAtt(), noNeedOutputAtt()
                | Attachments.Input _             -> noNeedInputAtt(), promptOutputAtt()
                | Attachments.Output _            -> promptInputAtt(), noNeedOutputAtt()
                | Attachments.Intermediary (_, _) -> promptInputAtt(), promptOutputAtt()
            
            Some (Attachments.create ia oa)
        with
            | :? InvalidArgumentException -> None // user cancelled 
            
    let promptAndBox (ic : InstructionChip) (boxes : FlowBox.FlowBox array) =
        let build a =
            try
                Some (Build.AndBox ic a boxes)
            with
                | NoPathFound(msg) ->
                    Editor.writeLine("Error: " ^ msg ^ ".")
                    None
        promptAttachments ic boxes |> Option.bind build
    
    let promptOrBox (ic : InstructionChip) (boxes : FlowBox.FlowBox array) =
        let promptOrdering() =
            if boxes.Length=1
            then Some HorizontalOrdering // doesn't matter
            else
            Editor.promptSelectIdName "Ordering of boxes" ["horizontal";"vertical"]
         |> Option.map (fun s -> if s = "horizontal" then HorizontalOrdering else VerticalOrdering)
            
        let build a ordering =
            try
                Some (Build.OrBox ic a boxes ordering)
            with
                | NoPathFound(msg) ->
                    Editor.writeLine("Error: " ^ msg ^ ".")
                    None
                    
        match promptAttachments ic boxes with
        | None -> None
        | Some a ->
            promptOrdering() |> Option.bind (build a) 

    let promptOrAnyPunchBox (ic : InstructionChip) makeBox baseMessage =
        let boxes = promptAnyPunchBoxes (ic : InstructionChip) makeBox baseMessage
        match boxes.Length with
        | 0 -> None
        | 1 -> Some boxes.[0]
        | _ -> promptOrBox ic boxes
             
    let promptOrInputBox ic =
        promptOrAnyPunchBox ic Build.inputBox "Select input flow punch"
        
    let promptOrOutputBox ic =
        promptOrAnyPunchBox ic Build.outputBox "Select output flow punch"
        
    let promptInstructions (ic : InstructionChip) (root : string) (box : FlowBox.FlowBox) =
        let instructions =
            Editor.promptSelectEntity "Select entity for extents: "
         |> Option.map (Convert.flowBox2instructions root box)
        instructions