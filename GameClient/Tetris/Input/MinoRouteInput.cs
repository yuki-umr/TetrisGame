using System;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris.Input; 

public class MinoRouteInput : InputSystem {
    public MinoRoute Route { get; set; }
    
    private InputState inputState;

    public override InputState PopState() {
        InputState currentState = inputState;
        inputState.ResetState(true);
        return currentState;
    }

    public void Update() {
        if (Route == null || Route.Length <= 0) {
            // Console.Error.WriteLine("MinoRouteInput: No route found");
            return;
        }
        
        InputKey nextInput = Route.PopKey();
        inputState.SetState(nextInput, true);
    }
}