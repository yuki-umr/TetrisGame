using System.Collections.Generic;
using System.Diagnostics;

namespace GameClient.Tetris; 

public class Searcher {
    public readonly int nextSeek, beamWidth;
    private readonly SearchStat stats;
    
    public string LastSearchStats => $"{stats.lastNode} nodes {stats.lastTime:F0}ms";
    public string TotalSearchStats => $"{stats.nodes} nodes in {stats.time:F2}ms";
    public string SearchSpeed => $"{stats.nodes * 1000 / stats.time:F2} nodes/s";

    public Searcher(int nextSeek, int beamWidth) {
        this.nextSeek = nextSeek;
        this.beamWidth = beamWidth;
        stats = new SearchStat();
    }
    
    public BeamNode BeamSearch(GameState gameState, BeamNode lastSelectedNode, Evaluator evaluator, out List<BeamNode>[] nodesTree) {
        int nodeCount = 0;
        Stopwatch sw = Stopwatch.StartNew();
        
        // if the estimated current state and the actual state is not the same, do a normal search from scratch
        BeamNode rootNode;
        if (lastSelectedNode == null || lastSelectedNode.GameState != gameState) {
            rootNode = BeamNode.CreateRootNode(gameState, evaluator);
        } else {
            lastSelectedNode.ConvertToRootNode();
            rootNode = lastSelectedNode;
        }
        
        List<BeamNode> bestNodes = new List<BeamNode> { rootNode };
        nodesTree = new List<BeamNode>[nextSeek + 1];
        for (int i = 0; i <= nextSeek; i++) {
            PriorityQueue<BeamNode, int> childNodes = BeamNode.ExpandNodes(bestNodes, evaluator);
            nodeCount += childNodes.Count;
            
            // select top [BeamWidth] nodes from all generated nodes
            bestNodes.Clear();
            nodesTree[i] = new List<BeamNode>();
            while (childNodes.Count > 0 && bestNodes.Count < beamWidth) {
                BeamNode childNode = childNodes.Dequeue();
                if (!childNode.HasRoute()) continue;
                childNode.NodeRank = bestNodes.Count;
                bestNodes.Add(childNode);
                nodesTree[i].Add(childNode);
            }

            if (bestNodes.Count == 0) return null;
        }

        BeamNode bestNode = bestNodes[0];

        stats.AddResult(nodeCount, sw.Elapsed.TotalMilliseconds);
        return bestNode;
    }

    private class SearchStat {
        public long nodes, lastNode;
        public double time, lastTime;

        public void AddResult(long nodes, double time) {
            lastNode = nodes;
            lastTime = time;
            this.nodes += nodes;
            this.time += time;
        }
    }
}