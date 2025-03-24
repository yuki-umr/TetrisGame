using Microsoft.Xna.Framework.Input;

namespace GameClient.Tetris.Input; 

public class KeyboardInput : InputSystem {
    private InputState inputState;
    
    public override InputState PopState() {
        InputState currentState = inputState;
        inputState.ResetState();
        return currentState;
    }

    public void Update() {
        KeyboardState keyboard = Keyboard.GetState();
        inputState.SetState(InputKey.Left, keyboard.IsKeyDown(Keys.A));
        inputState.SetState(InputKey.Right, keyboard.IsKeyDown(Keys.D));
        inputState.SetState(InputKey.HardDrop, keyboard.IsKeyDown(Keys.W));
        inputState.SetState(InputKey.SoftDrop, keyboard.IsKeyDown(Keys.S));
        
        inputState.SetState(InputKey.SpinClock, keyboard.IsKeyDown(Keys.K));
        inputState.SetState(InputKey.SpinCounterClock, keyboard.IsKeyDown(Keys.J));
        inputState.SetState(InputKey.Hold, keyboard.IsKeyDown(Keys.L));
        
        inputState.SetState(InputKey.Pause, keyboard.IsKeyDown(Keys.Space));
    }
}