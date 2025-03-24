using System;
using System.Linq;
using System.Threading;

namespace GameClient.EntryPoint;

public class SearchComparer {
    private const int MaxStep = 5000, MaxAttempt = 40, InitialSeed = 0;
    private const int Threads = 4;
    
    public SearchComparer() {
        Console.WriteLine($"Starting {Threads} threads for search comparison");

        for (int i = 0; i < Threads; i++) {
            CreateBotThread(i);
        }
    }

    private void CreateBotThread(int threadId) {
        int seed = InitialSeed + MaxAttempt * threadId;
        string argString = $"--mainClass GameNoWindow --subClass SearchCompareClient --args maxStep={MaxStep} maxAttempt={MaxAttempt} initialSeed={seed}";
        Thread thread = new Thread(args => Program.Main(((ThreadArgs)args!).ToArgs()));
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