using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris;

public class MonteCarloNode : StateNode {
    private readonly List<float> evaluations = new();
    private readonly int nodeIndex, nodeDepth;
    private Evaluation totalEvaluationToLeaf;
    
    // DOING bestChildIndex isn't pointing to the actual best child node
    private int bestChildIndex, visitCount = 1; // maxDepth describes the relative depth of the deepest child node from here
    
    public int MaxDepth { get; private set; }
    public int NodeDepth => nodeDepth;
    
    public static int CreatedChildNodesCount { get; set; }

    public MonteCarloNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode,
        MinoRoute route, bool useHold, int nodeIndex, int nodeDepth)
        : base(gameState, minoType, minoState, evaluation, parentNode, route, useHold) {
        this.nodeIndex = nodeIndex;
        this.nodeDepth = nodeDepth;
        totalEvaluationToLeaf = evaluation;
    }

    public static MonteCarloNode CreateRootNode(GameState gameState, IEvaluator evaluator) {
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

    public bool ExpandNode(IEvaluator evaluator) {
        if (Expanded) return ChildNodes.Count > 0;
        
        List<MinoPlacement> childPlacements = ListPossibleMinoPlacements(GameState.CurrentMino, false),
            holdChildPlacements = ListPossibleMinoPlacements(GameState.PeekMinoAfterHold(), true);

        ExpandChild(childPlacements, evaluator, false);
        ExpandChild(holdChildPlacements, evaluator, true);
        CreatedChildNodesCount += ChildNodes.Count;
        
        // recalculate the selection weights
        evaluations.Clear();
        foreach (StateNode node in ChildNodes) {
            MonteCarloNode childNode = (MonteCarloNode)node;
            float weight = childNode.CalculateSelectionWeight();
            evaluations.Add(weight);
            
            if (childNode.Evaluation > ChildNodes[bestChildIndex].Evaluation) {
                bestChildIndex = childNode.nodeIndex;
            }
        }
        
        bool expanded = ChildNodes.Count > 0;
        if (expanded) {
            MaxDepth = 1;
        }

        return expanded;
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
        parentNode.evaluations[nodeIndex] = newWeight;

        // update the best child index in the parent node (this node might get a worse evaluation than the previous best child)
        parentNode.bestChildIndex = 0;
        for (int i = 0; i < parentNode.ChildNodes.Count; i++) {
            MonteCarloNode childNode = (MonteCarloNode)parentNode.ChildNodes[i],
                bestChildNode = (MonteCarloNode)parentNode.ChildNodes[parentNode.bestChildIndex];
            if (bestChildNode.totalEvaluationToLeaf.Value < childNode.totalEvaluationToLeaf.Value) {
                parentNode.bestChildIndex = i;
            }
        }
    }

    public bool UpdateParentMaxDepth() {
        if (Parent == null) return false;
        MonteCarloNode parentNode = (MonteCarloNode)Parent;
        if (parentNode.MaxDepth <= MaxDepth) {
            parentNode.MaxDepth = MaxDepth + 1;
            return true;
        }

        return false;
    }
    
    public void LogNode() {
        Console.WriteLine($"AAA: childCount={ChildNodes.Count}, best={bestChildIndex}" +
                          $@", [{string.Join(", ", ChildNodes.Select((n, i) => {
                              MonteCarloNode node = (MonteCarloNode)n;
                              return $"({i}: {node.totalEvaluationToLeaf.Value}/{node.MaxDepth} {node.visitCount})";
                          }))}]");
        Span<float> selectionWeights = stackalloc float[evaluations.Count];
        CalculateChildSelectionWeights(ref selectionWeights, out float totalSelectionWeight);
        Console.WriteLine($"AAA: totalWeight={totalSelectionWeight}, [{string.Join(", ", selectionWeights.ToArray().Select((f, i) => $"({i}: {f})"))}]");
    }

    public MonteCarloNode GetBestChild() {
        if (ChildNodes is null || ChildNodes.Count == 0) return null;
        return (MonteCarloNode)ChildNodes[bestChildIndex];
    }

    public MonteCarloNode VisitWeightedRandomChild() {
        if (ChildNodes.Count == 0) {
            return null;
        }

        Span<float> selectionWeights = stackalloc float[evaluations.Count];
        CalculateChildSelectionWeights(ref selectionWeights, out float totalSelectionWeight);

        float randomValue = RandomGen.Float(totalSelectionWeight);
        MonteCarloNode node = (MonteCarloNode)ChildNodes[0];
        for (int i = 0; i < selectionWeights.Length; i++) {
            randomValue -= selectionWeights[i];
            if (randomValue > 0) continue;
            node = (MonteCarloNode)ChildNodes[i];
            break;
        }

        node.visitCount++;
        return node;
    }

    private void CalculateChildSelectionWeights(ref Span<float> selectionWeights, out float totalSelectionWeight) {
        if (evaluations.Count == 0) {
            totalSelectionWeight = 0;
            return;
        }
        
        totalSelectionWeight = 0;
        float worstSelectionWeight = float.MaxValue;
        for (int i = 0; i < evaluations.Count; i++) {
            float weight = evaluations[i];
            if (weight < worstSelectionWeight) worstSelectionWeight = weight;
        }
        
        for (int i = 0; i < evaluations.Count; i++) {
            float weight = evaluations[i] - worstSelectionWeight; // normalize the selection weights
            weight *= 0.01f;
            int visits = ((MonteCarloNode)ChildNodes[i]).visitCount;
            float selectionWeight = (float)Math.Pow(weight, 5); // really trying to favor the better nodes 
            totalSelectionWeight += selectionWeight;
            selectionWeights[i] = selectionWeight;
        }
    }

    private float CalculateSelectionWeight() {
        return totalEvaluationToLeaf.Value;
        // use the total evaluation to leaf to calculate the selection weight
        float rate = Math.Sign(totalEvaluationToLeaf.Value) * totalEvaluationToLeaf.Value * totalEvaluationToLeaf.Value;
        
        // if (visitCount > 0) return rate / (visitCount * visitCount);
        
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