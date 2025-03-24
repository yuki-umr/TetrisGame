using GameClient.Tetris;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameClient.EntryPoint;

public class GameWindow : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private WindowManager manager;

    public GameWindow() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        manager = (WindowManager)Program.GetOptions().GetSubClassInstance();
        _graphics.PreferredBackBufferWidth = manager.InitialWindowSize.x;
        _graphics.PreferredBackBufferHeight = manager.InitialWindowSize.y;
    }

    protected override void Initialize() {
        manager.Initialize();
        IsFixedTimeStep = manager.FixedTimeStep;
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Primitives.Initialize(GraphicsDevice);
        manager.LoadContent(GraphicsDevice);
    }

    protected override void UnloadContent() {
        Primitives.Unload();
        base.UnloadContent();
    }

    private int timeTotal;
    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        timeTotal += gameTime.ElapsedGameTime.Milliseconds;
        if (manager.MinimumFrameTime <= 0 || timeTotal > manager.MinimumFrameTime) {
            timeTotal = 0;
            if (manager.Update() == -1) Exit();
        }
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();
        manager.Draw(GraphicsDevice, _spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}