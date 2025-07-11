using System.Collections.Generic;

namespace GameClient.Tetris; 

public interface ISearcher {
    public string SearcherInfo { get; }

    // DOING: Search() should return the "leaf node" of the search tree, which is the best move to make(?????)
    public StateNode Search(GameState gameState, StateNode lastSelectedNode, Evaluator evaluator, out SearchProcess searchProcess);

    public SearchStats GetLastSearchStats();
}