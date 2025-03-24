using GameClient.Tetris;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.EntryPoint;

public abstract class WindowManager {
    public void Initialize() {
        OnInitialize();
    }

    public void LoadContent(GraphicsDevice graphicsDevice) {
        OnLoadContent(graphicsDevice);
    }
    
    public int Update() {
        return OnUpdate();
    }

    public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        OnDraw(graphicsDevice, spriteBatch);
    }

    public abstract Vector2Int InitialWindowSize { get; }
    public abstract bool FixedTimeStep { get; }
    public virtual int MinimumFrameTime => 16;
    protected virtual void OnInitialize() { }
    protected virtual void OnLoadContent(GraphicsDevice graphicsDevice) { }
    protected virtual int OnUpdate() => 0;
    protected virtual void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) { }
}