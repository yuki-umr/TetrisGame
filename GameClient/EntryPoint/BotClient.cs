using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace GameClient.EntryPoint;

public class BotClient : WindowManager {
    private const string LogFolderName = "log";
    
    // Tests all patterns of settings in this array
    private string[] allSettings = Array.Empty<string>();
    
    private GameController gameController;
    private BotPlayer botPlayer;
    private MinoRouteInput input;

    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);

    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => 0;

    private PlayOutResult currentPlayOut;
    private int resetStep = -1, maxAttempt, initialSeed, attempt, settingsIndex;

    protected override void OnInitialize() {
        foreach (string arg in Program.GetOptions().Args) {
            if (arg.StartsWith("maxStep=")) resetStep = int.Parse(arg[8..]);
            if (arg.StartsWith("maxAttempt=")) maxAttempt = int.Parse(arg[11..]);
            if (arg.StartsWith("initialSeed=")) initialSeed = int.Parse(arg[12..]);
            if (arg.StartsWith("testFlags=")) allSettings = arg[10..].Split(',');
        }
        
        StartNewSet();
        Setup();
    }

    private bool StartNewSet() {
        if (allSettings.Length <= settingsIndex) return false;

        currentPlayOut = new PlayOutResult(allSettings[settingsIndex]);
        attempt = 0;
        settingsIndex++;
        return true;
    }

    private void Setup() {
        gameController = new GameController(bagSeed: initialSeed + attempt);
        input = new MinoRouteInput();
        botPlayer = new StandardBotPlayer(gameController, input, currentPlayOut.settings);
        attempt++;
    }

    protected override int OnUpdate() {
        botPlayer.Update();
        gameController.ProcessInput(input);
        gameController.Update();
        
        if (gameController.IsDead) Setup();
        if (resetStep >= 0 && gameController.Statistics.steps >= resetStep) {
            currentPlayOut.statistics.Add(gameController.Statistics);
            Console.WriteLine($"Thread #{Program.GetOptions().Pid:00}: finished attempt {attempt:00}/{maxAttempt} of settings {settingsIndex}/{allSettings.Length} ({botPlayer.SearchSpeed})");
            if (attempt >= maxAttempt) {
                LogStatistics();
                if (!StartNewSet()) return -1;
            }
            
            Setup();
        }

        return 0;
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        gameController.Draw(spriteBatch);
        botPlayer.Draw(spriteBatch);
        Primitives.DrawText(spriteBatch, $"Attempt {attempt} / {maxAttempt}", 720, 32, 24, 0xffffffff);
    }

    private void LogStatistics() {
        if (!Directory.Exists(LogFolderName)) Directory.CreateDirectory(LogFolderName);

        string fileName = $"{resetStep}st-{initialSeed}sd-{maxAttempt}at-{Program.GetOptions().Pid}th-{DateTime.Now:yyMMddHHmmss}.json";
        string json = JsonConvert.SerializeObject(currentPlayOut, Formatting.Indented);
        
        using FileStream stream = File.OpenWrite($"{LogFolderName}/{fileName}");
        using StreamWriter writer = new StreamWriter(stream);
        writer.Write(json);
        string filePath = (Directory.GetCurrentDirectory() + $"\\{LogFolderName}\\" + fileName).Replace('\\', '/');
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Saved log of {maxAttempt} attempts to file:///{filePath}");
    }
}

[Serializable]
public class PlayOutSerializedResult {
    public string serializedSettings;
    public List<GameStatistics> statistics;

    public PlayOutSerializedResult(string serializedSettings) {
        this.serializedSettings = serializedSettings;
        statistics = new List<GameStatistics>();
    }
}

[Serializable]
public class PlayOutResult {
    public BotSettings settings;
    public string serializedSettings;
    public List<GameStatistics> statistics;

    public PlayOutResult() { }

    public PlayOutResult(string serializedSettings) {
        this.serializedSettings = serializedSettings;
        settings = BotSettings.Deserialize(serializedSettings);
        statistics = new List<GameStatistics>();
    }
}