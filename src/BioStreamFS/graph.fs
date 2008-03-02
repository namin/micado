#light

/// Graph utilities
module BioStream.Micado.Common.Graph

type IGraph =
    abstract NodeCount : int
    abstract Neighbors : int -> int seq

let create(table : bool[,]) =
    let nodeCount = min (Array2.length1 table) (Array2.length2 table)
    { new IGraph with
        member v.NodeCount = nodeCount
        member v.Neighbors(i) = Seq.filter (fun (j) -> table.[i,j]) (seq {0..nodeCount-1})
    }

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
    member v.Visited i = visited.[i]
    member v.Explore s = v.ExploreList [s]
    
type ConnectedComponents ( graph : IGraph ) =
    let dfs = new DFS(graph)
    let components =
        seq { for i in [0..graph.NodeCount-1] do
                if not (dfs.Visited i)
                then yield Set.of_seq (dfs.Explore i)
            }
    member v.GetEnumerator() = components.GetEnumerator()