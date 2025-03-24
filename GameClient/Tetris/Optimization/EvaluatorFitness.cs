using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Tetris.Input;
using GeneticSharp;

namespace GameClient.Tetris.Optimization; 

public class EvaluatorFitness : IFitness {
    private static int _chromosomeCount;
    
    private readonly int steps, attempts, initialSeed;
    
    public EvaluatorFitness(int steps, int attempts, int initialSeed) {
        (this.steps, this.attempts, this.initialSeed) = (steps, attempts, initialSeed);
    }

    public double Evaluate(IChromosome chromosome) {
        Gene[] genes = ((EvaluatorChromosome)chromosome).GetGenes();
        List<GameStatistics> stats = GetStatsOfGenes(genes);
        
        return stats.Average(s => s.rawAttackTotal);
    }

    private List<GameStatistics> GetStatsOfGenes(Gene[] genes) {
        int chromosomeId = _chromosomeCount++;
        List<GameStatistics> statistics = new List<GameStatistics>();
        for (int attempt = 0; attempt < attempts; attempt++) {
            GameController game = new GameController(bagSeed: initialSeed + attempt);
            MinoRouteInput input = new MinoRouteInput();
            BotSettings settings = new BotSettings { Genes = genes };
            StandardBotPlayer bot = new StandardBotPlayer(game, input, settings);
            while (!game.IsDead && game.Statistics.steps < steps) {
                bot.Update();
                game.ProcessInput(input);
                game.Update();
            }
            
            statistics.Add(game.Statistics);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Finished attempt {attempt}/{attempts} for chromosome#{chromosomeId} ({bot.SearchSpeed}) (dead={game.IsDead})");
        }
        
        return statistics;
    }
}