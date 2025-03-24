using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameClient.Tetris;
using GameClient.Tetris.Optimization;
using GeneticSharp;

namespace GameClient.EntryPoint;

public class GeneticsOptimizer {
    private const int MaxStep = 5000, MaxAttempt = 40, InitialSeed = 0, Population = 50, Generations = 100;
    private const int MinThreads = 8, MaxThreads = 16;

    private readonly GeneticAlgorithm ga;

    private static readonly int[] InitialGenes = {
        41, 160, 96, 30, -236, -502, -20, -30, 150, 600, 99999999, 7, -9, 32, 40, 20, 5, 2, 0, 392, 193, 214, -270, -52, -397, -785, 0, 0, -1500, 134
    };

    private static readonly int[] RandomGenes = Enumerable.Range(0, InitialGenes.Length).Select(_ => RandomizationProvider.Current.GetInt(-1000, 1000)).ToArray();
    
    public GeneticsOptimizer() {
        bool useDefaultValues = true;
        foreach (string arg in Program.GetOptions().Args) {
            if (arg.StartsWith("useDefault=")) useDefaultValues = bool.Parse(arg[11..]);
        }
        
        EvaluatorFitness fitness = new EvaluatorFitness(MaxStep, MaxAttempt, InitialSeed);
        EvaluatorChromosome adamChromosome = new EvaluatorChromosome(useDefaultValues ? InitialGenes : RandomGenes);
        Population population = new Population(Population, Population, adamChromosome);
        ParallelTaskExecutor taskExecutor = new ParallelTaskExecutor {
            MinThreads = MinThreads,
            MaxThreads = MaxThreads
        };

        ga = new GeneticAlgorithm(
            population,
            fitness,
            new TournamentSelection(),
            new OrderedCrossover(),
            new PartialShuffleMutation());
        
        ga.TaskExecutor = taskExecutor;
        ga.Termination = new GenerationNumberTermination(Generations);
        ga.GenerationRan += OnGenerationComplete;
        ga.TerminationReached += OnGenerationComplete;
        ga.Start();
    }

    private void OnGenerationComplete(object sender, EventArgs e) {
        EvaluatorChromosome bestChromosome = (EvaluatorChromosome)ga.BestChromosome;
        string chromosomeValues = $"[{string.Join(',', bestChromosome.GetGenes().Select(g => (int)g.Value))}]";
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Generation #{ga.GenerationsNumber} complete, current best is {chromosomeValues}");
    }
}