using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace GameClient.EntryPoint;

public class SearchCompareClient : WindowManager {
    private const string LogFolderName = "log-var";
    
    // Test all patterns of settings on default, change this to run only for specific configurations
    private static readonly BotSettings[] TestSettings = {
        new() { SearchType = BotSettings.SearchAlgorithm.Beam, BeamWidth = 12, BeamDepth = 5 },
        new() { SearchType = BotSettings.SearchAlgorithm.Beam, BeamWidth = 12, BeamDepth = 5, Evaluator = BotSettings.EvaluatorType.Thiery },
        new() { SearchType = BotSettings.SearchAlgorithm.Beam, BeamWidth = 1, BeamDepth = 1, Evaluator = BotSettings.EvaluatorType.Thiery },
    };

    private GameController gameController;
    private MultipleSearchBotPlayer botPlayer;
    private MinoRouteInput input;

    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);

    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => 0;

    private int resetStep = -1, maxAttempt, initialSeed, attempt;
    private readonly MultipleSearchResult searchResult = new();

    protected override void OnInitialize() {
        foreach (string arg in Program.GetOptions().Args) {
            if (arg.StartsWith("maxStep=")) resetStep = int.Parse(arg[8..]);
            if (arg.StartsWith("maxAttempt=")) maxAttempt = int.Parse(arg[11..]);
            if (arg.StartsWith("initialSeed=")) initialSeed = int.Parse(arg[12..]);
        }

        searchResult.botSettings.AddRange(TestSettings.Select(settings => settings.Serialized()));
        Setup();
    }

    private void Setup() {
        gameController = new GameController(bagSeed: initialSeed + attempt);
        input = new MinoRouteInput();
        botPlayer = new MultipleSearchBotPlayer(gameController, input, TestSettings);
        attempt++;
    }

    protected override int OnUpdate() {
        botPlayer.Update();
        gameController.ProcessInput(input);
        gameController.Update();
        
        if (gameController.IsDead) Setup();
        if (resetStep >= 0 && gameController.Statistics.steps >= resetStep) {
            searchResult.variedStates.AddRange(botPlayer.VariedStates);
            Console.WriteLine($"Thread #{Program.GetOptions().Pid:00}: finished attempt {attempt:00}/{maxAttempt} ({botPlayer.SearchSpeed})");
            if (attempt >= maxAttempt) {
                LogSearchResult();
                return -1;
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

    private void LogSearchResult() {
        if (!Directory.Exists(LogFolderName)) Directory.CreateDirectory(LogFolderName);
        string fileName = $"varied-{Program.GetOptions().Pid}th-{DateTime.Now:yyMMddHHmmss}.json";
        string json = JsonConvert.SerializeObject(searchResult, Formatting.Indented);
        
        using FileStream stream = File.OpenWrite($"{LogFolderName}/{fileName}");
        using StreamWriter writer = new StreamWriter(stream);
        writer.Write(json);
        string filePath = (Directory.GetCurrentDirectory() + $"\\{LogFolderName}\\" + fileName).Replace('\\', '/');
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Saved log of {maxAttempt} attempts to file:///{filePath}");
    }
}

[Serializable]
public class MultipleSearchResult {
    public readonly List<string> botSettings = new();
    public readonly List<VariedState> variedStates = new();
}

[Serializable]
public class VariedState {
    public int searchSeed;
    public string stateHash;
}
