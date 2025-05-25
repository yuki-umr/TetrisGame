using System;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public class StandardBotPlayer : BotPlayer {
    private const bool DrawProcess = true;

    private readonly GameController game;
    private readonly MinoRouteInput inputSystem;
    private readonly Evaluator evaluator;
    private readonly ISearcher searcher;

    private StateNode destinationNode, nextNode;

    public StandardBotPlayer(GameController game, MinoRouteInput inputSystem, BotSettings settings = null) {
        this.game = game;
        this.inputSystem = inputSystem;
        
        settings ??= new BotSettings();
        evaluator = Evaluator.GetDefault(settings);

        destinationNode = null;
        searcher = new BeamSearcher(settings.BeamDepth, settings.BeamWidth);
        
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
        destinationNode = searcher.Search(game.State, nextNode, evaluator, out _);
        if (destinationNode != null) {
            nextNode = destinationNode.GetSubRootNode();
            inputSystem.Route = nextNode.GetRoute();
            
            nextNode.GameState.SerializeToString();
        } else {
            // Console.WriteLine("no route found, killing game");
            game.Kill();
        }
    }

    public override void Draw(SpriteBatch spriteBatch) {
        string nodesInfo = searcher.SearcherInfo;
        SearchStats lastStats = searcher.GetLastSearchStats();
        nodesInfo += $"\n{lastStats.lastSearchStats}\n{lastStats.totalSearchStats}";
        nodesInfo += $"\n\navg:{lastStats.searchSpeed}";
        Primitives.DrawText(spriteBatch, nodesInfo, 720, 64, 24, 0xffffffff);
        Primitives.DrawText(spriteBatch, game.Statistics.ToString(), 720, 240, 16, 0xffffffff);
        Primitives.DrawText(spriteBatch, evaluator.ControlFlags, 720, 600, 10, 0xffffffff);
        
        if (destinationNode != null && DrawProcess) {
            foreach (StateNode node in destinationNode.GetNodesFromRoot()) {
                Mino mino = new Mino(node.MinoType, node.MinoState.rotation);
                Vector2Int drawPos = game.FieldPosToGlobal(node.MinoState.x, node.MinoState.y);
                Color minoColor = Color.Lerp(new Color(Constants.MinoColorCodes[node.MinoType]), Color.White, 0.3f);
                mino.Draw(spriteBatch, drawPos.x, drawPos.y, Constants.DrawBlockSize, 2, minoColor.PackedValue);
                if (node.Evaluation.result.lineClear > 0) break;
            }
            
            // draw patterns
            foreach (PatternMatchData matchData in nextNode.Evaluation.patternsFound) {
                Vector2Int drawPos = game.FieldPosToGlobal(matchData.state.x, matchData.state.y + matchData.pattern.pattern.Size.y - 1);
                Primitives.DrawRectangle(spriteBatch, drawPos.x, drawPos.y, matchData.pattern.pattern.Size.x * Constants.DrawBlockSize, 
                    matchData.pattern.pattern.Size.y * Constants.DrawBlockSize, 0xff0000ff, 2);
            }
        }
    }

    public override string SearchSpeed => searcher.GetLastSearchStats().searchSpeed;
}