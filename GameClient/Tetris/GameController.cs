using System;
using GameClient.Tetris.Input;
using GameClient.Tetris.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public class GameController {
    private GameState state;

    private bool noHold, manualDrop, manualShift, dead;
    private int garbagePending, garbageColumn, garbageLength;
    private int timerFall, timerLock, timerMove, dropSpeed, lastSrs, delayFrame;
    private GameMove lastMove;

    private Vector2Int minoPosition; 
    private Mino currentMino;

    public GameState State => state;
    public bool IsDead => dead;
    public GameStatistics Statistics { get; }
    private ColoredGameField GameField => (ColoredGameField)state.Field;
    private bool CanHardDrop => manualDrop || delayFrame <= 0; 
        
    public GameController(GameState gameState = null, int bagSeed = -1) {
        state = gameState ?? new GameState(new ColoredGameField(), bagSeed: bagSeed);

        timerMove = -1;
        
        lastMove = GameMove.None;
        Statistics = new GameStatistics();

        SpawnMino();
    }

    public void Update() {
        if (manualDrop) return;
        timerFall -= dropSpeed;
        delayFrame--;
        
        if (timerFall <= 0) {
            if (!DropMino()) {
                if (--timerLock <= 0) LockMino();
            } else {
                timerLock = Constants.LockTime;
                timerFall = Constants.FallTime;
            }
            
        }
    }

    public void ProcessInput(InputSystem input) {
        InputState inputState = input.PopState();
        if (inputState.IsPressed(InputKey.SpinClock)) {
            RotateMino(true);
        }

        if (inputState.IsPressed(InputKey.SpinCounterClock)) {
            RotateMino(false);
        }
        
        if (inputState.IsPressed(InputKey.Left) || inputState.IsPressed(InputKey.Right)) {
            int moveDirection = inputState.IsPressed(InputKey.Left) ? -1 : 1;
            timerMove = Constants.DelayedAutoShift;
            MoveMino(moveDirection);
        }

        if (inputState.IsPressed(InputKey.HardDrop) && CanHardDrop) { 
            GroundMino();
            LockMino();
        }
        
        if (inputState.IsPressed(InputKey.SoftDrop) && manualDrop) {
            DropMino();
        }

        if (inputState.IsPressed(InputKey.Hold) && !noHold) {
            Statistics.OnStep();
            noHold = true;
            state.HoldMino();
            SpawnMino();
        }

        if (inputState.IsPressed(InputKey.Pause)) {
            ChangeInputMode(!manualDrop, !manualShift);
        }

        if (manualShift) return;
        dropSpeed = inputState.IsDown(InputKey.SoftDrop) ? Constants.SoftDropFactor : 1;
        if (inputState.IsDown(InputKey.Left) || inputState.IsDown(InputKey.Right)) {
            int movingDirection = inputState.IsDown(InputKey.Left) ? -1 : 1;
            if (--timerMove <= 0) {
                timerMove = Constants.AutoRepeatRate;
                MoveMino(movingDirection);
            }
        } else {
            timerMove = -1;
        }
    }

    public void Draw(SpriteBatch spriteBatch) {
        DrawField(spriteBatch);
        DrawMinos(spriteBatch);
    }

    private void DrawField(SpriteBatch spriteBatch) {
        GameField.Draw(spriteBatch, Constants.DrawFieldOffsetX, Constants.DrawFieldOffsetY, Constants.DrawBlockSize);
    }

    private void DrawMinos(SpriteBatch spriteBatch) {
        // draw current mino and ghost mino
        int groundHeight = minoPosition.y;
        while (!state.Field.WillCollideMino(currentMino, minoPosition.x, groundHeight - 1)) groundHeight--;
        Vector2Int ghostPos = FieldPosToGlobal(minoPosition.x, groundHeight);
        Color minoColor = Color.Lerp(new(Constants.MinoColorCodes[currentMino.type]), new(Constants.BackgroundColor), 0.5f);
        currentMino.Draw(spriteBatch, ghostPos.x, ghostPos.y, Constants.DrawBlockSize, overrideColor: minoColor.PackedValue);
        Vector2Int pos = FieldPosToGlobal(minoPosition.x, minoPosition.y);
        currentMino.Draw(spriteBatch, pos.x, pos.y, Constants.DrawBlockSize);
        
        
        // draw holding mino
        if (state.HoldingMino != -1) {
            Mino holdMino = new Mino(state.HoldingMino, 0);
            int largeDelta = Constants.DrawHoldBlockSize * (holdMino.Size.x - 3) / 2;
            holdMino.Draw(spriteBatch, Constants.DrawHoldOffsetX - largeDelta, 
                Constants.DrawHoldOffsetY + largeDelta, Constants.DrawHoldBlockSize);
        }

        // draw next mino
        for (int i = 0; i < Constants.DrawNextCount; i++) {
            Mino mino = new Mino(state.PeekNextMino(i), 0);
            int largeDelta = Constants.DrawNextBlockSize * (mino.Size.x - 3) / 2;
            mino.Draw(spriteBatch, Constants.DrawNextOffsetX - largeDelta, 
                Constants.DrawNextOffsetY + largeDelta + Constants.DrawNextSpace * i, Constants.DrawNextBlockSize);
        }
    }

    private void LockMino() {
        Statistics.OnStep();
        bool lastSpin = lastMove == GameMove.Spin, lastSrs4 = lastSrs == 4;
        MovementResult result = state.LockMino(lastSpin, lastSrs4, CurrentMinoState);
        Statistics.OnMinoLocked(currentMino.type, result);
        noHold = false;
        SpawnMino();
    }

    private void SpawnMino() {
        timerLock = Constants.LockTime;
        timerFall = Constants.FallTime;
        currentMino = new Mino(state.CurrentMino, 0);
        minoPosition = Constants.MinoSpawnPosition;
        if (state.Field.WillCollideMino(currentMino, minoPosition.x, minoPosition.y)) Kill();
        if (!manualDrop) delayFrame = 3;
    }

    private bool DropMino() {
        if (state.Field.WillCollideMino(currentMino, minoPosition.x, minoPosition.y - 1)) return false;
        Statistics.OnStep();
        lastMove = GameMove.Drop;
        minoPosition.y--;
        return true;
    }

    private bool MoveMino(int movingDirection) {
        if (state.Field.WillCollideMino(currentMino, minoPosition.x + movingDirection, minoPosition.y)) return false;
        Statistics.OnStep();
        lastMove = GameMove.Move;
        minoPosition.x += movingDirection;
        return true;
    }

    public static bool TryPerformRotation(GameField field, Mino mino, MinoState state, bool clockwise, out MinoState newState, out int srsPattern) {
        Mino rotatedMino = mino.Rotated(clockwise);
        // check every srs delta patterns and use the first match
        for (int i = 0; i < 5; i++) {
            Vector2Int srsDelta = rotatedMino.WallKickDelta(clockwise, i);
            if (!field.WillCollideMino(rotatedMino, state.x + srsDelta.x, state.y + srsDelta.y)) {
                newState = new MinoState(state.x + srsDelta.x, state.y + srsDelta.y, rotatedMino.rotation);
                srsPattern = i;
                return true;
            }
        }
        
        // if all 5 srs check fails, rotation failed
        newState = state;
        srsPattern = -1;
        return false;
    }

    private bool RotateMino(bool clockwise) {
        Statistics.OnStep();
        if (TryPerformRotation(state.Field, currentMino, CurrentMinoState, clockwise, out MinoState newState, out int srsPattern)) {
            minoPosition = new Vector2Int(newState.x, newState.y);
            lastSrs = srsPattern;
            lastMove = GameMove.Spin;
            currentMino = currentMino.Rotated(clockwise);
            return true;
        }
        
        // if all 5 srs check fails, rotation failed
        return false;
    }

    private void GroundMino() {
        while (!state.Field.WillCollideMino(currentMino, minoPosition.x, minoPosition.y - 1)) {
            minoPosition.y--;
            lastMove = GameMove.Drop;
        }
    }

    public void ChangeInputMode(bool manualDrop = true, bool manualShift = true) {
        this.manualDrop = manualDrop;
        this.manualShift = manualShift;
    }

    public void Kill() {
        dead = true;
        Statistics.dead = true;
    } 

    public Vector2Int FieldPosToGlobal(int x, int y) {
        return new Vector2Int(Constants.DrawFieldOffsetX + x * Constants.DrawBlockSize,
            Constants.DrawFieldOffsetY + (Constants.GameFieldHeight - y - 1) * Constants.DrawBlockSize);
    }

    private MinoState CurrentMinoState => new(minoPosition.x, minoPosition.y, currentMino.rotation);

    private enum GameMove {
        None, Move, Spin, Drop
    }
}