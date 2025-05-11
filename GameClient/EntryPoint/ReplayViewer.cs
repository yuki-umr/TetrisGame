using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameClient.Tetris;
using GameClient.Tetris.Input;
using GameClient.Tetris.Replay;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using MovementResult = GameClient.Tetris.Replay.MovementResult;

namespace GameClient.EntryPoint;

public class ReplayViewer : WindowManager {
    private static readonly bool FastReplay = true;
    
    private const int MaxDisplayFileCount = 30, BlockSize = 16, NextSpacing = 64, ReplaySpacing = 360, MaxLogs = 5;
    private static readonly Vector2Int TextOffset = new(16, 16), ReplayOffset = new(320, 32), HoldOffset = new(-64, 64), NextOffset = new(192, 64),
        AttackLogOffset = new(0, 360);
    
    private const string LogDirectory = @"C:\ppt2logs\replays";
    
    private KeyboardInput inputSystem;
    
    private ReplayFileInfo[] replayFiles;
    private int selectedFileIndex, viewportFileIndexOffset;

    private ReplayData currentReplay;
    private bool playbackPaused;
    private readonly List<PlaybackState> playbackStates = new();
    
    
    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);
    public override bool FixedTimeStep => true;

    protected override void OnInitialize() {
        inputSystem = new KeyboardInput();
        var files = Directory.GetFiles(LogDirectory, "replay_*.json").Select(fileName => new FileInfo(fileName));
        replayFiles = files.Select(ReplayFileInfo.FromFile).ToArray();
    }
    
    
    // ////////////////////////////////////////////////////////
    // Update logic

    protected override int OnUpdate() {
        inputSystem.Update();
        InputState inputState = inputSystem.PopState();
        ProcessFileViewerInput(ref inputState);
        ProcessReplayUpdate(ref inputState);

        return 0;
    }
    
    private void ProcessFileViewerInput(ref InputState inputState) {
        if ((inputState.IsPressed(InputKey.SoftDrop) || (inputState.IsDown(InputKey.Hold) && inputState.IsDown(InputKey.SoftDrop))) 
            && selectedFileIndex < replayFiles.Length - 1) {
            selectedFileIndex++;
        } else if ((inputState.IsPressed(InputKey.HardDrop) || (inputState.IsDown(InputKey.Hold) && inputState.IsDown(InputKey.HardDrop)))
                   && selectedFileIndex > 0) {
            selectedFileIndex--;
        } else if (inputState.IsPressed(InputKey.Right)) {
            selectedFileIndex += MaxDisplayFileCount;
            selectedFileIndex = selectedFileIndex > replayFiles.Length - 1 ? replayFiles.Length - 1 : selectedFileIndex;
        } else if (inputState.IsPressed(InputKey.Left)) {
            selectedFileIndex -= MaxDisplayFileCount;
            selectedFileIndex = selectedFileIndex < 0 ? 0 : selectedFileIndex;
        }
        
        if (selectedFileIndex < viewportFileIndexOffset + 1) {
            viewportFileIndexOffset = selectedFileIndex - 1;
            if (viewportFileIndexOffset < 0) viewportFileIndexOffset = 0;
        } else if (selectedFileIndex >= viewportFileIndexOffset + MaxDisplayFileCount - 2) {
            viewportFileIndexOffset = selectedFileIndex - MaxDisplayFileCount + 2;
            if (viewportFileIndexOffset + MaxDisplayFileCount >= replayFiles.Length) {
                viewportFileIndexOffset = replayFiles.Length - MaxDisplayFileCount;
            }
        }

        if (inputState.IsPressed(InputKey.SpinCounterClock)) {
            ReplayFileInfo selectedReplay = replayFiles[selectedFileIndex];
            string replayJson = File.ReadAllText(selectedReplay.fileInfo.FullName);
            Stopwatch sw = Stopwatch.StartNew();
            ReplayData? deserializedReplay = JsonConvert.DeserializeObject<ReplayData>(replayJson);
            sw.Stop();

            if (deserializedReplay != null) {
                VersionManager.Apply(deserializedReplay);
                currentReplay = deserializedReplay;
                playbackStates.Clear();
                for (int i = 0; i < currentReplay.playerCount; i++) {
                    playbackStates.Add(new PlaybackState());
                }

                Console.WriteLine($"Loaded replay for {currentReplay.playerCount} players in {sw.Elapsed.TotalMilliseconds}ms");
                Console.WriteLine(JsonConvert.SerializeObject(currentReplay, Formatting.Indented));
            }
        }
    }

    private void ProcessReplayUpdate(ref InputState inputState) {
        if (inputState.IsPressed(InputKey.SpinClock)) playbackPaused = !playbackPaused;
        if (currentReplay is null || playbackPaused) return;
        
        for (int i = 0; i < currentReplay.playerCount; i++) {
            List<GameStateNode> nodes = currentReplay.gameStates[i];
            PlaybackState state = playbackStates[i];
            
            if (state.playbackCompleted || nodes.Count <= state.nodeIndex) continue;

            int nextRecordNodeIndex = state.nodeIndex, nextRecordIndex = state.minoRecordIndex, recordFrameCount = state.minoRecordFrameElapsed;
            bool keepPlaying = true, firstSkip = true;
            while (true) {
                if (nodes.Count <= nextRecordNodeIndex) {
                    keepPlaying = false;
                    break;
                }

                if (nodes[nextRecordNodeIndex].minoRecords.Count <= nextRecordIndex) {
                    MovementResult result = nodes[nextRecordNodeIndex].movementResult;
                    if (result.resultType == 0 && result.lineClearCount > 0) state.attackLogs.Add(CreateAttackResultString(result));
                    // else attackLogs.Add("HOLD");
                    
                    recordFrameCount = 0;
                    nextRecordIndex = 0;
                    nextRecordNodeIndex++;
                    continue;
                }
                
                if (nodes[nextRecordNodeIndex].minoRecords[nextRecordIndex].frameSinceLastRecord <= recordFrameCount || (FastReplay && firstSkip)) {
                    recordFrameCount = 0;
                    nextRecordIndex++;
                    firstSkip = false;
                    continue;
                }
                
                break;
            }

            if (!keepPlaying) {
                state.playbackCompleted = true;
                continue;
            }
            
            state.minoRecordFrameElapsed = recordFrameCount + 1;
            state.nodeIndex = nextRecordNodeIndex;
            state.minoRecordIndex = nextRecordIndex;
        }
    }

    private string CreateAttackResultString(MovementResult movementResult) {
        StringBuilder sb = new();
        if (movementResult.isTSpin) {
            sb.Append("T-spin ");
            if (movementResult.isMini) sb.Append("Mini ");
            sb.Append(movementResult.lineClearCount);
        } else {
            sb.Append(movementResult.lineClearCount).Append(" cleared");
        }
        
        if (movementResult.isBackToBack) sb.Append(" BTB");
        if (movementResult.isPerfectClear) sb.Append(" PC");
        if (movementResult.renCount > 2) sb.Append(movementResult.renCount).Append("ren");
        return sb.ToString();
    }
    
    
    // ////////////////////////////////////////////////////////
    // Draw logic

    Stopwatch globalStopwatch = new();
    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        globalStopwatch.Restart();
        DrawFileViewer(spriteBatch);
        DrawReplay(spriteBatch);
    }

    private void DrawFileViewer(SpriteBatch spriteBatch) {
        for (int i = 0; i < MaxDisplayFileCount; i++) {
            int fileIndex = viewportFileIndexOffset + i;
            if (fileIndex < 0 || replayFiles.Length <= fileIndex) break;
            
            bool selected = fileIndex == selectedFileIndex;
            ReplayFileInfo replay = replayFiles[fileIndex];
            Primitives.DrawText(spriteBatch, replay.displayName, TextOffset.x, TextOffset.y + 16 * i, 12, selected ? 0xff00ff00u : 0xffffffffu);
        }
    }

    private void DrawReplay(SpriteBatch spriteBatch) {
        if (currentReplay is null) return;
        for (int i = 0; i < currentReplay.playerCount; i++) {
            PlaybackState state = playbackStates[i];
            
            if (currentReplay.gameStates[i].Count <= state.nodeIndex) continue;
            GameStateNode node = currentReplay.gameStates[i][state.nodeIndex];
            Vector2Int drawOffset = ReplayOffset + new Vector2Int(ReplaySpacing * i, 0);

            for (int x = 0; x < state.field.Size.x; x++) {
                for (int y = 0; y < state.field.Size.y; y++) {
                    if (node.gameField.IsFilled(x, y, out int blockType)) {
                        state.field.SetBlock(x, y, blockType);
                    } else {
                        state.field.UnsetBlock(x, y);
                    }
                }
            }
            
            state.field.Draw(spriteBatch, drawOffset.x, drawOffset.y, BlockSize);

            if (node.TryGetCurrentMino(out int currentMinoType)) {
                MinoRecord currentMinoRecord = node.minoRecords[state.minoRecordIndex];
                
                // position handling for 'I' minos are different in PPT so we need to adjust for that
                Vector2Int drawPosition = new(currentMinoRecord.x, currentMinoRecord.y);
                if (currentMinoType == 6 && currentMinoRecord.rotation < 2) {
                    drawPosition += new Vector2Int(currentMinoRecord.rotation is > 0 and < 3 ? -1 : 0, currentMinoRecord.rotation < 2 ? -1 : 0);
                }
                
                state.currentMino = new Mino(currentMinoType, currentMinoRecord.rotation);
                state.currentMino.Draw(spriteBatch, drawOffset.x + BlockSize * (drawPosition.x - 1), 
                    drawOffset.y + BlockSize * (20 - drawPosition.y), BlockSize);
            }

            for (int nextIndex = 0; nextIndex < state.nextMinos.Length; nextIndex++) {
                if (!node.TryGetNextMino(nextIndex, out int nextMino)) continue;
                state.nextMinos[i] = new Mino(nextMino, 0);
                Vector2Int nextDrawOffset = drawOffset + NextOffset;
                state.nextMinos[i].Draw(spriteBatch, nextDrawOffset.x, nextDrawOffset.y + NextSpacing * nextIndex, BlockSize);
            }
            
            if (node.TryGetHoldMino(out int holdMinoType)) {
                state.holdMino = new Mino(holdMinoType, 0);
                state.holdMino.Draw(spriteBatch, drawOffset.x + HoldOffset.x, drawOffset.y + HoldOffset.y, BlockSize);
            }

            if (state.playbackCompleted) {
                Primitives.DrawText(spriteBatch, "[EOF]", drawOffset.x + 32, drawOffset.y + 32, 16, 0xffffffffu);
            }

            for (int j = 0; j < Math.Min(MaxLogs, state.attackLogs.Count); j++) {
                Vector2Int textOffset = drawOffset + TextOffset;
                Primitives.DrawText(spriteBatch, state.attackLogs[^(j + 1)], textOffset.x, textOffset.y + 16 * j, 16, 0xffffffffu);
            }
        }
    }
    
    
    // ////////////////////////////////////////////////////////
    // Data structs

    private class ReplayFileInfo {
        public FileInfo fileInfo;
        public string displayName;

        public static ReplayFileInfo FromFile(FileInfo fileInfo, int index) => new() {
            fileInfo = fileInfo,
            displayName = $"{index:D5} {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}",
        };
    }

    private class PlaybackState {
        public bool playbackCompleted;
        public int nodeIndex, minoRecordIndex, minoRecordFrameElapsed;

        public readonly ColoredGameField field = new();
        public readonly Mino[] nextMinos = new Mino[Constants.DrawNextCount];
        public readonly List<string> attackLogs = new();
        public Mino currentMino, holdMino;
    }
}