using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using GameClient.Tetris.Input;

namespace GameClient.Tetris.Pathfinding; 

public static class Pathfinder {
    private static Vector2Int fieldSize;
    
    public static MinoRoute FindPath(int minoType, GameField gameField, MinoState minoState) {
        PathNode[] nodes = GeneratePathNodeField(gameField.Size);
        
        MinoState endState = new MinoState(Constants.MinoSpawnPosition.x, Constants.MinoSpawnPosition.y, 0);
        MinoState currentState = minoState;
        PathNode startNode = PathNode.CreateRootNode(minoState, endState);
        nodes.SetNode(currentState, startNode);
        bool firstMove = true;

        List<PathNode> adjacentNodes = new List<PathNode>();
        PriorityQueue<PathNode, int> openNodes = new PriorityQueue<PathNode, int>();
        while (true) {
            if (currentState == endState) break;
            PathNode currentNode = nodes.GetNode(currentState);
            
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
                PathNode existingNode = nodes.GetNode(node.state);
                // if the already existing node is better than this new node, skip this node
                if (existingNode != null && existingNode.GetScore() <= node.GetScore()) continue;
                nodes.SetNode(node.state, node);
                openNodes.Enqueue(node, node.GetScore());
            }

            if (openNodes.Count == 0) break;
            currentState = openNodes.Dequeue().state;
        }

        PathNode trackNode = nodes.GetNode(endState);
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
        return new MinoRoute(inputs, lastInput, lastSrs);
    }

    private static PathNode[] GeneratePathNodeField(Vector2Int size) {
        fieldSize = size + new Vector2Int(4, 4);
        return ArrayPool<PathNode>.Shared.Rent(size.x * size.y * 4);
    }

    private static PathNode GetNode(this PathNode[] nodes, MinoState state) {
        return nodes[(state.x + 2) + (state.y + 2) * fieldSize.x + state.rotation * fieldSize.x * fieldSize.y];
    }

    private static void SetNode(this PathNode[] nodes, MinoState state, PathNode node) {
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
            return nodes.GetNode(parentState);
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

