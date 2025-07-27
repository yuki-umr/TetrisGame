using System.Collections.Generic;

namespace GameClient.Tetris; 

public interface ISearcher {
    public string SearcherInfo { get; }

    public StateNode Search(GameState gameState, StateNode lastSelectedNode, IEvaluator evaluator, out SearchProcess searchProcess);

    public SearchStats GetLastSearchStats();
}