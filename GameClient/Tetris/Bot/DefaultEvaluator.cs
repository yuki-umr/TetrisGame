using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace GameClient.Tetris;

public class DefaultEvaluator : IEvaluator {
    private readonly FieldParameters field;
    private readonly MoveParameters move;
    private readonly BotSettings settings;

    private readonly List<Pattern> patterns;

    public string ControlFlags => settings.ToString();

    private DefaultEvaluator(FieldParameters field, MoveParameters move, BotSettings settings) {
        this.field = field;
        this.move = move;
        this.settings = settings;

        if (settings.Genes is null) {
            field.wellDistance[0] = this.settings.WallWellMultiplier;
            if (this.settings.WallWellCcBased) field.wellDistance = new[] { 40, 40, 40, 10, 0 };
            field.tsd = Convert.ToInt32(field.tsd * this.settings.TsdWeightMod);
            move.tSpin[1] = Convert.ToInt32(move.tSpin[1] * this.settings.TsdWeightMod);

            field.preTsd = this.settings.PreTsdWeight;
            field.preTst = this.settings.PreTstWeight;

            field.tst = Convert.ToInt32(field.tst * this.settings.TstWeightMod);
            move.tSpin[2] = Convert.ToInt32(move.tSpin[2] * this.settings.TstWeightMod);

            if (this.settings.OverrideDoubleWeight) 
                move.lineClear[1] = this.settings.DoubleLineWeight;
        }
        

        patterns = PatternDefinition.GetAllPatterns();
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
            int[] parameterArray = result.tSpin ? (result.tSpinMini ? move.tSpinMini : move.tSpin) : move.lineClear;
            movementValue += parameterArray[result.lineClear - 1];
            if (result.perfectCleared) movementValue += move.perfect;
        }

        // T mino waste penalty
        if (settings.CheckTWaste && mino.type == 0 && !result.tSpin) movementValue += move.tWasted;

        int fieldValue = EvaluateField(stateAfterLock.Field, out List<PatternMatchData> patternsFound);

        Evaluation evaluation = new(fieldValue, movementValue, result, stateAfterLock, patternsFound);
        return evaluation;
    }

    public int EvaluateField(GameField gameField, out List<PatternMatchData> patternsFound) {
        patternsFound = FindPatternMatches(gameField);
        int value = 0;
        value += KeyFieldHeight(gameField);
        value += KeyFieldCeiling(gameField);
        value += KeyFieldSteepness(gameField);
        value += KeyFieldWalledWell(gameField);
        value += KeyPatternMatches(patternsFound, gameField);
        
        return value;
    }

    public int KeyFieldHeight(GameField gameField) {
        int fieldHeight = gameField.FieldHeight;
        int value = settings.NoHeight ? 0 : fieldHeight * field.height;
        if (!settings.NoHeightLevel) {
            if (fieldHeight > 10) value += (fieldHeight - 10) * field.height10;
            if (fieldHeight > 15) value += (fieldHeight - 15) * field.height15;
            if (fieldHeight > 18) value += (fieldHeight - 18) * field.height18;
        }
        
        return value;
    }

    public int KeyFieldCeiling(GameField gameField) {
        int holeCount = 0, ceilDepthTotal = 0;
        int[] columnHeights = gameField.ColumnHeights;
        for (var x = 0; x < columnHeights.Length; x++) {
            holeCount += BitOperations.PopCount(~gameField[x] & ((1u << columnHeights[x]) - 1)); 
            int ceilCount = 0;
            for (int y = 0; y < columnHeights[x]; y++) {
                ceilDepthTotal += ceilCount;
                if (gameField[x, y]) continue;
                holeCount++;
                if (gameField[x, y + 1]) ceilCount++;
            }
        }

        if (settings.NoCeiling) holeCount = 0;
        if (settings.NoCeilDepth) ceilDepthTotal = 0;
        return holeCount * field.ceiling + ceilDepthTotal * field.ceilDepth;
    }

    public int KeyFieldSteepness(GameField gameField) {
        int[] columnHeights = gameField.ColumnHeights;
        int lowestColumn = gameField.LowestColumn;
        int totalSteepness = 0, totalSSteepness = 0, totalFlatness = 0;
        for (int i = 0; i < columnHeights.Length - 1; i++) {
            int nextColumn = (i + 1 == lowestColumn) ? i + 2 : i + 1;
            if (i == lowestColumn || nextColumn == columnHeights.Length ) continue;
            int steepness = Math.Abs(columnHeights[nextColumn] - columnHeights[i]);
            totalSteepness += steepness;
            totalSSteepness += steepness * steepness;
            if (steepness == 0) totalFlatness++;
        }

        if (settings.NoSteepness) totalSteepness = 0;
        if (settings.NoSSteepness) totalSSteepness = 0;
        if (settings.NoFlatness) totalFlatness = 0;
        
        return totalSteepness * field.steepness + totalSSteepness * field.sSteepness + totalFlatness * field.flatness;
    }

    public int KeyFieldWalledWell(GameField gameField) {
        int lowestColumn = gameField.LowestColumn;
        int lowestIndex = (lowestColumn < gameField.Size.x / 2) ? lowestColumn : gameField.Size.x - lowestColumn - 1;
        if (settings.NoWellDistance || settings.NoInnerWellBonus && lowestIndex > 0) return 0;
        if (settings.NoWallWellPenalty && !settings.NoInnerWellBonus && lowestIndex == 0) return 0;
        int wellHeight = settings.FixedWellHeight 
            ? (gameField.LowestDepth > 0 ? 5 : 0)
            : Math.Min(gameField.LowestDepth, 4);
        if (lowestIndex == 0) wellHeight *= settings.WallWellMultiplier;
        return wellHeight * field.wellDistance[lowestIndex];
    }

    public int KeyPatternMatches(List<PatternMatchData> patternMatches, GameField gameField) {
        int totalEvaluation = 0;
        foreach (PatternMatchData matchData in patternMatches) {
            if (matchData.pattern.checkMinoPlacement) {
                Mino simulateMino = new Mino(matchData.pattern.minoType, matchData.state.rotation);
                int clearCount = gameField.SimulateMinoClear(simulateMino, matchData.state.x, matchData.state.y);
                if (matchData.pattern.type is PatternType.NormalTsd) {
                    totalEvaluation += clearCount * field.tsd;
                } else if (matchData.pattern.type is PatternType.NormalTst) {
                    totalEvaluation += clearCount * field.tst;
                }
            } else {
                if (matchData.pattern.type is PatternType.PreNormalTsd) {
                    totalEvaluation += field.preTsd;
                } else if (matchData.pattern.type is PatternType.PreNormalTst) {
                    totalEvaluation += field.preTst;
                }
            } 
        }

        return totalEvaluation;
    }

    private List<PatternMatchData> FindPatternMatches(GameField gameField) {
        List<PatternMatchData> matches = new List<PatternMatchData>();
        if (settings.NoPatternCheck) return matches;
        Span<int> columnHeights = new Span<int>(gameField.ColumnHeights);
        foreach (Pattern pattern in patterns) {
            if (settings.NoTsd && pattern.type == PatternType.NormalTsd) continue;
            if (settings.NoTst && pattern.type == PatternType.NormalTst) continue;
            if (settings.PreTsdWeight == 0 && pattern.type == PatternType.PreNormalTsd) continue;
            if (settings.PreTstWeight == 0 && pattern.type == PatternType.PreNormalTst) continue;

            for (int x = 0; x < gameField.Size.x - pattern.pattern.Size.x; x++) {
                int y = columnHeights[x + pattern.pivot.x] - pattern.pivot.y - 1;
                bool match = pattern.TestFieldRestriction(ref columnHeights, gameField, x, y) && gameField.CheckPatternMatch(pattern.pattern, x, y);
                if (!match) continue;

                // simulate line clear
                MinoState placeState = new MinoState(x + pattern.minoState.x, y + pattern.minoState.y, pattern.minoState.rotation);
                matches.Add(new PatternMatchData(placeState, pattern));
            }
        }
        
        return matches;
    }

    public static DefaultEvaluator GetDefault(BotSettings settings) {
        if (settings.Genes is null) {
            return CreateModifiedLemonTea(settings);
        } else {
            return CreateFromGeneValues(settings.Genes.Select(g => -(int)g.Value));
        }
    }

    public static DefaultEvaluator CreateModifiedLemonTea(BotSettings settings) {
        DefaultEvaluator evaluator = CreateLemonTea(settings);
        // evaluator.field.height = 0;
        // evaluator.field.height10 = -10;

        evaluator.move.tSpin[0] = 100;
        return evaluator;
    } 

    public static DefaultEvaluator CreateLemonTea(BotSettings settings) {
        FieldParameters field = new FieldParameters {
            height = -41,
            surround = -160,
            ceiling = -32 - 64,
            steepness = -30,
            tsd = 152 + 64 + 20,
            tst = 502,
            preTsd = 20,
            preTst = 30,
            height10 = -150,
            height15 = -600,
            height18 = -999,
            sSteepness = -7,
            flatness = 9,
            ceilDepth = -32,
            wellDistance = new []{ -40, -20, -5, -2, 0 } // 21
        };

        MoveParameters move = new MoveParameters {
            lineClear = new[] { -392, -193, -214, 270 },
            tSpin = new[] { 52, 397, 785 },
            tSpinMini = new[] { 0, 0 },
            perfect = 1500,
            tWasted = -134
        };

        return new DefaultEvaluator(field, move, settings);
    }
    
    public static DefaultEvaluator CreateLemonTeaInverted(BotSettings settings) {
        FieldParameters field = new FieldParameters {
            height = 41,
            surround = 160,
            ceiling = 32 + 64,
            steepness = 30,
            tsd = -152 - 64 - 20,
            tst = -502,
            preTsd = -20,
            preTst = -30,
            height10 = 150,
            height15 = 600,
            height18 = 99999999,
            sSteepness = 7,
            flatness = -9,
            ceilDepth = 32,
            wellDistance = new []{ 40, 20, 5, 2, 0 } // 21
        };

        MoveParameters move = new MoveParameters {
            lineClear = new[] { 392, 193, 214, -270 },
            tSpin = new[] { -52, -397, -785 },
            tSpinMini = new[] { 0, 0 },
            perfect = -1500,
            tWasted = 134
        };

        return new DefaultEvaluator(field, move, settings);
    }

    private class FieldParameters {
        public int height, surround, ceiling, steepness, tsd, tst, preTsd, preTst;
        public int height10, height15, height18, sSteepness, flatness, ceilDepth;
        public int[] wellDistance;
    }

    private class MoveParameters {
        public int[] lineClear, tSpin, tSpinMini;
        public int perfect;
        public int tWasted;
    }

    public static DefaultEvaluator CreateFromGeneValues(IEnumerable<int> geneCollection) {
        FieldParameters fieldParams = new();
        MoveParameters moveParams = new();
        using IEnumerator<int> genes = geneCollection.GetEnumerator();
        genes.MoveNext();

        DefaultEvaluator defaultEvaluator = GetDefault(new BotSettings());
        object[] targets = { fieldParams, moveParams };
        object[] defaults = { defaultEvaluator.field, defaultEvaluator.move };
        for (int i = 0; i < targets.Length; i++) {
            object target = targets[i];
            foreach (FieldInfo field in target.GetType().GetFields()) {
                if (field.FieldType == typeof(int)) {
                    field.SetValue(target, genes.Current);
                    genes.MoveNext();
                } else if (field.FieldType == typeof(int[])) {
                    // use default evaluator to estimate the array size
                    int arrayLength = ((int[])field.GetValue(defaults[i]))!.Length;
                    int[] targetArray = new int[arrayLength];
                    for (int j = 0; j < arrayLength; j++) {
                        targetArray[j] = genes.Current;
                        genes.MoveNext();
                    }
                    
                    field.SetValue(target, targetArray);
                } else {
                    Console.Error.WriteLine($"DefaultEvaluator.CreateFromGeneValues: type {field.FieldType} is not supported");
                    throw new NotImplementedException();
                }
            }
        }

        //test
        // Console.WriteLine($"#1 {string.Join(',', geneCollection.ToArray())}");
        // Console.WriteLine($"#2 {string.Join(',', GetGenes(fieldParams, moveParams))}");
        return new DefaultEvaluator(fieldParams, moveParams, new BotSettings());
    }

    private static int[] GetGenes(FieldParameters field, MoveParameters move) {
        object[] targets = { field, move };
        List<int> genes = new List<int>();
        foreach (object target in targets) {
            foreach (FieldInfo f in target.GetType().GetFields()) {
                if (f.FieldType == typeof(int)) {
                    genes.Add((int) f.GetValue(target)!);
                } else if (f.FieldType == typeof(int[])) {
                    genes.AddRange((int[]) f.GetValue(target)!);
                }
            }
        }

        return genes.ToArray();
    }
}