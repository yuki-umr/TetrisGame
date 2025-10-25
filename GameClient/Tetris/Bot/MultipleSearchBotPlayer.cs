using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.EntryPoint;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public class MultipleSearchBotPlayer : BotPlayer {
    public readonly List<VariedState> VariedStates = new();

    private readonly GameController game;
    private readonly MinoRouteInput inputSystem;
    private readonly List<SearcherSet> searchers;

    private readonly SearchCompareMethod compareMethod;

    private enum SearchCompareMethod {
        SelectedDifferentMove,
        HasBetterEvaluation,
    }

    private StateNode nextNode;

    public MultipleSearchBotPlayer(GameController game, MinoRouteInput inputSystem, IEnumerable<BotSettings> settings) {
        this.game = game;
        this.inputSystem = inputSystem;

        searchers = new List<SearcherSet>();
        foreach (BotSettings setting in settings) {
            IEvaluator evaluator = setting.GetEvaluator();
            ISearcher searcher = setting.GetSearcher();
            searchers.Add(new SearcherSet { searcher = searcher, evaluator = evaluator });
        }

        game.ChangeInputMode();
        UpdateMino();
    }

    public override void Update() {
        if (inputSystem.InputsLeft <= 0) {
            UpdateMino();
        }
        
        inputSystem.Update();
    }

    private struct NodeSearchResult {
        public StateNode destinationNode, nextNode;

        public NodeSearchResult(StateNode destinationNode, StateNode nextNode) {
            this.destinationNode = destinationNode;
            this.nextNode = nextNode;
        }
    }

    private void UpdateMino() {
        List<NodeSearchResult> searchResults = new();
        int minSearchDepth = int.MaxValue;
        Dictionary<GameState, StateNode> bestStates = new();
        bool saveThisState = false;

        int searchSeed = RandomGen.ResetSeed();
        foreach ((ISearcher searcher, IEvaluator evaluator) in searchers) {
            // some searchers like MCTS uses random while searching, we need to set a seed to get consistent results
            RandomGen.SetSeed(searchSeed);
            StateNode destinationNode = searcher.Search(game.State, null, evaluator, out _);
            if (destinationNode == null) continue;
            
            StateNode searcherNextNode = destinationNode.GetSubRootNode();
            NodeSearchResult searchState = new(destinationNode, searcherNextNode);

            // some search algorithms like MCTS can have destinations with different depths
            int destinationDepth = destinationNode.NodeDepth - searcherNextNode.NodeDepth;
            minSearchDepth = Math.Min(minSearchDepth, destinationDepth);
            
            searchResults.Add(searchState);
        }

        // Compare the first move and save if any searcher made a different move
        if (compareMethod == SearchCompareMethod.SelectedDifferentMove) {
            saveThisState = bestStates.Count > 1;
        } else if (compareMethod == SearchCompareMethod.HasBetterEvaluation) {
            Evaluation baseEvaluation = default;
            for (int i = 0; i < searchResults.Count; i++) {
                NodeSearchResult result = searchResults[i];
                int desiredDepth = result.nextNode.NodeDepth + minSearchDepth;
                StateNode nodeAtMinDepth = result.destinationNode;
                while (nodeAtMinDepth.NodeDepth > desiredDepth) {
                    nodeAtMinDepth = nodeAtMinDepth.Parent;
                }

                Evaluation totalEvaluation = default;
                bool totalEvaluationSet = false;
                while (!nodeAtMinDepth.IsRoot) {
                    if (!totalEvaluationSet) {
                        totalEvaluationSet = true;
                        totalEvaluation = nodeAtMinDepth.Evaluation;
                    } else {
                        totalEvaluation = new Evaluation(totalEvaluation.field, totalEvaluation.movement + nodeAtMinDepth.Evaluation.movement);
                    }
                    nodeAtMinDepth = nodeAtMinDepth.Parent;
                }
            }
        }
        
        
        if (searchResults.Count > 0 && searchResults[0].nextNode != null) {
            nextNode = searchResults[0].nextNode;
            inputSystem.SetCurrentRoute(nextNode.GetRoute());

            if (saveThisState) {
                // serialize current game state
                string stateHash = nextNode.Parent.GameState.SerializeToString();
                VariedState variedState = new() {
                    searchSeed = searchSeed,
                    stateHash = stateHash
                };
                
                VariedStates.Add(variedState);
            }
        } else {
            Console.WriteLine("no route found, killing game");
            game.Kill();
        }
    }

    public override void Draw(SpriteBatch spriteBatch) { }

    public override string SearchSpeed => searchers[0].searcher.GetLastSearchStats().searchSpeed;

    private class SearcherSet {
        public ISearcher searcher;
        public IEvaluator evaluator;
        
        public void Deconstruct(out ISearcher searcher, out IEvaluator evaluator) {
            searcher = this.searcher;
            evaluator = this.evaluator;
        }
    }
}