namespace GameClient.Tetris;

public class MonteCarloSearcher : ISearcher {
    public string SearcherInfo { get; }
    
    private readonly int fixedIterations;

    public MonteCarloSearcher(int iterations) {
        fixedIterations = iterations;
        SearcherInfo = $"MCTS ({iterations} iterations)";
    }
    
    public StateNode Search(GameState gameState, StateNode lastSelectedNode, Evaluator evaluator, out SearchProcess searchProcess) {
        MonteCarloNode rootNode;
        if (lastSelectedNode is MonteCarloNode lastMonteCarloNode && lastSelectedNode.GameState == gameState) {
            lastMonteCarloNode.ConvertToRootNode();
            rootNode = lastMonteCarloNode;
        } else {
            rootNode = MonteCarloNode.CreateRootNode(gameState, evaluator);
        }

        searchProcess = new MonteCarloSearchProcess();
        for (int i = 0; i < fixedIterations; i++) {
            SingleIteration(rootNode, evaluator);
        }

        return rootNode.GetBestChild();
    }

    public SearchStats GetLastSearchStats() {
        throw new System.NotImplementedException();
    }

    private void SingleIteration(MonteCarloNode rootNode, Evaluator evaluator) {
        // 1. Traverse the tree to select a node to expand
        MonteCarloNode currentNode = rootNode;
        while (currentNode.Expanded) {
            currentNode = currentNode.VisitWeightedRandomChild();
        }
        
        // DOING: remove nodes with no route
        // 2. Generate child nodes with all possible moves, and evaluate them 
        currentNode.ExpandNode(evaluator);
        
        // 3. Apply backpropagation and recursively update the parent nodes
        Backpropagate(currentNode);
    }

    private void Backpropagate(MonteCarloNode node) {
        while (true) {
            // rebuild the selection weights used in the next iteration
            bool evaluationUpdated = node.UpdateNodeEvaluationFromChild();

            if (!evaluationUpdated || node.Parent is null || node.Parent.IsRoot) return;
            node.UpdateSelectionWeightInParent();
            node = (MonteCarloNode)node.Parent;
        }
    }
}