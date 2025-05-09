using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using GameClient.EntryPoint;

namespace GameClient;

// Command line arguments:
// mainClass: specifies the entry point class, which will get instantiated right after launch
// subClass: specifies the class which the mainClass should instantiate after launch
// pid: specifies the unique PID of the specific process. This is mostly useful when running multiple simulations in parallel
// args: setting multiple keywords is supported, use ' ' to split keywords
//       the format for setting a specific value to a target is "target=value"
public static class Program {
    private static readonly Dictionary<string, Type> EntryClass = new() {
        { "GameWindow", typeof(GameWindow) },
        { "GameNoWindow", typeof(GameNoWindow) },
        { "TetrisClient", typeof(TetrisClient) },
        { "BotClient", typeof(BotClient) },
        { "SearchCompareClient", typeof(SearchCompareClient) },
        { "BatchTester", typeof(BatchTester) },
        { "SearchComparer", typeof(SearchComparer) },
        { "MultipleSearchViewer", typeof(MultipleSearchViewer) },
        { "NodeViewer", typeof(NodeViewer) },
        { "QuickTest", typeof(QuickTest) },
        { "LogGraphViewer", typeof(LogGraphViewer) },
        { "GeneticsOptimizer", typeof(GeneticsOptimizer) },
        { "ReplayViewer", typeof(ReplayViewer) },
    };

    private static int ThreadId => Environment.CurrentManagedThreadId; 
    private static readonly ConcurrentDictionary<int, OptionArgs> ThreadOptions = new();
    
    public static void Main(string[] args) {
        ParserResult<OptionArgs> parseResult = Parser.Default.ParseArguments<OptionArgs>(args);
        if (parseResult.Errors.Any()) return;
        ThreadOptions[ThreadId] = parseResult.Value;
        Console.WriteLine($"Starting thread id {ThreadId}");

        if (parseResult.Value.Args.Contains("stop")) return;
        object entry = parseResult.Value.GetMainClassInstance();
        if (entry is Microsoft.Xna.Framework.Game game) game.Run();
    }

    public static OptionArgs GetOptions() => ThreadOptions[ThreadId];

    public class OptionArgs {
        [Option("mainClass", Required = true)]
        public string MainClass { get; set; }
        
        [Option("subClass", Required = false)]
        public string SubClass { get; set; }

        [Option("pid", Required = false, Hidden = true, Default = -1)]
        public int Pid { get; set; }

        [Option("args", Required = false)]
        public IEnumerable<string> Args { get; set; }

        public object GetMainClassInstance() => Activator.CreateInstance(EntryClass[MainClass]);

        public object GetSubClassInstance() => Activator.CreateInstance(EntryClass[SubClass]);
    }
}