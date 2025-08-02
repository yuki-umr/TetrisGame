using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.EntryPoint;

public class NodeViewer : WindowManager {
    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);
    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => 4;

    private const int MoveStep = 8, Boost = 8, BlockSize = 8, FieldSpaceX = 16, FieldSpaceY = 160;

    private const int ItemWidth = BlockSize * Constants.GameFieldWidth + FieldSpaceX,
        ItemHeight = BlockSize * Constants.GameFieldHeight + FieldSpaceY;

    private List<BeamNode>[] nodesTree;
    private List<ColoredGameField>[] fieldsTree;
    private KeyboardInput inputSystem;
    private Vector2Int drawOffset;
    private IEvaluator evaluator;

    private const int BeamWidth = 12;

    private GameState CreateTestGameState() {
        uint[] field = {
            0b0000111u, 
            0b0000111u, 
            0b0000001u, 
            0b0000000u, 
            0b0000001u, 
            0b0000111u, 
            0b0000111u, 
            0b0000011u, 
            0b0000011u, 
            0b0000011u
        };
        Array.Reverse(field);
        int defaultMino = 3;
        List<ulong> nextMinos = new List<ulong> { 0, 2, 5, 0, 4, 6 };
        
        BitMatrix initialField = new BitMatrix(field, Constants.DefaultGameFieldSize);
        MinoBag bag = new MinoBag(nextMinos);
        GameState newState = new GameState(new GameField(initialField), defaultMino: defaultMino, minoBag: bag);
        return newState;
    }
    
    protected override void OnInitialize() {
        GameState gameState = CreateTestGameState();
        // evaluator = DefaultEvaluator.GetDefault(new BotSettings());
        evaluator = ThieryEvaluator.GetDefault(new BotSettings());
        ISearcher searcher = new BeamSearcher(5, BeamWidth);
        
        searcher.Search(gameState, null, evaluator, out SearchProcess process);
        BeamSearchProcess beamProcess = (BeamSearchProcess)process;
        
        nodesTree = beamProcess.NodesTree;
        fieldsTree = nodesTree.Select(list => list.Select(node => node.Parent.GameState.Field.ConvertToColored()).ToList()).ToArray();
        Console.WriteLine(fieldsTree[1].Count);
        inputSystem = new KeyboardInput();
    }

    protected override int OnUpdate() {
        inputSystem.Update();
        InputState inputState = inputSystem.PopState();
        int moveStep = MoveStep * (inputState.IsDown(InputKey.Hold) ? Boost : 1);
        if (inputState.IsDown(InputKey.Left)) drawOffset.x += moveStep;
        if (inputState.IsDown(InputKey.Right)) drawOffset.x -= moveStep;
        if (inputState.IsDown(InputKey.HardDrop)) drawOffset.y += moveStep;
        if (inputState.IsDown(InputKey.SoftDrop)) drawOffset.y -= moveStep;

        return 0;
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        for (var y = 0; y < nodesTree.Length; y++) {
            for (var x = 0; x < nodesTree[y].Count; x++) {
                StateNode node = nodesTree[y][x];
                ColoredGameField field = fieldsTree[y][x];
                int dx = ItemWidth * x + drawOffset.x;
                int dy = ItemHeight * y + drawOffset.y;
                field.Draw(spriteBatch, dx, dy, BlockSize);
                
                Mino placedMino = new Mino(node.MinoType, node.MinoState.rotation);
                int yMinoDraw = dy + (Constants.GameFieldHeight - node.MinoState.y - 1) * BlockSize;
                placedMino.Draw(spriteBatch, dx + node.MinoState.x * BlockSize, yMinoDraw, BlockSize);
                
                foreach (PatternMatchData matchData in node.Evaluation.patternsFound) {
                    Mino patternMino = new Mino(matchData.pattern.minoType, matchData.state.rotation);
                    int yDraw = dy + (Constants.GameFieldHeight - matchData.state.y - 1) * BlockSize;
                    patternMino.Draw(spriteBatch, dx + matchData.state.x * BlockSize, yDraw, BlockSize, overrideColor: 0xff0000ffu, outline: 2);
                }
                
                // Draw a line pointing to their origin node
                if (node.NodeRank < BeamWidth)
                    Primitives.DrawLine(spriteBatch, dx + ItemWidth / 2, dy, 
                        ItemWidth * node.Parent.NodeRank + ItemWidth / 2 + drawOffset.x, dy - FieldSpaceY, 1, 0xff000080u);

                // bool tsd = !node.IsRoot && node.Parent.Evaluation.patternsFound != null && node.Parent.Evaluation.patternsFound.Any(p => p.state == node.MinoState);
                
                // string breakdown = $"{evaluator.KeyFieldHeight(node.GameState.Field)} {evaluator.KeyFieldCeiling(node.GameState.Field)}\n" +
                //                    $"{evaluator.KeyFieldSteepness(node.GameState.Field)} {evaluator.KeyFieldWalledWell(node.GameState.Field)} " +
                //                    $"{evaluator.KeyPatternMatches(node.Evaluation.patternsFound, node.GameState.Field)}";
                string eval = $"fld {node.Evaluation.field} {node.UseHold}\nmov {node.Evaluation.movement}\nrnk{node.Parent.NodeRank}";
                Primitives.DrawText(spriteBatch, eval, dx, dy + BlockSize * (Constants.GameFieldHeight + 2), 12, Color.White);
            }
        }

        base.OnDraw(graphicsDevice, spriteBatch);
    }
}