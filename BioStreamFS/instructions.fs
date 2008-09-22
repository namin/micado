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
         |> Set.union_all   

    let rec mentionedUsed flowBox =
        match flowBox with
        | Primitive (_, u) -> 
            u
        | Extended (_, u, f) -> 
            u.append(mentionedUsed f)
        | Or (_, fs, _) | And (_, fs) | Seq (_, fs) ->
            fs 
         |> Seq.map mentionedUsed
         |> Seq.reduce (fun u u' -> u.append(u'))
        
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
            fs |> Array.map of_FlowBox |> Array.reduce_right wrapAround

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
                 
type Instruction (partial : bool, root: string, indices : int array, entity : Entity option, used : Used) =
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
    member v.Extents = entity |> Option.map toExtents2d
    member v.Partial = partial

module Convert =
    open BioStream.Micado.Plugin // needed for Database.writeEntity

    let rec to_instructions partial root box (extents : Extents2d) indices =
        match box with
        | InstructionBox.Single used ->
            seq {yield Instruction(partial, 
                                  root, 
                                  indices |> List.rev |> Array.of_list, 
                                  extents |> rectangle |> Database.writeEntityAndReturn |> Some, 
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
            seq { for i in 0..(n-1) do
                      yield! to_instructions partial root boxes.[i] (ext i) (i::indices)
                }
        
    let box2instructions partial root box entity =
        match box with
        | InstructionBox.Single used ->
            seq {yield Instruction(partial, root, [||], Some entity, used)} 
        | _ -> let extents = toExtents2d entity
               entity.Dispose()
               to_instructions partial root box extents []
    
    let flowBox2instructions root flowBox entity =
        let partial = FlowBox.attachmentKind flowBox <> Attachments.Complete
        box2instructions partial root (InstructionBox.of_FlowBox flowBox) entity
        
type NodeType = 
    | ValveNode of int
    | PunchNode of int
    | IntersectionNode of int
            
type InstructionChip (chip : Chip) =
    let mutable disposed = false
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
    let cleanup() =
        if not disposed then 
            disposed <- true;
            (chip :> System.IDisposable).Dispose(); 
    member v.Chip = chip
    member v.Representation = rep
    member v.ToNodeType node = node2type node
    member v.OfNodeType nt = type2node nt
    interface System.IDisposable with
        member v.Dispose() = cleanup()
        
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

module Store =
    
    open BioStream.Micado.Plugin
    
    type Ratio = double
    
    type ClickedFlow = FlowSegment * double
    
    type FlowAnnotation = 
        Path of ClickedFlow * ClickedFlow
      | InputPunch of Punch
      | OutputPunch of Punch
      | InputOr of ClickedFlow * Ordering * Punch array
      | OutputOr of ClickedFlow * Ordering * Punch array
      | OrInput of ClickedFlow * Ordering * string array
      | OrOutput of ClickedFlow * Ordering * string array
      | OrIntermediary of (ClickedFlow * ClickedFlow) * Ordering * string array
      | OrComplete of Ordering * string array
      | AndInput of ClickedFlow * string array
      | AndOutput of ClickedFlow *  string array
      | AndIntermediary of (ClickedFlow * ClickedFlow)  * string array
      | AndComplete of string array
      | SeqInput of string array
      | SeqOutput of string array
      | SeqIntermediary of string array
      | SeqComplete of string array
      
    let flowAnnotationKind = 
        let intermediary() = Attachments.Intermediary (0,0)
        let input() = Attachments.Input 0
        let output() = Attachments.Output 0
        let complete() = Attachments.Complete
        function
        | Path _ | OrIntermediary _ | AndIntermediary _ | SeqIntermediary _ -> intermediary()
        | InputPunch _ | InputOr _ | OrInput _ | AndInput _ | SeqInput _ -> input()
        | OutputPunch _ | OutputOr _ | OrOutput _ | AndOutput _ | SeqOutput _ -> output()
        | OrComplete _ | AndComplete _ | SeqComplete _ -> complete()
    
    let containedFlowAnnotations =
        function
        | Path _
        | InputPunch _
        | OutputPunch _
        | InputOr _
        | OutputOr _ -> [||]
        | OrInput (_,_,ns) 
        | OrOutput (_,_,ns)
        | OrIntermediary (_,_,ns)
        | OrComplete (_,ns)
        | AndInput (_,ns)
        | AndOutput (_,ns)
        | AndIntermediary (_,ns)
        | AndComplete (ns)
        | SeqInput (ns)
        | SeqOutput (ns)
        | SeqIntermediary (ns)
        | SeqComplete (ns) -> ns
            
    let flowApp = "flow"
    
    let promptedEdgeToClickedFlow (ic : InstructionChip) (clickedPt : Point2d,e) =
        let normalize (f : FlowSegment ) = ic.Chip.FlowLayer.Entity2Segment f.Entity |> Option.get
        let f = ic.Representation.ToFlowSegment e |> normalize
        let p = f.Segment.GetParameterOf(clickedPt)
        let p' = if p<0.0 
                 then 0.0 
                 else if p>1.0
                      then 1.0
                      else p
        (f,p') : ClickedFlow
    
    let clickedFlowToPromptedEdge (ic : InstructionChip) ((f,r) : ClickedFlow) =
        let pt = f.Segment.EvaluatePoint(r)
        let e = ic.Representation.ClosestEdge(pt)
        (pt,e)
    
    let nodeIndexToClickedFlow (ic : InstructionChip) ni =
        let p = ni |> ic.Representation.ToPoint
        let e = p |> ic.Representation.ClosestEdge
        promptedEdgeToClickedFlow ic (p,e)
    
    let clickedFlowToNodeIndex (ic : InstructionChip) ((f,r) : ClickedFlow) =
        let pt,e = clickedFlowToPromptedEdge ic (f,r)
        let a,b = FlowRepresentation.edge2nodes ic.Representation e
        let ptA,ptB = ic.Representation.ToPoint a, ic.Representation.ToPoint b
        let ni = if ptA.GetDistanceTo(pt) < ptB.GetDistanceTo(pt) then a else b
        ni
            
    let attachmentsDispatch (ic : InstructionChip) (makeInput, makeOutput, makeIntermediary, makeComplete) (a : Attachments.Attachments) =
        let toClickedFlow ni = ni |> nodeIndexToClickedFlow ic
        match a.Kind with
        | Attachments.Input ni -> makeInput (toClickedFlow ni)
        | Attachments.Output ni -> makeOutput (toClickedFlow ni)
        | Attachments.Intermediary (ni1,ni2) -> makeIntermediary (toClickedFlow ni1,toClickedFlow ni2)
        | Attachments.Complete -> makeComplete()
    
    let orDispatcher names ordering =
        let input cf = OrInput (cf,ordering,names)
        let output cf = OrOutput (cf,ordering,names)
        let intermediary cfs = OrIntermediary (cfs,ordering,names)
        let complete() = OrComplete (ordering,names)
        (input, output, intermediary, complete)

    let andDispatcher names =
        let input cf = AndInput (cf,names)
        let output cf = AndOutput (cf,names)
        let intermediary cfs = AndIntermediary (cfs,names)
        let complete() = AndComplete (names)
        (input, output, intermediary, complete)

    let seqDispatcher names =
        let input cf = SeqInput names
        let output cf = SeqOutput names
        let intermediary cfs = SeqIntermediary names
        let complete() = SeqComplete names
        (input, output, intermediary, complete)
        
    let flowAnnotationToBox (ic : InstructionChip) findBoxByName ann =
        let f2e f = clickedFlowToPromptedEdge ic f
        let punchBox builder p = builder ic ((ic.Chip.FlowLayer.Punch2Index p) |> Option.get)
        let inputAtts cf = Attachments.create None (Some (clickedFlowToNodeIndex ic cf))
        let outputAtts cf  = Attachments.create (Some (clickedFlowToNodeIndex ic cf)) None
        let intermediaryAtts (cf1,cf2) = Attachments.create (Some (clickedFlowToNodeIndex ic cf1)) (Some (clickedFlowToNodeIndex ic cf2))
        let completeAtts() = Attachments.create None None
        let putOrBox builder attachments ordering punches =
            let boxes = punches |> Array.map (punchBox builder)
            Build.OrBox ic attachments boxes ordering
        let orBox attachments ordering names =
            let boxes = Array.map findBoxByName names
            Build.OrBox ic attachments boxes ordering
        let andBox attachments names =
            let boxes = Array.map findBoxByName names
            Build.AndBox ic attachments boxes
        let seqBox names =
            let boxes = Array.map findBoxByName names
            Build.SeqBox ic boxes
        match ann with
        | Path (f1,f2) -> Build.pathBox ic (f2e f1) (f2e f2)
        | InputPunch p -> punchBox Build.inputBox p
        | OutputPunch p -> punchBox Build.outputBox p
        | InputOr (cf,o,ps) -> putOrBox (Build.inputBox) (inputAtts cf) o ps
        | OutputOr (cf,o,ps) -> putOrBox (Build.outputBox) (outputAtts cf) o ps
        | OrInput (cf,o,ns) -> orBox (inputAtts cf) o ns
        | OrOutput (cf,o,ns) -> orBox (outputAtts cf) o ns
        | OrIntermediary (cfs,o,ns) -> orBox (intermediaryAtts cfs) o ns
        | OrComplete (o,ns) -> orBox (completeAtts()) o ns
        | AndInput (cf,ns) -> andBox (inputAtts cf) ns
        | AndOutput (cf,ns) -> andBox (outputAtts cf) ns
        | AndIntermediary (cfs,ns) -> andBox (intermediaryAtts cfs) ns
        | AndComplete (ns) -> andBox (completeAtts()) ns
        | SeqInput (ns)
        | SeqOutput (ns)
        | SeqIntermediary (ns)
        | SeqComplete (ns) -> seqBox ns

    let rec to_instructions partial root box (entities : _ array) totals indices =
        let rec get_index totals indices =
            match totals,indices with
            | [],[] -> 0
            | t::t',i::i' -> 
                i + t*(get_index t' i')
            | _ -> failwith "totals and indices are not the same length!"
        let getEntity i =
            if entities.Length <= i
            then None
            else entities.[i]
        match box with
        | InstructionBox.Single used ->
            seq {yield Instruction(partial, 
                                  root, 
                                  indices |> List.rev |> Array.of_list, 
                                  getEntity (get_index totals indices), 
                                  used)}
        | InstructionBox.Multi (ordering, boxes) ->
            let n = boxes.Length
            let totals' = n::totals
            seq { for i in 0..(n-1) do
                      yield! to_instructions partial root boxes.[i] entities totals' (i::indices)
                }
        
    let box2instructions partial root box (entities : _ array) =
        match box with
        | InstructionBox.Single used ->
            seq {yield Instruction(partial, root, [||], entities.[0], used)} 
        | _ -> to_instructions partial root box entities [] []
    
    let flowBox2instructions root flowBox entities =
        let partial = FlowBox.attachmentKind flowBox <> Attachments.Complete
        box2instructions partial root (InstructionBox.of_FlowBox flowBox) entities
                             
    let toDatabase key flowAnn =
        let dictId = Database.getNamedObjectsDictionaryId true flowApp |> Option.get
        let rb =
            let rb = new ResultBuffer()
            let wEntity (e : #Entity) =
                rb.Add(new TypedValue((int)DxfCode.SoftPointerId, e.ObjectId))
            let wClickedFlow ((f,r) : ClickedFlow) =
                wEntity (f.Entity)
                rb.Add(new TypedValue((int)DxfCode.Real, r))
            let wStr str =
                rb.Add(new TypedValue((int)DxfCode.Text, str))
            let wOrdering o =
                let s =
                    match o with
                    | HorizontalOrdering -> "h"
                    | VerticalOrdering -> "v"
                wStr s
            let wArray w a =
                for el in a do
                    w el
            let wPutOr cf o ps =
                wClickedFlow cf
                wOrdering o
                wArray wEntity ps
            match flowAnn with
            | Path (cf1, cf2) -> wStr "path"; wClickedFlow cf1; wClickedFlow cf2
            | InputPunch p -> wStr "inputPunch"; wEntity p
            | OutputPunch p -> wStr "outputPunch"; wEntity p
            | InputOr (cf,o,ps) -> wStr "inputOr"; wPutOr cf o ps
            | OutputOr (cf,o,ps) -> wStr "outputOr"; wPutOr cf o ps
            | OrInput (cf,o,ns) -> wStr "orInput"; wClickedFlow cf; wOrdering o; wArray wStr ns
            | OrOutput (cf,o,ns) -> wStr "orOutput"; wClickedFlow cf; wOrdering o; wArray wStr ns
            | OrIntermediary ((cf1,cf2),o,ns) -> wStr "orIntermediary"; wClickedFlow cf1; wClickedFlow cf2; wOrdering o; wArray wStr ns
            | OrComplete (o,ns) -> wStr "orComplete"; wOrdering o; wArray wStr ns
            | AndInput (cf,ns) -> wStr "andInput"; wClickedFlow cf; wArray wStr ns
            | AndOutput (cf,ns) -> wStr "andOutput"; wClickedFlow cf; wArray wStr ns
            | AndIntermediary ((cf1,cf2),ns) -> wStr "andIntermediary"; wClickedFlow cf1; wClickedFlow cf2; wArray wStr ns
            | AndComplete (ns) -> wStr "andComplete"; wArray wStr ns
            | SeqInput (ns) -> wStr "seqInput"; wArray wStr ns
            | SeqOutput (ns) -> wStr "seqOutput"; wArray wStr ns
            | SeqIntermediary (ns) -> wStr "seqIntermediary"; wArray wStr ns
            | SeqComplete (ns) -> wStr "seqComplete"; wArray wStr ns
            rb
        Database.writeDictionaryEntry dictId key rb
    
    let instrApp = "instructions"
    
    let instructionSetReader (tr : Transaction) (key : string) (values : TypedValue array) =
         let readEntity (v : TypedValue) =
            match v.Value with
            | :? ObjectId as objId ->
                try
                    Some (tr.GetObject(objId, OpenMode.ForRead))
                with _ -> 
                    None
                |> Option.bind (fun obj -> match obj with
                                           | :? Entity as entity -> Some entity
                                           | _ -> None)
            | _ -> None 
         Some (values |> Array.map readEntity)
    
    let readInstructionSetInDatabase (key : string) =
        match Database.getNamedObjectsDictionaryId false instrApp with
        | None -> None
        | Some dictId ->
            Database.readDictionaryEntry dictId instructionSetReader key
    
    /// Reads all the instruction sets in the database, 
    /// returning a map from instruction/flow annotation key to an array of optional mapped entities.
    let readAllInstructionSetsInDatabase() =
        match Database.getNamedObjectsDictionaryId false instrApp with
        | None -> Map.empty
        | Some dictId -> 
            let skipper = (fun key -> Editor.writeLine ("Skipped instruction " ^ key))
            Database.readDictionary skipper dictId instructionSetReader
                        
    let updateInstructionSetInDatabase (key : string) entityValues =
        let rb = new ResultBuffer(entityValues)
        let dictId = Database.getNamedObjectsDictionaryId true instrApp |> Option.get
        Database.writeDictionaryEntry dictId key rb
    
    let readInstructionReferenceInEntityUnchecked (entity : Entity) =
        let reader _ _ (values : TypedValue array) =
            if values.Length<>1
            then None
            else
            match values.[0].Value with
            | :? int as i -> Some i
            | _ -> None
        let skipper _ = ()
        let read dictId =
            let map = Database.readDictionary skipper dictId reader |> Map.to_array
            if map.Length=1
            then Some map.[0]
            else None
        Database.getExtensionDictionaryId false instrApp entity |> Option.bind read
    
    let entitiesToValues entities =
        let tv id = new TypedValue((int)DxfCode.SoftPointerId, id)
        let tv0 () = new TypedValue((int)DxfCode.Int8, 0)
        entities 
        |> Array.map (function
                      | None -> tv0()
                      | Some (e : Entity) -> e.ObjectId |> tv)

    let checkOrDeleteEntityReferenceInInstruction delete (entity : Entity) (key,i) =
        match readInstructionSetInDatabase key with
        | None -> None
        | Some entities ->
            if i >= entities.Length
            then None
            else
            match entities.[i] with
            | None -> None
            | Some entity' ->
                if entity <> entity'
                then None
                else
                if delete
                then entities.[i] <- None
                     let values = entities |> entitiesToValues
                     updateInstructionSetInDatabase key values    
                Some (key,i)
    
    let checkEntityReferenceInInstruction = checkOrDeleteEntityReferenceInInstruction false
    let deleteEntityReferenceInstruction entity v = checkOrDeleteEntityReferenceInInstruction true entity v |> ignore    
    
    /// Returns the instruction key and index to which this entity is mapped.
    let readInstructionReferenceInEntity (entity : Entity) =
        entity |> readInstructionReferenceInEntityUnchecked 
        |> Option.bind (checkEntityReferenceInInstruction entity)
    
    let deleteAnyInstructionReferenceInEntity (entity : Entity) =
        match readInstructionReferenceInEntityUnchecked entity with
        | None -> ()
        | Some (key,i) -> 
            deleteEntityReferenceInstruction entity (key,i)
            let dictId = Database.getExtensionDictionaryId false instrApp entity |> Option.get
            Database.deleteDictionaryEntries dictId (seq { yield key })
                                
    let writeInstructionReferenceInEntity (key : string) (entity : Entity) (i : int) =
        let dictId = Database.getExtensionDictionaryId true instrApp entity |> Option.get
        let rb = new ResultBuffer([|new TypedValue((int)DxfCode.Int32, i)|])
        Database.writeDictionaryEntry dictId key rb
    
    let writeInstructionReferenceInEntityCautiously key entity i =
        deleteAnyInstructionReferenceInEntity entity
        writeInstructionReferenceInEntity key entity i
    
    /// Writes the instructions to the database, also updating the mapped entities.                   
    let writeInstructionSetToDatabase (key : string) (instructions : seq<Instruction>) =
        let entities = instructions |> Seq.map (fun (i : Instruction) -> i.Entity) |> Array.of_seq
        if entities.Length = 1
        then entities.[0] |> Option.iter (fun entity -> writeInstructionReferenceInEntityCautiously key entity 0)
        else for i in [0..entities.Length-1] do
                entities.[i] |> Option.iter (fun entity -> writeInstructionReferenceInEntity key entity i)                
        let entityValues = entitiesToValues entities 
        updateInstructionSetInDatabase key entityValues            
                              
    let fromDatabaseCustom (ic : InstructionChip) skipper =
        let reader (ic : InstructionChip) (tr : Transaction) (key : string) (values : TypedValue array) =
            let c f x y =
                match y with
                | None -> None
                | Some y -> f x |> Option.map (fun r -> y,r)
            let readEntity (v : TypedValue) =
                match v.Value with
                | :? ObjectId as objId ->
                    try
                        Some (tr.GetObject(objId, OpenMode.ForRead))
                    with _ -> 
                        None
                    |> Option.bind (fun obj -> match obj with
                                               | :? Entity as entity -> Some entity
                                               | _ -> None)
                | _ -> None 
            let readArray readEl a =
                let arrayOfRevList = Routing.arrayOfRevList
                let n = Array.length a
                let rec helper i lst =
                    if i=n
                    then Some (arrayOfRevList lst)
                    else 
                    match readEl a.[i] with
                    | None -> None
                    | Some el -> helper (i+1) (el::lst)
                helper 0 []
            let readFlowSegment (v : TypedValue) =
                v |> readEntity |> Option.bind ic.Chip.FlowLayer.Entity2Segment
            let readPunch (v : TypedValue) =
                v |> readEntity |> Option.bind (function 
                                                | :? Punch as p -> Some p
                                                | _ -> None)
            let readRatio (v : TypedValue) =
                match v.Value with
                | :? float as f -> 
                    if 0.0<=f && f<=1.0 then Some f else None
                | _ -> None
            let readStr (v : TypedValue) =
                match v.Value with
                | :? string as s -> Some s
                | _ -> None
            let readOrdering (v : TypedValue) =
                v |> readStr |> Option.bind (function 
                                             | "h" -> Some HorizontalOrdering
                                             | "v" -> Some VerticalOrdering
                                             | _ -> None)     
            let readPath() =
                match values with
                | [|_;f1;r1;f2;r2 |] ->
                    readFlowSegment f1 |> c readRatio r1 |> c readFlowSegment f2 |> c readRatio r2
                    |> Option.map (fun (((f1,r1),f2),r2) -> Path ((f1,r1), (f2,r2)))
                | _ -> None
            let readJustOnePunch makeStore =
                match values with
                | [|_;p|] -> p |> readPunch |> Option.map makeStore
                | _ -> None
            let readPutOr makeStore =
                if values.Length < 4
                then None
                else 
                readFlowSegment values.[1] |> c readRatio values.[2] |> c readOrdering values.[3]
                |> Option.bind (fun ((f,r),o) -> readArray readPunch values.[4..] |> Option.map (fun ps -> makeStore ((f,r),o,ps)))
            let iReadClickedFlow i =
                readFlowSegment values.[i] |> c readRatio values.[i+1]
            let iReadClickedFlows i =
                iReadClickedFlow i |> c iReadClickedFlow (i+2)
            let iReadOrdering i =
                readOrdering values.[i]
            let iReadNames i =
                readArray readStr values.[i..]        
            let readSeq makeStore =
                readArray readStr values.[1..]
                |> Option.map makeStore
            let readOrPut makeStore =
                if values.Length < 4 
                then None 
                else iReadClickedFlow 1 |> c iReadOrdering 3 |> c iReadNames 4 |> Option.map (fun ((cf,o),ns) -> makeStore (cf,o,ns))
            let readAndPut makeStore =
                if values.Length < 3 
                then None 
                else iReadClickedFlow 1 |> c iReadNames 3 |> Option.map (fun (cf,ns) -> makeStore (cf,ns))
            if values.Length=0
            then None
            else
            match values.[0].Value with
            | :? string as t ->
                match t with
                | "path" -> readPath()
                | "inputPunch" -> readJustOnePunch InputPunch
                | "outputPunch" -> readJustOnePunch OutputPunch
                | "inputOr" -> readPutOr InputOr
                | "outputOr" -> readPutOr OutputOr
                | "orInput" -> readOrPut OrInput
                | "orOutput" -> readOrPut OrOutput
                | "orIntermediary" ->
                    if values.Length < 6 
                    then None 
                    else iReadClickedFlows 1 |> c iReadOrdering 5 |> c iReadNames 6 |> Option.map (fun ((cfs,o),ns) -> OrIntermediary (cfs,o,ns))                
                | "orComplete" ->
                    if values.Length < 2 
                    then None 
                    else iReadOrdering 1 |> c iReadNames 2 |> Option.map OrComplete                
                | "andInput" -> readAndPut AndInput
                | "andOutput" -> readAndPut AndOutput
                | "andIntermediary" ->
                    if values.Length < 5 
                    then None 
                    else iReadClickedFlows 1 |> c iReadNames 5 |> Option.map AndIntermediary                
                | "andComplete" ->
                    iReadNames 1 |> Option.map AndComplete
                | "seqInput" -> readSeq SeqInput
                | "seqOutput" -> readSeq SeqOutput
                | "seqIntermediary" -> readSeq SeqIntermediary
                | "seqComplete" -> readSeq SeqComplete
                | _ -> None
            | _ -> None
        match Database.getNamedObjectsDictionaryId false flowApp with
        | None -> Map.empty
        | Some dictId ->
            Database.readDictionary skipper dictId (reader ic) 
            
    let fromDatabase ic = fromDatabaseCustom ic (fun key -> Editor.writeLine ("Skipped flow annotation " ^ key))
    
    let purgeFlowAnnotations (ic : InstructionChip) =
        let toDelete = ref []
        let skipper key = toDelete := key :: !toDelete
        let store = fromDatabaseCustom ic skipper
        let boxDict = new Dictionary<string,FlowBox.FlowBox>()
        let rec findBoxByName name =
            match boxDict.TryGetValue name with
            | true, box -> box
            | false, _ ->
                let ann = Map.find name store
                let box = flowAnnotationToBox ic findBoxByName ann
                boxDict.[name] <- box
                box 
        for kv in store do
            try
                flowAnnotationToBox ic findBoxByName (kv.Value) |> ignore
            with :? NoPathFoundException | :? System.ArgumentException ->
                toDelete := (kv.Key) :: !toDelete      
        toDelete := List.sort compare !toDelete
        for key in !toDelete do
            Editor.writeLine ("flow annotation " ^ key ^ " scheduled for deletion")
        let n = (!toDelete).Length
        let doDelete = n>0 && Editor.promptYesOrNo false ("Are you sure you want to delete " ^ n.ToString() ^ " flow annotations?")
        if doDelete
        then Database.deleteDictionaryEntries (Database.getNamedObjectsDictionaryId false flowApp |> Option.get) !toDelete
             Database.deleteDictionaryEntries (Database.getNamedObjectsDictionaryId false instrApp |> Option.get) !toDelete
             Editor.writeLine (n.ToString() ^ " flow annotations deleted.")
        else Editor.writeLine "Purge cancelled -- no flow annotations were deleted."
                                                         
module Interactive =

    open BioStream.Micado.Plugin
    open BioStream.Micado.Plugin.Editor.Extra

    let promptEdge (flowRep : FlowRepresentation.IFlowRepresentation) message =
        Editor.promptPoint message
     |> Option.map (Geometry.to2d >> (fun (point) -> point, flowRep.ClosestEdge point))
        
    type BioStream.Micado.Core.FlowRepresentation.IFlowRepresentation with
        member v.promptEdge message = promptEdge v message
    
    let promptAnyPunchBox (ic : InstructionChip) makeStore makeBox message =
        ic.Chip.FlowLayer.promptPunch message
     |> Option.map (fun pi -> (makeStore (ic.Chip.FlowLayer.Punches.[pi])),(makeBox ic pi))

    let arrayOfRevList = Routing.arrayOfRevList
    
    let promptAnyPunchBoxes (ic : InstructionChip) makeBox baseMessage =
        let message (i : int) = baseMessage ^ " " ^ "#" ^ i.ToString()
        let rec acc pickedSet punches boxes i =
            match ic.Chip.FlowLayer.promptPunch (message i) with
            | None -> (arrayOfRevList punches, arrayOfRevList boxes)
            | Some pi ->
                if Set.mem pi pickedSet
                then Editor.writeLine "Punch already selected."
                     acc pickedSet punches boxes i
                else
                Editor.writeLine ""
                let box = makeBox ic pi
                let punch = ic.Chip.FlowLayer.Punches.[pi]
                acc (Set.add pi pickedSet) (punch::punches) (box::boxes) (i+1)
        acc Set.empty [] [] 1
        
    let promptInputBox ic =
        promptAnyPunchBox ic Store.InputPunch Build.inputBox "Select an input flow punch: "

    let promptOutputBox ic =
        promptAnyPunchBox ic Store.OutputPunch Build.outputBox "Select an output flow punch: "
        
    let promptPathBox (ic : InstructionChip) =
        let promptInputEdge() = ic.Representation.promptEdge "Select an input point on the flow: "
        let promptOutputEdge() = ic.Representation.promptEdge "Select an outpoint point on the flow: "
        match promptInputEdge() with
        | Some inputEdge ->
            match promptOutputEdge() with
            | Some outputEdge -> 
               let box =
                   try
                    Some (Build.pathBox ic inputEdge outputEdge)
                   with 
                    | NoPathFound(msg) -> 
                        Editor.writeLine ("Error: " ^ msg ^ ".")
                        None
               box |> Option.map (fun box -> 
                                    let f = Store.promptedEdgeToClickedFlow ic 
                                    (Store.Path ((f inputEdge), (f outputEdge)), box))
            | None -> None
        | None -> None

    let promptSeqBox (ic : InstructionChip) (boxes : FlowBox.FlowBox array) =
        try
            Some (Build.SeqBox ic boxes)
        with
            | :? System.ArgumentException as e ->
                Editor.writeLine ("Invalid argument: " ^ e.Message ^ ".")
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
            | :? System.ArgumentException -> None // user cancelled 
            
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
        let punches,boxes = promptAnyPunchBoxes (ic : InstructionChip) makeBox baseMessage
        let intermediary cfs = failwith "input/output cannot be intermediary"
        let complete cf = failwith "input/output cannot be complete"
        match boxes.Length with
        | 0 -> None
        | 1 -> 
            let makeStore box =
                let input cf = Store.InputPunch punches.[0]
                let output cf = Store.OutputPunch punches.[0]
                Store.attachmentsDispatch ic (input, output, intermediary, complete) (FlowBox.attachment box)
            Some ((makeStore boxes.[0]), boxes.[0])
        | _ ->
            let makeStore box =
                match box with
                | FlowBox.Or (a,_,o) ->
                    let input cf = Store.InputOr (cf,o,punches)
                    let output cf = Store.OutputOr (cf,o,punches)
                    Store.attachmentsDispatch ic (input, output, intermediary, complete) a
                | _ -> failwith "Not an OR box." 
            promptOrBox ic boxes |> Option.map (fun box -> ((makeStore box), box))
             
    let promptOrInputBox ic =
        promptOrAnyPunchBox ic Build.inputBox "Select input flow punch"
        
    let promptOrOutputBox ic =
        promptOrAnyPunchBox ic Build.outputBox "Select output flow punch"
        
    let promptInstructions (ic : InstructionChip) (root : string) (box : FlowBox.FlowBox) =
        let instructions =
            Editor.promptSelectEntity "Select entity for extents: "
         |> Option.map (Convert.flowBox2instructions root box)
        instructions