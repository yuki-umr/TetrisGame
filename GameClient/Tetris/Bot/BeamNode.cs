using System.Collections.Generic;

namespace GameClient.Tetris;

public class BeamNode : StateNode {
    private BeamNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode, bool useHold) 
        : base(gameState, minoType, minoState, evaluation, parentNode, useHold) {
    }

    public static BeamNode CreateRootNode(GameState gameState, Evaluator evaluator) {
        int fieldEvaluation = evaluator.EvaluateField(gameState.Field, out List<PatternMatchData> patterns);
        Evaluation eval = new(fieldEvaluation, 0, default, gameState, patterns);
        return new BeamNode(gameState, -1, default, eval, null, false);
    }

    protected override void CreateChild(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, bool useHold) {
        BeamNode node = new(gameState, minoType, minoState, evaluation, this, useHold);
        ChildNodes.Add(node);
    }

    public static PriorityQueue<StateNode, int> ExpandNodes(List<StateNode> nodes, Evaluator evaluator) {
        PriorityQueue<StateNode, int> newNodes = new PriorityQueue<StateNode, int>(nodes.Count * 40);
        foreach (StateNode node in nodes) {
            if (node.Expanded) {
                foreach (StateNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, childNode.GetEvaluationTotalFromRoot());
                }
            } else {
                List<MinoState> childStates = node.ListMinoPlaceable(node.GameState.CurrentMino),
                    holdChildStates = node.ListMinoPlaceable(node.GameState.PeekMinoAfterHold());

                node.ExpandChild(childStates, evaluator, false);
                node.ExpandChild(holdChildStates, evaluator, true);
                
                foreach (StateNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, childNode.GetEvaluationTotalFromRoot());
                }
            }
        }

        return newNodes;
    }
}