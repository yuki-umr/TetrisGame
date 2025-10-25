using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace GameClient.EntryPoint;

public class MultipleSearchViewer : WindowManager {
    private const string FavoriteFileName = "favorites.json";
    private const int BlockSize = 8, SkipRange = 10, NextSpace = 64;
    private static readonly Vector2Int ItemSpace = new(16, 8);
    private static readonly Vector2Int ItemSize = new(BlockSize * Constants.GameFieldWidth + ItemSpace.x, BlockSize * Constants.GameFieldHeight + ItemSpace.y);
    private static readonly Vector2Int ItemOffset = new(432, 16), TextOffset = new(-104, 16), BaseOffset = new(80, 64);
    private static readonly Vector2Int IndexTextOffset = new(64, 432);
    private static readonly Vector2Int HoldOffset = new(16, 80), NextOffset = new(256, 80);
    
    public override Vector2Int InitialWindowSize => new(1680, 720);
    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => 4;

    private string folderName = "250805";

    private int nodeIndex, favoriteIndex, maxDepth;
    private bool favoriteOnlyMode;
    private readonly KeyboardInput inputSystem = new();

    private readonly SortedSet<int> favoriteIndexes = new();
    private List<int> favoriteIndexList = new();
    private readonly List<SearchPair> searchPairs = new();
    private readonly List<OriginalState> originalStates = new();
    private readonly List<ColoredGameField> originalFields = new();
    private readonly List<List<(ColoredGameField, StateNode)>> nodeFields = new();

    private int CurrentIndex {
        get => favoriteOnlyMode ? favoriteIndex : nodeIndex;
        set {
            if (favoriteOnlyMode) favoriteIndex = value;
            else nodeIndex = value;
        }
    }
    private int CurrentNodeIndex => favoriteOnlyMode ? favoriteIndexList[CurrentIndex] : CurrentIndex;
    private int TotalNodes => favoriteOnlyMode ? favoriteIndexList.Count : originalStates.Count;
    
    // file format: {date}/varied-{index}th-{date}.json
    
    protected override void OnInitialize() {
        Primitives.DrawOutsideWindow = true;
        foreach (string arg in Program.GetOptions().Args) {
            if (arg.StartsWith("folder=")) folderName = arg[7..];
        }
        
        string[] searchFiles = Directory.GetFiles($"log-var/{folderName}/", "varied-*.json");
        string[] serializedSettings = null;
        Console.WriteLine(searchFiles.Length);
        foreach (string file in searchFiles) {
            string json = File.ReadAllText(file);
            MultipleSearchResult searchResult = JsonConvert.DeserializeObject<MultipleSearchResult>(json);
            Debug.Assert(searchResult != null, nameof(searchResult) + " is null");
            
            foreach (VariedState variedState in searchResult.variedStates) {
                GameState state = GameState.DeserializeFromString(variedState.stateHash);
                OriginalState originalState = new() {
                    searchSeed = variedState.searchSeed,
                    state = state
                };
                
                originalStates.Add(originalState);
                originalFields.Add(state.Field.ConvertToColored());
            }
            
            serializedSettings ??= searchResult.botSettings.ToArray();
        }
        
        searchPairs.AddRange(serializedSettings!.Select(setting => new SearchPair(setting)));

        maxDepth = searchPairs.Min(pair => pair.settings.BeamDepth); // use the minimum beam depth for now
        UpdateViewNode();
        LoadFavorite();
    }

    protected override int OnUpdate() {
        inputSystem.Update();
        InputState inputState = inputSystem.PopState();
        if (inputState.IsPressed(InputKey.SpinCounterClock)) ToggleFavorite();
        if (inputState.IsPressed(InputKey.Hold)) RegisterFavorite();
        
        int newIndex = CurrentIndex;
        if (inputState.IsPressed(InputKey.Left)) newIndex = Math.Clamp(CurrentIndex - 1, 0, TotalNodes - 1);
        if (inputState.IsPressed(InputKey.Right)) newIndex = Math.Clamp(CurrentIndex + 1, 0, TotalNodes - 1);
        if (inputState.IsPressed(InputKey.HardDrop)) newIndex = Math.Clamp(CurrentIndex + SkipRange, 0, TotalNodes - 1);
        if (inputState.IsPressed(InputKey.SoftDrop)) newIndex = Math.Clamp(CurrentIndex - SkipRange, 0, TotalNodes - 1);
        
        if (newIndex != CurrentIndex) {
            CurrentIndex = newIndex;
            UpdateViewNode();
        }
        
        return 0;
    }

    private void UpdateViewNode() {
        OriginalState selectedState = originalStates[CurrentNodeIndex];
        nodeFields.Clear();
        
        for (var i = 0; i < searchPairs.Count; i++) {
            var searcher = searchPairs[i];
            
            RandomGen.SetSeed(selectedState.searchSeed); // reset the search seed every time
            StateNode bestNode = searcher.Search(selectedState.state);
            List<StateNode> nodes = bestNode.GetNodesFromRoot();
            nodeFields.Add(new List<(ColoredGameField, StateNode)>());
            
            // nodes.Count -> maximum depth
            // maxDepth -> clamped depth
            for (var j = 0; j < maxDepth; j++) {
                if (j >= nodes.Count) {
                    // If the node does not exist, fill with empty field
                    nodeFields[i].Add((new ColoredGameField(), nodes.Last()));
                    continue;
                }
                
                // Save parent colored field for rendering
                ColoredGameField coloredField = nodes[j].Parent.GameState.Field.ConvertToColored();
                nodeFields[i].Add((coloredField, nodes[j]));
            }
        }
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        GameState selectedState = originalStates[CurrentNodeIndex].state;
        originalFields[CurrentNodeIndex].Draw(spriteBatch, BaseOffset.x, BaseOffset.y, BlockSize * 2);
        
        Mino currentMino = new Mino(selectedState.CurrentMino, 0);
        int yCurrentDraw = BaseOffset.y + (Constants.GameFieldHeight - Constants.MinoSpawnPosition.y - 1) * BlockSize * 2;
        currentMino.Draw(spriteBatch, BaseOffset.x + Constants.MinoSpawnPosition.x * BlockSize * 2, yCurrentDraw, BlockSize * 2);

        string currentIndex = $"{CurrentIndex + 1}/{TotalNodes} (#{CurrentNodeIndex + 1})\n{(favoriteOnlyMode ? "Favorite Only" : "All Nodes")}";
        if (favoriteIndexes.Contains(CurrentNodeIndex)) currentIndex += "\n*";
        Primitives.DrawText(spriteBatch, currentIndex, IndexTextOffset.x, IndexTextOffset.y, 16, 0xffffffffu);
        for (int i = 0; i < 5; i++) {
            Mino nextMino = new Mino(selectedState.PeekNextMino(i), 0);
            nextMino.Draw(spriteBatch, NextOffset.x, NextOffset.y + NextSpace * i, BlockSize * 2);
        }

        if (selectedState.HoldingMino != -1) {
            Mino holdMino = new Mino(selectedState.HoldingMino, 0);
            holdMino.Draw(spriteBatch, HoldOffset.x, HoldOffset.y, BlockSize * 2);
        }
        
        for (var y = 0; y < nodeFields.Count; y++) {
            int dy = ItemOffset.y + ItemSize.y * y;
            int totalAttackLines = 0, totalClearLines = 0, totalBadClears = 0, totalGoodAttacks = 0;
            for (var x = 0; x < nodeFields[y].Count; x++) {
                (ColoredGameField field, StateNode node) = nodeFields[y][x];
                bool isBadClear = node.Evaluation.result.lineClear is not (0 or 4) && !node.Evaluation.result.tSpin;
                totalAttackLines += node.Evaluation.result.attackLine; 
                totalClearLines += node.Evaluation.result.lineClear;
                totalBadClears += isBadClear ? 1 : 0;
                totalGoodAttacks += isBadClear ? 0 : node.Evaluation.result.attackLine;
                int dx = ItemOffset.x + ItemSize.x * x;
                field.Draw(spriteBatch, dx, dy, BlockSize);
                
                Mino placedMino = new Mino(node.MinoType, node.MinoState.rotation);
                int yMinoDraw = dy + (Constants.GameFieldHeight - node.MinoState.y - 1) * BlockSize;
                placedMino.Draw(spriteBatch, dx + node.MinoState.x * BlockSize, yMinoDraw, BlockSize);
                
                foreach (PatternMatchData matchData in node.Evaluation.patternsFound) {
                    if (matchData.pattern.checkMinoPlacement) {
                        Mino patternMino = new Mino(matchData.pattern.minoType, matchData.state.rotation);
                        int yDraw = dy + (Constants.GameFieldHeight - matchData.state.y - 1) * BlockSize;
                        patternMino.Draw(spriteBatch, dx + matchData.state.x * BlockSize, yDraw, BlockSize, overrideColor: 0xff0000ffu, outline: 2);
                    } else {
                        // Vector2Int size = matchData.pattern.pattern.Size;
                        // int yDraw = dy + (Constants.GameFieldHeight - matchData.state.y - 1) * BlockSize;
                        // Primitives.DrawRectangle(spriteBatch, dx + matchData.state.x * BlockSize, yDraw, size.x * BlockSize, size.y * BlockSize,
                        //     0xffff0000u, outline: 2);
                    }
                }
                
                Primitives.DrawText(spriteBatch, $"#{node.NodeRank}", dx, dy, 14, 0xffffffffu);
            }

            StateNode lastNode = nodeFields[y][maxDepth - 1].Item2;
            string searcherInfo = $"{searchPairs[y].searcher.SearcherInfo}\n\n" +
                                  // $"{string.Join('\n', Enum.GetValues<BotSettings.Flag>().Where(flag => searchPairs[y].settings.HasFlag(flag)))}\n\n" +
                                  $"{totalAttackLines} attacks ({totalGoodAttacks})\n" +
                                  $"{totalClearLines} lines\n" +
                                  $"{totalBadClears} bad clears\n" +
                                  $"{lastNode.Evaluation.field}fld {lastNode.GetEvaluationTotalFromRoot()}all";
            Primitives.DrawText(spriteBatch, searcherInfo, ItemOffset.x + TextOffset.x, 
                dy + TextOffset.y, 12, 0xffffffffu);
        }
        
        base.OnDraw(graphicsDevice, spriteBatch);
    }

    private void ToggleFavorite() {
        favoriteOnlyMode = !favoriteOnlyMode;
        if (favoriteOnlyMode) {
            if (favoriteIndexes.Count == 0) favoriteOnlyMode = false;
            else {
                favoriteIndexList = favoriteIndexes.ToList();
                favoriteIndex = Math.Clamp(favoriteIndex, 0, favoriteIndexes.Count - 1);
            }
        }
        
        UpdateViewNode();
    }

    private void RegisterFavorite() {
        if (favoriteIndexes.Contains(CurrentNodeIndex)) favoriteIndexes.Remove(CurrentNodeIndex);
        else favoriteIndexes.Add(CurrentNodeIndex);
        
        SaveFavorite();
    }

    private void SaveFavorite() {
        string file = $"log-var/{folderName}/{FavoriteFileName}";
        string serializedFavorites = JsonConvert.SerializeObject(favoriteIndexes.ToList());
        File.WriteAllText(file, serializedFavorites);
    }

    private void LoadFavorite() {
        string file = $"log-var/{folderName}/{FavoriteFileName}";
        if (!File.Exists(file)) return;
        string json = File.ReadAllText(file);
        List<int> loadedFavorites = JsonConvert.DeserializeObject<List<int>>(json);
        Debug.Assert(loadedFavorites != null);
        foreach (int index in loadedFavorites) {
            favoriteIndexes.Add(index);
        }
    }

    private class SearchPair {
        public readonly ISearcher searcher;
        private readonly IEvaluator evaluator;
        public readonly BotSettings settings;

        public SearchPair(string botFlags) {
            settings = BotSettings.Deserialize(botFlags);
            searcher = settings.GetSearcher();
            evaluator = settings.GetEvaluator();
        }

        public StateNode Search(GameState state) {
            return searcher.Search(state, null, evaluator, out _);
        }
    }

    private class OriginalState {
        public int searchSeed;
        public GameState state;
    }
}