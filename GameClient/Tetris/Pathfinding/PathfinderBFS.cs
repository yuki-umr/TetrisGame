using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Tetris.Input;

namespace GameClient.Tetris.Pathfinding;

public class PathfinderBFS : Pathfinder {
    public override List<MinoPlacement> ListAllPossiblePlacements(int minoType, GameField field, bool useHold) {
        Dictionary<MinoState, MinoPlacement> placements = new();
        Dictionary<MinoState, Node> visitedNodes = new(128);
        MinoState initialState = MinoState.InitialState;
        PriorityQueue<MinoState, int> checkQueue = new();
        
        visitedNodes[initialState] = Node.CreateRootNode(initialState);
        checkQueue.Enqueue(initialState, 0);

        while (checkQueue.TryDequeue(out MinoState stateFrom, out _)) {
            // check all possible moves from here
            // if the new state has not been visited yet, add it to the queue

            Node currentNode = visitedNodes[stateFrom];
            AddNodeAfterInput(currentNode, stateFrom, InputKey.Left);
            AddNodeAfterInput(currentNode, stateFrom, InputKey.Right);
            AddNodeAfterInput(currentNode, stateFrom, InputKey.SpinClock);
            AddNodeAfterInput(currentNode, stateFrom, InputKey.SpinCounterClock);
            AddNodeAfterInput(currentNode, stateFrom, InputKey.SoftDrop);
            
            // add the hard dropped state as a valid placement
            TryAddPlacement(stateFrom);
        }

        if (placements.Count > 0) Console.WriteLine(placements.Values.First().route.ToString());
        return new List<MinoPlacement>(placements.Values);

        void AddNodeAfterInput(Node node, MinoState currentState, InputKey input) {
            Mino mino = new(minoType, currentState.rotation);
            MinoState newState;
            Node newNode;

            if (input is InputKey.Left or InputKey.Right) {
                int delta = input is InputKey.Left ? -1 : 1;
                newState = new MinoState(currentState.x + delta, currentState.y, currentState.rotation);
                if (field.WillCollideMino(mino, newState.x, newState.y)) return;
                newNode = node.CreateChildNode(currentState, input);
            } else if (input is InputKey.SpinClock or InputKey.SpinCounterClock) {
                bool clockwise = input is InputKey.SpinClock;
                if (!GameController.TryPerformRotation(field, mino, currentState, clockwise, out newState, out int srsPattern))
                    return;
                newNode = node.CreateChildNode(currentState, input, srsPattern);
            } else if (input is InputKey.SoftDrop) {
                newState = new MinoState(currentState.x, currentState.y - 1, currentState.rotation);
                if (field.WillCollideMino(mino, newState.x, newState.y)) return;
                newNode = node.CreateChildNode(currentState, input);
            } else {
                return;
            }
            
            // if the new rotated state has been visited, skip it (there should be a better route in the queue deriving from this state)
            if (!visitedNodes.TryAdd(newState, newNode)) return;
            checkQueue.Enqueue(newState, newNode.inputLength);
        }

        void TryAddPlacement(MinoState state) {
            Node startingNode = visitedNodes[state];
            Mino mino = new(minoType, state.rotation);
            MinoState groundedState = state;
            bool movedOnDrop = false;
            // check for grounded position from top to bottom
            // TODO: this could be optimized by checking the field height
            while (!field.WillCollideMino(mino, groundedState.x, groundedState.y - 1)) {
                groundedState = new MinoState(groundedState.x, groundedState.y - 1, groundedState.rotation);
                movedOnDrop = true;
            }

            if (placements.ContainsKey(groundedState)) return; // don't allow duplicates
            
            // mino is grounded at checkState, assuming that the original state is available for placement
            List<InputKey> reversedRoute = new();
            MinoState routeState = state;
            while (visitedNodes.TryGetValue(routeState, out Node node) && !node.isRootNode) {
                // add the input that led to this state
                reversedRoute.Add(node.input);
                
                // go back to the previous state
                routeState = node.previousState;
            }

            // reverse the route to get the correct order of inputs
            List<InputKey> inputs = new(reversedRoute.Count);
            for (int i = reversedRoute.Count - 1; i >= 0; i--) {
                inputs.Add(reversedRoute[i]);
            }
            
            InputKey lastInput = movedOnDrop ? InputKey.HardDrop : inputs[^1];
            inputs.Add(InputKey.HardDrop); // add a hard drop anyway to finalize the placement
            MinoRoute route = new(inputs, lastInput, startingNode.lastSrsPattern, useHold);
            MinoPlacement placement = new(groundedState, route);
            
            placements[groundedState] = placement;
        }
    }

    private readonly struct Node {
        public readonly MinoState previousState;
        public readonly InputKey input; // input applied from the previous node
        public readonly int lastSrsPattern, inputLength; // length of the input sequence that led to this node
        public readonly bool isRootNode;
        
        private Node(MinoState previousState, InputKey input, int lastSrsPattern, int inputLength, bool isRootNode) {
            this.previousState = previousState;
            this.input = input;
            this.lastSrsPattern = lastSrsPattern;
            this.inputLength = inputLength;
            this.isRootNode = isRootNode;
        }
        
        public static Node CreateRootNode(MinoState initialState) => new(initialState, InputKey.None, 0, 0, true);
        
        public Node CreateChildNode(MinoState parentState, InputKey input, int lastSrsPattern = 0) {
            return new Node(parentState, input, lastSrsPattern, inputLength + 1, false);
        }
    }
}