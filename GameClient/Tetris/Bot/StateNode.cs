using System;
using System.Collections.Generic;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris; 

public abstract class StateNode : IComparable<StateNode> {
    public StateNode Parent { get; }
    public MinoState MinoState { get; }
    public int MinoType { get; }
    public GameState GameState { get; }
    public Evaluation Evaluation { get; }
    public List<StateNode> ChildNodes { get; set; }
    public bool UseHold { get; }
    public int NodeRank { get; set; }

    private MinoRoute route;
    private bool convertedToRoot;

    public bool IsRoot => Parent == null || convertedToRoot;
    public bool Expanded => ChildNodes != null;

    protected abstract void CreateChild(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, bool useHold);
    
    protected StateNode(GameState gameState, int minoType, MinoState minoState, Evaluation evaluation, StateNode parentNode, bool useHold) {
        GameState = gameState.Copy();
        MinoType = minoType;
        MinoState = minoState;
        Evaluation = evaluation;
        Parent = parentNode;
        UseHold = useHold;
    }

    public void ConvertToRootNode() {
        convertedToRoot = true;
    }

    public StateNode GetSubRootNode() {
        if (IsRoot) {
            Console.Error.WriteLine("StateNode::GetSubRootNode is being called on a root node, returning null");
            return null;
        }
        
        StateNode node = this;
        while (!node.Parent.IsRoot) node = node.Parent;
        return node;
    }

    public List<StateNode> GetNodesFromRoot() {
        if (IsRoot) return new List<StateNode>();
        List<StateNode> nodes = Parent.GetNodesFromRoot();
        nodes.Add(this);
        return nodes;
    }

    public int GetEvaluationTotalFromRoot() {
        if (IsRoot) {
            Console.Error.WriteLine("StateNode::GetEvaluationTotalFromRoot is being called on a root node, returning 0");
            return 0;
        }

        return Parent.GetEvaluationTotalInternal() + Evaluation.Value;
    }

    private int GetEvaluationTotalInternal() {
        if (IsRoot) return 0;
        return Parent.GetEvaluationTotalInternal() + Evaluation.movement;
    }

    public void ExpandChild(List<MinoState> childStates, Evaluator evaluator, bool useHold) {
        ChildNodes ??= new List<StateNode>();
        foreach (MinoState state in childStates) {
            GameState gameStateBeforeLock = GameState;
            if (useHold) {
                gameStateBeforeLock = GameState.Copy();
                gameStateBeforeLock.HoldMino();
            }

            int currentMinoType = gameStateBeforeLock.CurrentMino;
            Mino mino = new(currentMinoType, 0);
            Evaluation eval = evaluator.EvaluateMove(gameStateBeforeLock, mino, state, Evaluation.patternsFound);
            GameState nextState = eval.gameStateAfterMove;

            CreateChild(nextState, currentMinoType, state, eval, useHold);
        }
    }

    public MinoRoute GetRoute() {
        if (IsRoot) {
            Console.Error.WriteLine("StateNode Error: should not be calling GetRoute() on root node");
            return MinoRoute.GetDefault();
        }

        if (route == null) {
            // if there are other rotation variations, also try that (except for O piece)
            int variation = Mino.RotatedVariations[MinoType], testRotation = MinoState.rotation;
            do {
                route = Pathfinder.FindPath(MinoType, Parent.GameState.Field, MinoState, UseHold);
                testRotation += variation;
            } while (!route.HasRoute && testRotation < 4 && variation != 0);
        }

        return route;
    }
    
    public bool HasRoute() {
        return IsRoot || GetRoute().HasRoute;
    }

    public List<MinoState> ListMinoPlaceable(int minoType) {
        int variation = Mino.RotatedVariations[minoType];
        List<MinoState> placeable = new List<MinoState>();

        for (int rotation = 0; rotation < variation; rotation++) {
            Mino mino = new Mino(minoType, rotation);
            for (int x = -2; x < GameState.Field.Size.x; x++) {
                for (int y = -2; y <= GameState.Field.FieldHeight; y++) {
                    // mino must not collide
                    if (GameState.Field.WillCollideMino(mino, x, y)) continue;
                    // mino must be grounded
                    if (!GameState.Field.WillCollideMino(mino, x, y - 1)) continue;
                    
                    placeable.Add(new MinoState(x, y, rotation));
                }
            }
        }

        return placeable;
    }

    public int CompareTo(StateNode other) {
        return GetEvaluationTotalFromRoot() - other.GetEvaluationTotalFromRoot();
    }
}