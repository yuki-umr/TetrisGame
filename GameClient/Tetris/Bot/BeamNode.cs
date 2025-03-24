using System;
using System.Collections.Generic;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris; 

public class BeamNode : IComparable<BeamNode> {
    public BeamNode Parent { get; }
    public MinoState MinoState { get; }
    public int MinoType { get; }
    public GameState GameState { get; }
    public Evaluation Evaluation { get; }
    private List<BeamNode> ChildNodes { get; set; }
    public bool UseHold { get; }
    public int NodeRank { get; set; }

    private MinoRoute route;
    private bool convertedToRoot;

    public bool IsRoot => Parent == null || convertedToRoot;
    private bool Expanded => ChildNodes != null;

    private BeamNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, BeamNode parentNode, bool useHold) {
        GameState = gameState.Copy();
        MinoType = minoType;
        MinoState = minoState;
        Evaluation = evaluation;
        Parent = parentNode;
        UseHold = useHold;
    }

    public static BeamNode CreateRootNode(GameState gameState, Evaluator evaluator) {
        int fieldEvaluation = evaluator.EvaluateField(gameState.Field, out List<PatternMatchData> patterns);
        Evaluation eval = new Evaluation(fieldEvaluation, 0, default, gameState, patterns);
        return new BeamNode(gameState, -1, default, eval, null, false);
    }

    public void ConvertToRootNode() {
        convertedToRoot = true;
    }

    public BeamNode GetSubRootNode() {
        if (IsRoot) {
            Console.Error.WriteLine("BeamNode::GetSubRootNode is being called on a root node, returning null");
            return null;
        }
        
        BeamNode node = this;
        while (!node.Parent.IsRoot) node = node.Parent;
        return node;
    }

    public List<BeamNode> GetNodesFromRoot() {
        if (IsRoot) return new List<BeamNode>();
        List<BeamNode> nodes = Parent.GetNodesFromRoot();
        nodes.Add(this);
        return nodes;
    }

    public int GetEvaluationTotalFromRoot() {
        if (IsRoot) {
            Console.Error.WriteLine("BeamNode::GetEvaluationTotalFromRoot is being called on a root node, returning 0");
            return 0;
        }

        return Parent.GetEvaluationTotalInternal() + Evaluation.Value;
    }

    private int GetEvaluationTotalInternal() {
        if (IsRoot) return 0;
        return Parent.GetEvaluationTotalInternal() + Evaluation.movement;
    }

    private void ExpandChild(List<MinoState> childStates, Evaluator evaluator, bool useHold) {
        ChildNodes ??= new List<BeamNode>();
        foreach (MinoState state in childStates) {
            GameState gameStateBeforeLock = GameState;
            if (useHold) {
                gameStateBeforeLock = GameState.Copy();
                gameStateBeforeLock.HoldMino();
            }

            int currentMinoType = gameStateBeforeLock.CurrentMino;
            Mino mino = new Mino(currentMinoType, 0);
            Evaluation eval = evaluator.EvaluateMove(gameStateBeforeLock, mino, state, Evaluation.patternsFound);
            GameState nextState = eval.gameStateAfterMove;

            ChildNodes.Add(new BeamNode(nextState, currentMinoType, state, eval, this, useHold));
        }
    }

    public MinoRoute GetRoute() {
        if (IsRoot) {
            Console.Error.WriteLine("BeamNode Error: should not be calling GetRoute() on root node");
            return MinoRoute.GetDefault();
        }

        if (route == null) {
            // if there are other rotation variations, also try that (except for O piece)
            int variation = Mino.RotatedVariations[MinoType], testRotation = MinoState.rotation;
            do {
                route = Pathfinder.FindPath(MinoType, Parent.GameState.Field, MinoState);
                testRotation += variation;
            } while (!route.HasRoute && testRotation < 4 && variation != 0);
            
            if (UseHold) route.SetUseHold();
        }

        return route;
    }
    
    public bool HasRoute() {
        return IsRoot || GetRoute().HasRoute;
    }

    public static PriorityQueue<BeamNode, int> ExpandNodes(List<BeamNode> nodes, Evaluator evaluator) {
        PriorityQueue<BeamNode, int> newNodes = new PriorityQueue<BeamNode, int>(nodes.Count * 40);
        foreach (BeamNode node in nodes) {
            if (node.Expanded) {
                foreach (BeamNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, childNode.GetEvaluationTotalFromRoot());
                }
            } else {
                List<MinoState> childStates = ListMinoPlaceable(node.GameState, node.GameState.CurrentMino),
                    holdChildStates = ListMinoPlaceable(node.GameState, node.GameState.PeekMinoAfterHold());

                node.ExpandChild(childStates, evaluator, false);
                node.ExpandChild(holdChildStates, evaluator, true);
                
                foreach (BeamNode childNode in node.ChildNodes) {
                    newNodes.Enqueue(childNode, childNode.GetEvaluationTotalFromRoot());
                }
            }
        }

        return newNodes;
    }

    private static List<MinoState> ListMinoPlaceable(GameState gameState, int minoType) {
        int variation = Mino.RotatedVariations[minoType];
        List<MinoState> placeable = new List<MinoState>();

        for (int rotation = 0; rotation < variation; rotation++) {
            Mino mino = new Mino(minoType, rotation);
            for (int x = -2; x < gameState.Field.Size.x; x++) {
                for (int y = -2; y <= gameState.Field.FieldHeight; y++) {
                    // mino must not collide
                    if (gameState.Field.WillCollideMino(mino, x, y)) continue;
                    // mino must be grounded
                    if (!gameState.Field.WillCollideMino(mino, x, y - 1)) continue;
                    
                    placeable.Add(new MinoState(x, y, rotation));
                }
            }
        }

        return placeable;
    }

    public int CompareTo(BeamNode other) {
        return GetEvaluationTotalFromRoot() - other.GetEvaluationTotalFromRoot();
    }
}