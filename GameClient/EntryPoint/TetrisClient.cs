using System;
using System.IO;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace GameClient.EntryPoint;

public class TetrisClient : WindowManager {
    private GameController gameController;
    private BotPlayer botPlayer;
    private InputSystem input;
    private bool botMode = false;

    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);

    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => minimumFrameTime;

    private int minimumFrameTime = 16, resetStep = -1;

    protected override void OnInitialize() {
        foreach (string arg in Program.GetOptions().Args) {
            if (arg == "ai") botMode = true;
            if (arg == "maxSpeed") minimumFrameTime = 1;
            if (arg.StartsWith("maxStep=")) resetStep = int.Parse(arg[8..]);
        }
        
        Setup();
    }

    private void Setup() {
        gameController = new GameController();

        if (botMode) {
            input = new MinoRouteInput();
            botPlayer = new StandardBotPlayer(gameController, (MinoRouteInput)input);
        } else {
            input = new KeyboardInput();
        }
    }

    protected override int OnUpdate() {
        if (botMode) {
            botPlayer.Update();
        } else {
            ((KeyboardInput)input).Update();
        }
        
        gameController.ProcessInput(input);
        gameController.Update();
        
        if (gameController.IsDead) Setup();
        else if (resetStep > 0 && gameController.Statistics.steps >= resetStep) {
            File.AppendAllLines("result.json", new [] {
                JsonConvert.SerializeObject(gameController.Statistics)
            });
            Setup();
        }

        return 0;
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        gameController.Draw(spriteBatch);
        if (botMode) botPlayer.Draw(spriteBatch);
        if (resetStep != 0) {
            Primitives.DrawText(spriteBatch, $"Step: {gameController.Statistics.steps}/{resetStep}", 480, 640, 12, Color.White);
        }
    }
}