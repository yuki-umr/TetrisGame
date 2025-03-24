using System;
using System.Linq;
using GeneticSharp;

namespace GameClient.Tetris.Optimization; 

public class EvaluatorChromosome : ChromosomeBase {
    public EvaluatorChromosome(int[] input) : base(input.Length) {
        Gene[] genes = input.Select(x => new Gene(x)).ToArray();
        ReplaceGenes(0, genes);
    }

    private EvaluatorChromosome(Gene[] genes) : base(genes.Length) {
        ReplaceGenes(0, genes);
    }

    public override Gene GenerateGene(int geneIndex) {
        return new Gene(RandomizationProvider.Current.GetInt(-500, 500));
    }

    public override IChromosome CreateNew() {
        return new EvaluatorChromosome(GetGenes());
    }
}