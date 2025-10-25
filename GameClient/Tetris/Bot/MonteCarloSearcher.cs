using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameClient.EntryPoint;

namespace GameClient.Tetris;

public class MonteCarloSearcher : ISearcher {
    public string SearcherInfo { get; }

    private readonly List<int> expandedDepths = new();
    private readonly Dictionary<int, int> expandedDepthCount = new();
    public readonly int fixedIterations;

    public MonteCarloSearcher(int iterations) {
        fixedIterations = iterations;
        SearcherInfo = $"MCTS ({iterations} iterations)";
    }

    public StateNode Search(GameState gameState, StateNode lastSelectedNode, IEvaluator evaluator, out SearchProcess searchProcess) {
        MonteCarloNode rootNode;
        if (lastSelectedNode is MonteCarloNode lastMonteCarloNode && lastSelectedNode.GameState == gameState) {
            lastMonteCarloNode.ConvertToRootNode();
            rootNode = lastMonteCarloNode;
        } else {
            rootNode = MonteCarloNode.CreateRootNode(gameState, evaluator);
        }

        Stopwatch sw = Stopwatch.StartNew();
        searchProcess = new MonteCarloSearchProcess();
        SearchReport report = new();
        expandedDepths.Clear();
        expandedDepthCount.Clear();
        for (int i = 0; i < fixedIterations; i++) {
            SingleIteration(rootNode, evaluator, report);
        }

        int depth = 0;
        rootNode.LogNode(fixedIterations);

        MonteCarloNode resultNode = rootNode, nextNode = null;
        while ((nextNode = resultNode.GetBestChild()) != null) { // traverse the tree to find the end result
            resultNode = nextNode;
            depth++;
        }

        if (BotClient.GetThreadProgressInfo(out var progressInfo)) {
            Console.WriteLine($"AAA-it{fixedIterations}: mcts iter at step={progressInfo.currentStep}, " +
                              $"att={progressInfo.attempt}/{progressInfo.maxAttempt} completed, " +
                              $"time={sw.Elapsed.TotalMilliseconds:F2}ms");
        }

        Console.WriteLine($"AAA-it{fixedIterations}: exdepth({expandedDepths.Count})=[{string.Join(", ", expandedDepths)}]");
        Console.WriteLine($"AAA-it{fixedIterations}: () bestNodeDepth={depth}, maxDepth={rootNode.MaxDepth} " +
                          $"expandedDepth={string.Join(", ", expandedDepthCount.OrderBy(pair => pair.Key).Select(pair => $"{{{pair.Key}:{pair.Value}}}"))} " +
                          $"createdNodes={report.createdNodes}");
        // Console.WriteLine($"AAA: finished {fixedIterations}iter, {sw.Elapsed.TotalMilliseconds:F2}ms");
        return resultNode;
    }

    public SearchStats GetLastSearchStats() {
        // TODO: Implement a way to track and return search statistics
        return new SearchStats("", "", "");
    }

    private void SingleIteration(MonteCarloNode rootNode, IEvaluator evaluator, SearchReport report) {
        // 1. Traverse the tree to select a node to expand
        MonteCarloNode currentNode = rootNode;
        int expandDepth = 0;
        while (currentNode != null && currentNode.Expanded) {
            currentNode = currentNode.VisitWeightedRandomChild();
            expandDepth++;
        }
        
        if (currentNode == null) {
            expandedDepths.Add(-1);
            // No valid node found, return early
            return;
        }
        
        // 2. Generate child nodes with all possible moves, and evaluate them 
        // expandedDepths.Add(currentNode.NodeDepth);
        currentNode.ExpandNode(evaluator, report);
        if (expandedDepthCount.TryGetValue(expandDepth, out int count)) {
            expandedDepthCount[expandDepth] = count + 1;
        } else {
            expandedDepthCount[expandDepth] = 1;
        }
        
        // 3. Apply backpropagation and recursively update the parent nodes
        Backpropagate(currentNode);
    }

    private void Backpropagate(MonteCarloNode node) {
        MonteCarloNode leafNode = node;
        
        while (true) {
            // rebuild the selection weights used in the next iteration
            bool evaluationUpdated = node.UpdateNodeEvaluationFromChild();

            if (!evaluationUpdated || node.Parent is null || node.Parent.IsRoot) break;
            node.UpdateSelectionWeightInParent();
            node = (MonteCarloNode)node.Parent;
        }

        // update max depth
        node = leafNode;
        while (node != null) {
            bool updatedParentDepth = node.UpdateParentMaxDepth();
            if (updatedParentDepth) {
                node = (MonteCarloNode)node.Parent;
            } else {
                break; // no need to continue if the parent depth was not updated
            }
        }
    }

    public class SearchReport {
        public int createdNodes;
    }
}