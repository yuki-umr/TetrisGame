using System;
using System.Collections.Generic;

namespace GameClient.Tetris;

public class MonteCarloNode : StateNode {
    private readonly List<float> selectionWeights = new();
    private readonly int nodeIndex;
    private Evaluation totalEvaluationToLeaf;
    
    private int bestChildIndex, visitCount;
    private float totalSelectionWeight;

    public MonteCarloNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode,
        bool useHold, int nodeIndex)
        : base(gameState, minoType, minoState, evaluation, parentNode, useHold) {
        this.nodeIndex = nodeIndex;
        totalEvaluationToLeaf = evaluation;
    }

    public static MonteCarloNode CreateRootNode(GameState gameState, Evaluator evaluator) {
        int fieldEvaluation = evaluator.EvaluateField(gameState.Field, out List<PatternMatchData> patterns);
        Evaluation eval = new(fieldEvaluation, 0, default, gameState, patterns);
        MonteCarloNode rootNode = new(gameState, -1, default, eval, null, false, 0);
        return rootNode;
    }

    protected override void CreateChild(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, bool useHold) {
        int newNodeIndex = ChildNodes.Count;
        MonteCarloNode node = new(gameState, minoType, minoState, evaluation, this, useHold, newNodeIndex);
        ChildNodes.Add(node);
    }

    public void ExpandNode(Evaluator evaluator) {
        if (Expanded) return;
        
        List<MinoState> childStates = ListMinoPlaceable(GameState.CurrentMino),
            holdChildStates = ListMinoPlaceable(GameState.PeekMinoAfterHold());

        ExpandChild(childStates, evaluator, false);
        ExpandChild(holdChildStates, evaluator, true);
        
        // recalculate the selection weights
        selectionWeights.Clear();
        totalSelectionWeight = 0f;
        foreach (StateNode node in ChildNodes) {
            MonteCarloNode childNode = (MonteCarloNode)node;
            float weight = childNode.CalculateSelectionWeight();
            totalSelectionWeight += weight;
            selectionWeights.Add(totalSelectionWeight);
            
            if (childNode.Evaluation > ChildNodes[bestChildIndex].Evaluation) {
                bestChildIndex = childNode.nodeIndex;
            }
        }
    }

    // 
    public bool UpdateNodeEvaluationFromChild() {
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
        
        if (Evaluation > parentNode.ChildNodes[parentNode.bestChildIndex].Evaluation) {
            parentNode.bestChildIndex = nodeIndex;
        }
    }

    public MonteCarloNode GetBestChild() {
        if (ChildNodes.Count == 0) return null;
        return (MonteCarloNode)ChildNodes[bestChildIndex];
    }

    public MonteCarloNode VisitWeightedRandomChild() {
        if (ChildNodes.Count == 0) return null;

        float randomValue = Random.Shared.NextSingle() * totalSelectionWeight;
        MonteCarloNode node = (MonteCarloNode)ChildNodes[0];
        for (int i = 0; i < selectionWeights.Count; i++) {
            if (!(randomValue < selectionWeights[i])) continue;
            node = (MonteCarloNode)ChildNodes[i];
            break;
        }

        node.visitCount++;
        return node;
    }

    private float CalculateSelectionWeight() {
        // use the total evaluation to leaf to calculate the selection weight
        
        // DOING: doesn't this cause the selection weight to be biased towards the "most explored" nodes and cause a loop?
        return 0;
    }
}