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

    private StateNode nextNode;

    public MultipleSearchBotPlayer(GameController game, MinoRouteInput inputSystem, IEnumerable<BotSettings> settings) {
        this.game = game;
        this.inputSystem = inputSystem;

        searchers = new List<SearcherSet>();
        foreach (BotSettings setting in settings) {
            Evaluator evaluator = Evaluator.GetDefault(setting);
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

    private void UpdateMino() {
        StateNode bestNode = null;
        Dictionary<GameState, StateNode> bestStates = new();
        bool saveThisState = false;

        int searchSeed = RandomGen.ResetSeed();
        foreach ((ISearcher searcher, Evaluator evaluator) in searchers) {
            // some searchers like MCTS uses random while searching, we need to set a seed to get consistent results
            RandomGen.SetSeed(searchSeed);
            StateNode destinationNode = searcher.Search(game.State, null, evaluator, out _);
            if (destinationNode == null) continue;
            
            StateNode searcherNextNode = destinationNode.GetSubRootNode();
            bestNode ??= searcherNextNode;
            if (bestStates.TryAdd(searcherNextNode.GameState, searcherNextNode)) {
                if (searcher is MonteCarloSearcher) {
                    Console.WriteLine($"AAA: {destinationNode.GetNodesFromRoot().Count}"); // DOING: usage of GetNodesFromRoot() is wrong, it must be used for the "leaf node" only
                } 
            }
        }

        // Compare the first move and save if any searcher made a different move
        saveThisState = bestStates.Count > 1;
        
        if (bestNode != null) {
            nextNode = bestNode;
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
        public Evaluator evaluator;
        
        public void Deconstruct(out ISearcher searcher, out Evaluator evaluator) {
            searcher = this.searcher;
            evaluator = this.evaluator;
        }
    }
}