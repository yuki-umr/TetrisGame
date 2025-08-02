using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace GameClient.Tetris;

public class ThieryEvaluator : IEvaluator {
    private readonly FieldParameters field;
    private readonly MoveParameters move;

    private ThieryEvaluator(FieldParameters field, MoveParameters move) {
        this.field = field;
        this.move = move;
    }

    public Evaluation EvaluateMove(GameState gameState, Mino mino, MinoState minoState, IEnumerable<PatternMatchData> previousPatterns = null) {
        GameState stateAfterLock = gameState.Copy();
        
        bool lastSpin = false, lastSrs4 = false;
        // if the given state matches the state in the previously found pattern, it means the pattern has been used
        if (previousPatterns != null && mino.type == 0) {
            foreach (PatternMatchData matchData in previousPatterns) {
                if (matchData.state != minoState) continue;
                (lastSpin, lastSrs4) = (true, true);
                break;
            }
        }

        MovementResult result = stateAfterLock.LockMino(lastSpin, lastSrs4, minoState);

        int movementValue = 0;
        // Line clear bonus
        if (result.lineClear != 0) {
            movementValue += result.lineClear * result.lineClear * move.erodedPieceCells;
        }

        movementValue += minoState.y * move.landingHeight;

        // T mino waste penalty
        int fieldValue = EvaluateField(stateAfterLock.Field, out List<PatternMatchData> patternsFound);

        Evaluation evaluation = new(fieldValue, movementValue, result, stateAfterLock, patternsFound);
        return evaluation;
    }

    public int EvaluateField(GameField gameField, out List<PatternMatchData> patternsFound) {
        patternsFound = new List<PatternMatchData>();
        int value = 0;
        value += KeyHoles(gameField);
        value += KeyRowTransitions(gameField);
        value += KeyColumnTransitions(gameField);
        value += KeyCumulativeWells(gameField);
        value += KeyHoleDepth(gameField);
        value += KeyRowsWithHoles(gameField);
        
        return value;
    }

    private int KeyHoles(GameField gameField) {
        int totalHoles = 0;
        for (int x = 0; x < gameField.Size.x; x++) {
            for (int y = gameField.Size.y - 1; y >= 0; y--) {
                if (!gameField[x, y]) continue;
                // Count holes below the current block
                int holesBelow = 0;
                while (!gameField[x, y - 1]) {
                    holesBelow++;
                    y--;
                }
                
                totalHoles += holesBelow;
            }
        }
        
        // for (int y = 0; y < gameField.Size.y - 1; y++) {
        //     for (int x = 0; x < gameField.Size.x; x++) {
        //         if (!gameField[x, y] && gameField[x, y - 1]) totalHoles++;
        //     }
        // }

        return totalHoles * field.holes;
    }
    
    private int KeyRowTransitions(GameField gameField) {
        int rowTransitions = 0;
        for (int y = 0; y < gameField.Size.y; y++) {
            bool lastBlock = gameField[0, y];
            for (int x = 1; x < gameField.Size.x; x++) {
                if (gameField[x, y] != lastBlock) {
                    rowTransitions++;
                    lastBlock = gameField[x, y];
                }
            }
        }

        return rowTransitions * field.rowTransitions;
    }
    
    private int KeyColumnTransitions(GameField gameField) {
        int columnTransitions = 0;
        for (int x = 0; x < gameField.Size.x; x++) {
            bool lastBlock = gameField[x, 0];
            for (int y = 1; y < gameField.Size.y; y++) {
                if (gameField[x, y] != lastBlock) {
                    columnTransitions++;
                    lastBlock = gameField[x, y];
                }
            }
        }

        return columnTransitions * field.columnTransitions;
    }
    
    private int KeyCumulativeWells(GameField gameField) {
        int cumulativeWells = 0;
        for (int x = 0; x < gameField.Size.x; x++) {
            int wellDepth = 0;
            bool isWell = false;
            if (x == 0) {
                isWell = gameField.ColumnHeights[x] < gameField.ColumnHeights[x + 1];
                wellDepth = gameField.ColumnHeights[x + 1] - gameField.ColumnHeights[x];
            } else if (x == gameField.Size.x - 1) {
                isWell = gameField.ColumnHeights[x] < gameField.ColumnHeights[x - 1];
                wellDepth = gameField.ColumnHeights[x - 1] - gameField.ColumnHeights[x];
            } else {
                isWell = gameField.ColumnHeights[x] < gameField.ColumnHeights[x - 1] &&
                         gameField.ColumnHeights[x] < gameField.ColumnHeights[x + 1];
                wellDepth = Math.Min(gameField.ColumnHeights[x - 1], gameField.ColumnHeights[x + 1]) - gameField.ColumnHeights[x];
            }

            if (!isWell) continue;
            cumulativeWells += wellDepth;
        }
        
        return cumulativeWells * field.cumulativeWells;
    }
    
    public int KeyHoleDepth(GameField gameField) {
        int totalDepth = 0;
        for (int x = 0; x < gameField.Size.x; x++) {
            int columnHeight = gameField.ColumnHeights[x], fullCells = 0;
            if (columnHeight == 0) continue; // skip empty columns
            for (int y = columnHeight - 1; y >= 0; y--) {
                if (gameField[x, y]) {
                    fullCells++;
                    continue;
                }
                
                totalDepth += fullCells; // add the number of full cells above the hole
            }
        }

        return totalDepth * field.holeDepth;
    }
    
    public int KeyRowsWithHoles(GameField gameField) {
        int rowsWithHoles = 0;
        for (int x = 0; x < gameField.Size.x; x++) {
            for (int y = 0; y < gameField.Size.y - 1; y++) {
                if (!gameField[x, y] && gameField[x, y + 1]) {
                    rowsWithHoles++;
                    break; // only count the row once
                }
            }
        }

        return rowsWithHoles * field.rowsWithHoles;
    }

    public static ThieryEvaluator GetDefault(BotSettings settings) {
        FieldParameters field = new() {
            holes = -1308,
            rowTransitions = -922,
            columnTransitions = -1977,
            cumulativeWells = -1049,
            holeDepth = -161,
            rowsWithHoles = -2404,
            patternDiversity = 0
        };
        
        MoveParameters move = new() {
            landingHeight = -1263,
            erodedPieceCells = 660
        };
        
        return new ThieryEvaluator(field, move);
    } 

    private class FieldParameters {
        public int holes, rowTransitions, columnTransitions, cumulativeWells, holeDepth, rowsWithHoles, patternDiversity;
    }

    private class MoveParameters {
        public int landingHeight, erodedPieceCells;
    }
}