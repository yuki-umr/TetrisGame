using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameClient.Tetris;

public class BeamSearcher : ISearcher {
    public readonly int nextSeek, beamWidth;
    private readonly SearchStat stats;

    public string SearcherInfo { get; }

    public BeamSearcher(int nextSeek, int beamWidth) {
        this.nextSeek = nextSeek;
        this.beamWidth = beamWidth;
        
        SearcherInfo = $"Beam Search (depth={nextSeek}, width={beamWidth})";
        stats = new SearchStat();
    }
    
    public StateNode Search(GameState gameState, StateNode lastSelectedNode, IEvaluator evaluator, out SearchProcess searchProcess) {
        int nodeCount = 0;
        Stopwatch sw = Stopwatch.StartNew();
        
        // if the estimated current state and the actual state is not the same, do a normal search from scratch
        StateNode rootNode;
        if (lastSelectedNode == null || lastSelectedNode.GameState != gameState) {
            rootNode = BeamNode.CreateRootNode(gameState, evaluator);
        } else {
            lastSelectedNode.ConvertToRootNode();
            rootNode = lastSelectedNode;
        }
        
        List<StateNode> bestNodes = new List<StateNode> { rootNode };
        BeamSearchProcess process = new(nextSeek);
        searchProcess = process;
        for (int i = 0; i <= nextSeek; i++) {
            PriorityQueue<StateNode, int> childNodes = BeamNode.ExpandNodes(bestNodes, evaluator);
            nodeCount += childNodes.Count;
            
            // select top [BeamWidth] nodes from all generated nodes
            bestNodes.Clear();
            while (childNodes.Count > 0 && bestNodes.Count < beamWidth) {
                BeamNode childNode = (BeamNode)childNodes.Dequeue();
                if (!childNode.HasRoute()) continue;
                childNode.NodeRank = bestNodes.Count;
                bestNodes.Add(childNode);
                process.AddNode(i, childNode);
            }

            if (bestNodes.Count == 0) return null;
        }

        StateNode bestNode = bestNodes[0];

        stats.AddResult(nodeCount, sw.Elapsed.TotalMilliseconds);
        return bestNode;
    }

    public SearchStats GetLastSearchStats() {
        string lastSearchStats = $"{stats.lastNode} nodes {stats.lastTime:F0}ms";
        string totalSearchStats = $"{stats.nodes} nodes in {stats.time:F2}ms";
        string searchSpeed = $"{stats.nodes * 1000 / stats.time:F2} nodes/s";
        return new SearchStats(lastSearchStats, totalSearchStats, searchSpeed);
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