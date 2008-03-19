#light

/// Graph utilities
module BioStream.Micado.Common.Graph

/// a graph
type IGraph =
    /// number of nodes
    abstract NodeCount : int
    /// given the node s, returns all nodes v, s.t. there is an edge from s to v
    abstract Neighbors : int -> int seq

/// create a graph
let create(table : bool[,]) =
    let nodeCount = min (Array2.length1 table) (Array2.length2 table)
    { new IGraph with
        member v.NodeCount = nodeCount
        member v.Neighbors(i) = Seq.filter (fun (j) -> table.[i,j]) (seq {0..nodeCount-1})
    }

/// depth-first-search exploration
type DFS ( graph : IGraph ) =
    let visited = Array.create graph.NodeCount false
    member private v.ExploreList ss =
        match ss with
        | [] -> seq []
        | s::ss -> if visited.[s]
                   then v.ExploreList ss
                   else visited.[s] <- true
                        let neighbors = Seq.to_list (graph.Neighbors s)
                        seq { yield s
                              yield! v.ExploreList (List.append neighbors ss) }
    // whether the given node has been visited by some exploration
    member v.Visited i = visited.[i]
    /// explores the given start node using depth-first search
    /// visiting any node that is reachable from the start node
    member v.Explore s = v.ExploreList [s]
    
/// connected components of a graph
type ConnectedComponents ( graph : IGraph ) =
    let dfs = new DFS(graph)
    let components =
        seq { for i in [0..graph.NodeCount-1] do
                if not (dfs.Visited i)
                then yield Set.of_seq (dfs.Explore i)
            }
    /// enumerates all connected components as sets of node indices
    member v.GetEnumerator() = components.GetEnumerator()