using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameClient.Tetris;

namespace GameClient.EntryPoint;

public class BatchTester {
    private const int MaxStep = 5000, MaxAttempt = 100, InitialSeed = 100;
    private const int TestsPerThread = 1;
    
    private static readonly List<BotSettings> Settings = BotSettings.GenerateAllSettings(new BotSettingsVarianceOptions()
        .AddObjectOptions((nameof(BotSettings.PreTsdWeight), Enumerable.Range(0, 64).Select(i => i * 10).Cast<object>().ToArray()))
    );

    // private static readonly List<BotSettings> Settings = new() {
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 50 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 100 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 500 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 1000 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 1000 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 1000 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.MCTS, MCTSIterations = 5000 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 1 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 2 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 3 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 4 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 5 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 6 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 7 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 8 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 9 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 10 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 11 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 12 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 12 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 12 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 13 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 14 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 15 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 16 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 17 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 18 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 19 },
        // new BotSettings { SearchType = BotSettings.SearchAlgorithm.Beam, BeamDepth = 5, BeamWidth = 20 },
    // };

    // private static readonly List<BotSettings> Settings = new() {
    //     new BotSettings(),
    //     new BotSettings { PreTsdWeight = 0 },
    //     new BotSettings { NoTsd = true, PreTsdWeight = 0},
    //     new BotSettings { PreTstWeight = 0 },
    //     new BotSettings { NoTst = true, PreTstWeight = 0},
    //     new BotSettings { PreTstWeight = 0 },
    //     new BotSettings { NoPatternCheck = true },
    //     new BotSettings { NoHeight = true },
    //     new BotSettings { NoHeightLevel = true },
    //     new BotSettings { NoHeight = true, NoHeightLevel = true },
    //     new BotSettings { NoCeiling = true },
    //     new BotSettings { NoCeilDepth = true },
    //     new BotSettings { NoCeiling = true, NoCeilDepth = true },
    //     new BotSettings { NoSteepness = true },
    //     new BotSettings { NoSSteepness = true },
    //     new BotSettings { NoSteepness = true, NoSSteepness = true },
    //     new BotSettings { NoFlatness = true },
    //     new BotSettings { NoWellDistance = true },
    // };

    // private static readonly List<BotSettings> Settings = new() {
    //     new BotSettings(),
    //     new BotSettings { NoPatternCheck = true },
    //     new BotSettings { NoHeight = true, NoHeightLevel = true },
    //     new BotSettings { NoHeight = true, NoHeightLevel = true, NoPatternCheck = true },
    // };
    
    
    public BatchTester() {
        int split = Settings.Count / TestsPerThread + (Settings.Count % TestsPerThread == 0 ? 0 : 1);
        Console.WriteLine($"Starting {split} threads for batch testing");

        for (int i = 0; i < split; i++) {
            string testFlags = string.Join(',', Settings.Skip(i * TestsPerThread).Take(TestsPerThread).Select(s => s.Serialized()));
            CreateBotThread(testFlags, i);
        }
    }

    private void CreateBot(string testFlags, int threadId) {
        string argString = $"--mainClass GameNoWindow --subClass BotClient --args maxStep={MaxStep} maxAttempt={MaxAttempt} initialSeed={InitialSeed} testFlags={testFlags}";
        Program.Main(new ThreadArgs(argString, threadId).ToArgs());
    }

    private void CreateBotThread(string testFlags, int threadId) {
        string argString = $"--mainClass GameNoWindow --subClass BotClient --args maxStep={MaxStep} maxAttempt={MaxAttempt} initialSeed={InitialSeed} testFlags={testFlags}";
        Thread thread = new Thread(args => Program.Main(((ThreadArgs)args!).ToArgs()));
        Console.WriteLine(threadId);
        thread.Start(new ThreadArgs(argString, threadId));
    }

    private readonly struct ThreadArgs {
        private readonly string args;
        private readonly int pid;

        public ThreadArgs(string args, int pid) {
            this.args = args;
            this.pid = pid;
        }

        public string[] ToArgs() {
            return args.Split(' ').Append($"--pid={pid}").ToArray();
        }
    }
}