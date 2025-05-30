using System;
using GameClient.Tetris.Pathfinding;

namespace GameClient.Tetris.Input; 

public class MinoRouteInput : InputSystem {
    public MinoRoute Route { get; private set; }
    
    public int InputsLeft => Route?.GetLength(ref cursor) ?? 0;
    
    private MinoRoute.Cursor cursor;
    private InputState inputState;

    public override InputState PopState() {
        InputState currentState = inputState;
        inputState.ResetState(true);
        return currentState;
    }

    public void SetCurrentRoute(MinoRoute route) {
        Route = route;
        cursor = route.CreateCursor();
    }

    public void Update() {
        if (Route == null || Route.GetLength(ref cursor) <= 0) {
            // Console.Error.WriteLine("MinoRouteInput: No route found");
            return;
        }
        
        InputKey nextInput = Route.PopKey(ref cursor);
        inputState.SetState(nextInput, true);
    }
}