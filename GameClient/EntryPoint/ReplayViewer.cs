using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.EntryPoint;

public class ReplayViewer : WindowManager {
    private KeyboardInput inputSystem;
    
    public override Vector2Int InitialWindowSize => new(800, 600);
    public override bool FixedTimeStep => true;

    protected override void OnInitialize() {
        inputSystem = new KeyboardInput();
    }

    protected override int OnUpdate() {
        inputSystem.Update();
        InputState inputState = inputSystem.PopState();

        return 0;
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        
    }
}