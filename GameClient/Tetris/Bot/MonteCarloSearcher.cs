using System;
using System.Collections.Generic;
using System.Linq;

namespace GameClient.Tetris;

public class MonteCarloSearcher : ISearcher {
    public string SearcherInfo { get; }

    private readonly Dictionary<int, int> expandedDepthCount = new();
    private readonly int fixedIterations;

    public MonteCarloSearcher(int iterations) {
        fixedIterations = iterations;
        SearcherInfo = $"MCTS ({iterations} iterations)";
    }
    
    public StateNode Search(GameState gameState, StateNode lastSelectedNode, Evaluator evaluator, out SearchProcess searchProcess) {
        MonteCarloNode rootNode;
        if (lastSelectedNode is MonteCarloNode lastMonteCarloNode && lastSelectedNode.GameState == gameState) {
            lastMonteCarloNode.ConvertToRootNode();
            rootNode = lastMonteCarloNode;
        } else {
            rootNode = MonteCarloNode.CreateRootNode(gameState, evaluator);
        }

        searchProcess = new MonteCarloSearchProcess();
        MonteCarloNode.CreatedChildNodesCount = 0;
        expandedDepthCount.Clear();
        for (int i = 0; i < fixedIterations; i++) {
            SingleIteration(rootNode, evaluator);
        }

        int depth = 0;
        MonteCarloNode resultNode = rootNode, nextNode = null;
        while ((nextNode = resultNode.GetBestChild()) != null) { // traverse the tree to find the end result
            resultNode = nextNode;
            depth++;
        }

        Console.WriteLine($"AAA: bestNodeDepth={depth}, " +
                          $"expandedDepth={string.Join(", ", expandedDepthCount.OrderBy(pair => pair.Key).Select(pair => $"{{{pair.Key}:{pair.Value}}}"))} " +
                          $"createdNodes={MonteCarloNode.CreatedChildNodesCount}");
        return resultNode;
    }

    public SearchStats GetLastSearchStats() {
        // TODO: Implement a way to track and return search statistics
        return new SearchStats("", "", "");
    }

    private void SingleIteration(MonteCarloNode rootNode, Evaluator evaluator) {
        // 1. Traverse the tree to select a node to expand
        MonteCarloNode currentNode = rootNode;
        int expandDepth = 0;
        while (currentNode != null && currentNode.Expanded) {
            currentNode = currentNode.VisitWeightedRandomChild();
            expandDepth++;
        }
        
        if (currentNode == null) {
            // No valid node found, return early
            return;
        }
        
        // 2. Generate child nodes with all possible moves, and evaluate them 
        currentNode.ExpandNode(evaluator);
        if (expandedDepthCount.TryGetValue(expandDepth, out int count)) {
            expandedDepthCount[expandDepth] = count + 1;
        } else {
            expandedDepthCount[expandDepth] = 1;
        }
        
        // 3. Apply backpropagation and recursively update the parent nodes
        Backpropagate(currentNode);
    }

    private void Backpropagate(MonteCarloNode node) {
        while (true) {
            // rebuild the selection weights used in the next iteration
            bool evaluationUpdated = node.UpdateNodeEvaluationFromChild();

            if (!evaluationUpdated || node.Parent is null || node.Parent.IsRoot) return;
            node.UpdateSelectionWeightInParent();
            node = (MonteCarloNode)node.Parent;
        }
    }
}