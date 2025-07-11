using System;
using System.Collections.Generic;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris;

public class MonteCarloNode : StateNode {
    private readonly List<float> selectionWeights = new();
    private readonly int nodeIndex, nodeDepth;
    private Evaluation totalEvaluationToLeaf;
    
    private int bestChildIndex, visitCount;
    private float totalSelectionWeight;
    
    public int NodeDepth => nodeDepth;
    
    public static int CreatedChildNodesCount { get; set; }

    public MonteCarloNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode,
        MinoRoute route, bool useHold, int nodeIndex, int nodeDepth)
        : base(gameState, minoType, minoState, evaluation, parentNode, route, useHold) {
        this.nodeIndex = nodeIndex;
        this.nodeDepth = nodeDepth;
        totalEvaluationToLeaf = evaluation;
    }

    public static MonteCarloNode CreateRootNode(GameState gameState, Evaluator evaluator) {
        int fieldEvaluation = evaluator.EvaluateField(gameState.Field, out List<PatternMatchData> patterns);
        Evaluation eval = new(fieldEvaluation, 0, default, gameState, patterns);
        MonteCarloNode rootNode = new(gameState, -1, default, eval, null, null, false, 0, 0);
        return rootNode;
    }

    protected override void CreateChild(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, MinoRoute route, bool useHold) {
        int newNodeIndex = ChildNodes.Count;
        MonteCarloNode node = new(gameState, minoType, minoState, evaluation, this, route, useHold, newNodeIndex, nodeDepth + 1);
        ChildNodes.Add(node);
    }

    public bool ExpandNode(Evaluator evaluator) {
        if (Expanded) return ChildNodes.Count > 0;
        
        List<MinoPlacement> childPlacements = ListPossibleMinoPlacements(GameState.CurrentMino, false),
            holdChildPlacements = ListPossibleMinoPlacements(GameState.PeekMinoAfterHold(), true);

        ExpandChild(childPlacements, evaluator, false);
        ExpandChild(holdChildPlacements, evaluator, true);
        CreatedChildNodesCount += ChildNodes.Count;
        
        // recalculate the selection weights
        selectionWeights.Clear();
        totalSelectionWeight = 0f;
        foreach (StateNode node in ChildNodes) {
            MonteCarloNode childNode = (MonteCarloNode)node;
            float weight = childNode.CalculateSelectionWeight();
            totalSelectionWeight += weight;
            selectionWeights.Add(weight);
            
            if (childNode.Evaluation > ChildNodes[bestChildIndex].Evaluation) {
                bestChildIndex = childNode.nodeIndex;
            }
        }

        return ChildNodes.Count > 0;
    }

    // 
    public bool UpdateNodeEvaluationFromChild() {
        if (ChildNodes.Count == 0) {
            totalEvaluationToLeaf = new Evaluation(0, 0, Evaluation.result, Evaluation.gameStateAfterMove, Evaluation.patternsFound);
            return true; // force update to 0 if no children exist
        }
        
        Evaluation bestChild = GetBestChild().totalEvaluationToLeaf;
        
        // the parent node's evaluation will be represented by "the best leaf node's field eval" + "all rewards summed up to that point"
        Evaluation updatedEvaluation = new(bestChild.Value, Evaluation.movement, 
            Evaluation.result, Evaluation.gameStateAfterMove, Evaluation.patternsFound);
        
        if (updatedEvaluation > bestChild) {
            totalEvaluationToLeaf = updatedEvaluation;
            return true;
        }

        return false;
    }

    // Called during the backpropagation phase of MCTS, this updates this node's selection weight from the parent node
    public void UpdateSelectionWeightInParent() {
        if (Parent == null) return;
        float newWeight = CalculateSelectionWeight();
        
        MonteCarloNode parentNode = (MonteCarloNode)Parent;
        float originalWeight = parentNode.selectionWeights[nodeIndex];
        parentNode.selectionWeights[nodeIndex] = newWeight;
        parentNode.totalSelectionWeight += newWeight - originalWeight;

        // update the best child index in the parent node (this node might get a worse evaluation than the previous best child)
        parentNode.bestChildIndex = 0;
        for (int childIndex = 0; childIndex < parentNode.ChildNodes.Count; childIndex++) {
            if (parentNode.ChildNodes[childIndex].Evaluation <= parentNode.ChildNodes[parentNode.bestChildIndex].Evaluation) continue;
            parentNode.bestChildIndex = childIndex;
        }
    }

    public MonteCarloNode GetBestChild() {
        if (ChildNodes is null || ChildNodes.Count == 0) return null;
        return (MonteCarloNode)ChildNodes[bestChildIndex];
    }

    public MonteCarloNode VisitWeightedRandomChild() {
        if (ChildNodes.Count == 0) return null;

        float randomValue = RandomGen.Float(totalSelectionWeight);
        MonteCarloNode node = (MonteCarloNode)ChildNodes[0];
        for (int i = 0; i < selectionWeights.Count; i++) {
            randomValue -= selectionWeights[i];
            if (randomValue > 0) continue;
            node = (MonteCarloNode)ChildNodes[i];
            break;
        }

        node.visitCount++;
        return node;
    }

    private float CalculateSelectionWeight() {
        // use the total evaluation to leaf to calculate the selection weight
        float rate = totalEvaluationToLeaf.Value * totalEvaluationToLeaf.Value;
        if (visitCount > 0) return rate / (visitCount * visitCount);
        
        return rate;
    }
    
    public int GetSearchDepth() {
        return GetDeepestDepth(this);

        static int GetDeepestDepth(MonteCarloNode node) {
            if (!node.Expanded) return node.nodeDepth; 
            
            int maxVisitedNodeIndex = 0;
            for (int i = 0; i < node.ChildNodes.Count; i++) {
                MonteCarloNode childNode = (MonteCarloNode)node.ChildNodes[i];
                if (childNode.visitCount > ((MonteCarloNode)node.ChildNodes[maxVisitedNodeIndex]).visitCount) {
                    maxVisitedNodeIndex = i;
                }
            }
            
            return GetDeepestDepth((MonteCarloNode)node.ChildNodes[maxVisitedNodeIndex]);
        }
    }
}