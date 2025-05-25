using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public class MultipleSearchBotPlayer : BotPlayer {
    public readonly List<string> VariedStates = new();

    private readonly GameController game;
    private readonly MinoRouteInput inputSystem;
    private readonly List<(BeamSearcher, Evaluator)> searchers;
    private readonly int minimumSearchDepth;

    private StateNode nextNode;

    public MultipleSearchBotPlayer(GameController game, MinoRouteInput inputSystem, IEnumerable<BotSettings> settings) {
        this.game = game;
        this.inputSystem = inputSystem;

        searchers = new List<(BeamSearcher, Evaluator)>();
        foreach (BotSettings setting in settings) {
            Evaluator evaluator = Evaluator.GetDefault(setting);
            BeamSearcher searcher = new BeamSearcher(setting.BeamDepth, setting.BeamWidth);
            searchers.Add((searcher, evaluator));
        }

        minimumSearchDepth = searchers.Min(s => s.Item1.nextSeek);
        game.ChangeInputMode();
        UpdateMino();
    }

    public override void Update() {
        if (inputSystem.Route.Length <= 0) {
            UpdateMino();
        }
        
        inputSystem.Update();
    }

    private void UpdateMino() {
        StateNode bestNode = null;
        HashSet<GameState> bestStates = new HashSet<GameState>();
        List<Pattern>[] foundPatterns = new List<Pattern>[minimumSearchDepth];
        bool saveThisState = false;
        foreach (var (searcher, evaluator) in searchers) {
            StateNode node = searcher.Search(game.State, null, evaluator, out _);
            if (node == null) continue;
            List<StateNode> nodes = node.GetNodesFromRoot();
            for (var i = 0; i < minimumSearchDepth; i++) {
                List<Pattern> newPatterns = nodes[i].Evaluation.patternsFound
                    .Select(p => p.pattern)
                    .Where(p => p.checkMinoPlacement)
                    .OrderBy(p => p.patternIndex).ToList();
                if (foundPatterns[i] == null) {
                    foundPatterns[i] = newPatterns;
                } else {
                    // if there are any new patterns not found in the previous search, flag this state
                    if (foundPatterns[i].Count != newPatterns.Count) continue;
                    bool sequenceEqual = true;
                    for (int j = 0; j < newPatterns.Count; j++) {
                        if (foundPatterns[i][j].patternIndex != newPatterns[j].patternIndex) sequenceEqual = false;
                    }

                    if (sequenceEqual) continue;
                    // Console.WriteLine($"[{string.Join(", ", foundPatterns[i].Select(p => p.patternIndex))}] != [{string.Join(", ", newPatterns.Select(p => p.patternIndex))}]");
                    saveThisState = true;
                }
            }
            
            bestNode ??= node = node.GetSubRootNode();
            bestStates.Add(node.GameState);
        }

        // Compare the first move and save if any searcher made a different move
        // saveThisState = bestStates.Count > 1;
        
        if (bestNode != null) {
            nextNode = bestNode;
            inputSystem.Route = nextNode.GetRoute();

            if (saveThisState) {
                // serialize current game state
                string stateHash = nextNode.Parent.GameState.SerializeToString();
                VariedStates.Add(stateHash);
            }
        } else {
            Console.WriteLine("no route found, killing game");
            game.Kill();
        }
    }

    public override void Draw(SpriteBatch spriteBatch) { }

    public override string SearchSpeed => searchers[0].Item1.GetLastSearchStats().searchSpeed;
}