using System;
using System.Buffers;
using System.Collections.Generic;
using GameClient.Tetris.Input;

namespace GameClient.Tetris.Pathfinding; 

public class PathfinderAStar : Pathfinder {
    private static Vector2Int fieldSize;
    
    public override List<MinoPlacement> ListAllPossiblePlacements(int minoType, GameField field, bool useHold) {
        // List all possible MinoStates that can be determined only by collisions
        int variation = Mino.RotatedVariations[minoType];
        List<MinoState> placeable = new List<MinoState>();

        for (int rotation = 0; rotation < variation; rotation++) {
            Mino mino = new(minoType, rotation);
            for (int x = -2; x < field.Size.x; x++) {
                for (int y = -2; y <= field.FieldHeight; y++) {
                    // mino must not collide
                    if (field.WillCollideMino(mino, x, y)) continue;
                    // mino must be grounded
                    if (!field.WillCollideMino(mino, x, y - 1)) continue;
                    
                    placeable.Add(new MinoState(x, y, rotation));
                }
            }
        }

        List<MinoPlacement> placements = new(placeable.Count);
        foreach (MinoState minoState in placeable) {
            MinoRoute route = FindPath(minoType, field, minoState, useHold);
            if (!route.HasRoute) {
                // if no route is found, skip this placement
                continue;
            }
            
            MinoPlacement placement = new(minoState, route);
            placements.Add(placement);
        }

        return placements;
    }
    
    public MinoRoute FindPath(int minoType, GameField gameField, MinoState minoState, bool useHold) {
        PathNode[] nodes = GeneratePathNodeField(gameField.Size);
        
        MinoState endState = MinoState.InitialState;
        MinoState currentState = minoState;
        PathNode startNode = PathNode.CreateRootNode(minoState, endState);
        SetNode(nodes, currentState, startNode);
        bool firstMove = true;

        List<PathNode> adjacentNodes = new List<PathNode>();
        PriorityQueue<PathNode, int> openNodes = new PriorityQueue<PathNode, int>();
        while (true) {
            if (currentState == endState) break;
            PathNode currentNode = GetNode(nodes, currentState);
            
            // get all adjacent nodes
            adjacentNodes.Clear();
            Mino mino = new Mino(minoType, currentState.rotation);

            // move: spin
            for (int k = 0; k < 2; k++) {
                // check both rotations
                bool clockwise = k == 0;
                InputKey inputKey = k == 0 ? InputKey.SpinClock : InputKey.SpinCounterClock;
                int previousRotation = mino.RotationAfterSpin(!clockwise, currentState.rotation);
                for (int i = 0; i < 5; i++) {
                    // go back 1 step and test if the spin from there is valid
                    Vector2Int srsDelta = mino.WallKickDelta(clockwise, i, currentState.rotation);
                    Vector2Int previousPos = new Vector2Int(currentState.x - srsDelta.x, currentState.y - srsDelta.y);
                    Mino previousMino = mino.Rotated(previousRotation);
                    if (gameField.WillCollideMino(previousMino, previousPos.x, previousPos.y)) continue;

                    // try spinning from previous position and check if it matches
                    bool spinValid = true;
                    for (int j = 0; j < i; j++) {
                        Vector2Int forwardSrsDelta = mino.WallKickDelta(clockwise, j, currentState.rotation);
                        Vector2Int forwardPos = previousPos + forwardSrsDelta;
                        if (!gameField.WillCollideMino(mino, forwardPos.x, forwardPos.y)) {
                            // spin is invalid, it goes somewhere else so skip this srs pattern
                            spinValid = false;
                            break;
                        }
                    }

                    if (!spinValid) continue;
                    // spin is valid for srs pattern [i]
                    MinoState childMinoState = new MinoState(previousPos.x, previousPos.y, previousRotation);
                    PathNode childNode = currentNode.CreateChildNode(i, inputKey, childMinoState);
                    adjacentNodes.Add(childNode);
                }
            }
            
            // move: horizontal movement
            for (int i = 0; i < 2; i++) {
                int moveDirection = i == 0 ? -1 : 1;
                InputKey inputKey = i == 0 ? InputKey.Right : InputKey.Left;
                if (!gameField.WillCollideMino(mino, currentState.x + moveDirection, currentState.y)) {
                    MinoState childMinoState = new MinoState(currentState.x + moveDirection, currentState.y, currentState.rotation);
                    PathNode childNode = currentNode.CreateChildNode(0, inputKey, childMinoState);
                    adjacentNodes.Add(childNode);
                } 
            }
            
            // move: going up
            if (firstMove) {
                // if this is the first move, add all possible states that can be hard dropped to currentState
                int dropHeight = currentState.y + 1;
                while (!gameField.WillCollideMino(mino, currentState.x, dropHeight) && dropHeight <= endState.y) {
                    MinoState childMinoState = new MinoState(currentState.x, dropHeight, currentState.rotation);
                    PathNode childNode = currentNode.CreateChildNode(0, InputKey.HardDrop, childMinoState);
                    adjacentNodes.Add(childNode);
                    dropHeight++;
                }

                firstMove = false;
            } else if (!gameField.WillCollideMino(mino, currentState.x, currentState.y + 1)) {
                MinoState childMinoState = new MinoState(currentState.x, currentState.y + 1, currentState.rotation);
                PathNode childNode = currentNode.CreateChildNode(0, InputKey.SoftDrop, childMinoState);
                adjacentNodes.Add(childNode);
            }
            
            // map all adjacent nodes to nodes[]
            foreach (PathNode node in adjacentNodes) {
                PathNode existingNode = GetNode(nodes, node.state);
                // if the already existing node is better than this new node, skip this node
                if (existingNode != null && existingNode.GetScore() <= node.GetScore()) continue;
                SetNode(nodes, node.state, node);
                openNodes.Enqueue(node, node.GetScore());
            }

            if (openNodes.Count == 0) break;
            currentState = openNodes.Dequeue().state;
        }

        PathNode trackNode = GetNode(nodes, endState);
        if (trackNode == null) return MinoRoute.GetDefault();
        List<InputKey> inputs = new List<InputKey>();
        int lastSrs = -1;
        InputKey lastInput = InputKey.None;
        while (!trackNode.IsRootNode) {
            inputs.Add(trackNode.key);
            lastSrs = trackNode.srsType;
            lastInput = trackNode.key;
            trackNode = trackNode.GetParentNode(nodes);
        }

        if (inputs.Count > 0 && inputs[^1] != InputKey.HardDrop) {
            // if the last few inputs are softDrops, compress them with a single hard drop
            while (inputs[^1] == InputKey.SoftDrop) inputs.RemoveAt(inputs.Count - 1);
            inputs.Add(InputKey.HardDrop);
        }

        ArrayPool<PathNode>.Shared.Return(nodes, true);
        return new MinoRoute(inputs, lastInput, lastSrs, useHold);
    }

    private PathNode[] GeneratePathNodeField(Vector2Int size) {
        fieldSize = size + new Vector2Int(4, 4);
        return ArrayPool<PathNode>.Shared.Rent(size.x * size.y * 4);
    }

    private static PathNode GetNode(PathNode[] nodes, MinoState state) {
        return nodes[(state.x + 2) + (state.y + 2) * fieldSize.x + state.rotation * fieldSize.x * fieldSize.y];
    }

    private static void SetNode(PathNode[] nodes, MinoState state, PathNode node) {
        nodes[(state.x + 2) + (state.y + 2) * fieldSize.x + state.rotation * fieldSize.x * fieldSize.y] = node;
    }
    
    private class PathNode : IComparable<PathNode> {
        public readonly int steps, distEnd, fCost, srsType;
        public readonly InputKey key;
        public readonly MinoState state, endState;
        private readonly MinoState parentState;
        public bool IsRootNode { get; }

        private PathNode(int steps, int srsType, InputKey key, MinoState state, MinoState endState, MinoState? parentState) {
            this.steps = steps;
            this.srsType = srsType;
            this.key = key;
            this.state = state;
            this.endState = endState;
            if (parentState != null) this.parentState = parentState.Value;
            else IsRootNode = true;

            distEnd = CalculateStateDistance(state, endState);
            fCost = steps + distEnd;
        }

        public static PathNode CreateRootNode(MinoState state, MinoState endState) {
            return new PathNode(0, 0, InputKey.None, state, endState, null);
        }

        public PathNode CreateChildNode(int srsType, InputKey key, MinoState state) {
            return new PathNode(steps + 1, srsType, key, state, endState, this.state);
        }

        public PathNode GetParentNode(PathNode[] nodes) {
            if (IsRootNode) return null;
            return GetNode(nodes, parentState);
        }

        private static int CalculateStateDistance(MinoState a, MinoState b) {
            int dist = Math.Abs(a.rotation - b.rotation);
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) + (dist == 3 ? 1 : dist);
        }

        public int GetScore() {
            return fCost * 100 - state.y;
        }
    
        public int CompareTo(PathNode node) {
            return GetScore() - node.GetScore();
        }
    }
}

