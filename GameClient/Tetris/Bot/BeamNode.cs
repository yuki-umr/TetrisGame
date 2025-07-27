using System.Collections.Generic;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris;

public class BeamNode : StateNode {
    private BeamNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode,
        MinoRoute route, bool useHold) 
        : base(gameState, minoType, minoState, evaluation, parentNode, route, useHold) {
    }

    public static BeamNode CreateRootNode(GameState gameState, IEvaluator evaluator) {
        int fieldEvaluation = evaluator.EvaluateField(gameState.Field, out List<PatternMatchData> patterns);
        Evaluation eval = new(fieldEvaluation, 0, default, gameState, patterns);
        return new BeamNode(gameState, -1, default, eval, null, null, false);
    }

    protected override void CreateChild(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, MinoRoute route, bool useHold) {
        BeamNode node = new(gameState, minoType, minoState, evaluation, this, route, useHold);
        ChildNodes.Add(node);
    }

    public static PriorityQueue<StateNode, int> ExpandNodes(List<StateNode> nodes, IEvaluator evaluator) {
        PriorityQueue<StateNode, int> newNodes = new PriorityQueue<StateNode, int>(nodes.Count * 40);
        foreach (StateNode node in nodes) {
            if (node.Expanded) {
                foreach (StateNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, -childNode.GetEvaluationTotalFromRoot()); // negate the evaluation to prioritize "higher" scores
                }
            } else {
                List<MinoPlacement> childPlacements = node.ListPossibleMinoPlacements(node.GameState.CurrentMino, false),
                    holdChildPlacements = node.ListPossibleMinoPlacements(node.GameState.PeekMinoAfterHold(), true);

                node.ExpandChild(childPlacements, evaluator, false);
                node.ExpandChild(holdChildPlacements, evaluator, true);
                
                foreach (StateNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, -childNode.GetEvaluationTotalFromRoot());
                }
            }
        }

        return newNodes;
    }
}