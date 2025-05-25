using System.Collections.Generic;

namespace GameClient.Tetris; 

public interface ISearcher {
    public string SearcherInfo { get; }

    public StateNode Search(GameState gameState, StateNode lastSelectedNode, Evaluator evaluator, out SearchProcess searchProcess);

    public SearchStats GetLastSearchStats();
}