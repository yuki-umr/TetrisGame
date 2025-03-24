using System;
using System.Collections.Generic;

namespace GameClient.Tetris; 

public class PatternDefinition {
    private static readonly PatternDefinition[] Patterns = {
        new() {
            patternType = PatternType.NormalTsd,
            pattern = new[,] {
                { 1, 0, 1 },
                { 0, 0, 0 },
                { 1, 0, 0 }
            },
            checkMinoPlacement = true,
            minoType = 0,
            minoState = new MinoState(0, 0, 2),
            pivot = new Vector2Int(2, 0),
            fieldHeightRestriction = CheckHeightTsd
        },
        
        new() {
            patternType = PatternType.NormalTsd,
            pattern = new[,] {
                { 1, 0, 0 },
                { 0, 0, 0 },
                { 1, 0, 1 }
            },
            checkMinoPlacement = true,
            minoType = 0,
            minoState = new MinoState(0, 0, 2),
            pivot = new Vector2Int(0, 0),
            fieldHeightRestriction = CheckHeightTsdMirror
        },

        new() {
            patternType = PatternType.NormalTst,
            pattern = new[,] {
                { 0, 0, 0, 0, 1 },
                { 2, 0, 1, 0, 0 },
                { 2, 2, 2, 0, 0 }
            },
            checkMinoPlacement = true,
            minoType = 0,
            minoState = new MinoState(-1, 0, 1),
            pivot = new Vector2Int(1, 2),
            fieldHeightRestriction = CheckHeightTst
        },
        
        new() {
            patternType = PatternType.NormalTst,
            pattern = new[,] {
                { 2, 2, 2, 0, 0 },
                { 2, 0, 1, 0, 0 },
                { 0, 0, 0, 0, 1 }
            },
            checkMinoPlacement = true,
            minoType = 0,
            minoState = new MinoState(1, 0, 3),
            pivot = new Vector2Int(1, 2),
            fieldHeightRestriction = CheckHeightTstMirror
        },
        
        new() {
            patternType = PatternType.PreNormalTsd,
            pattern = new[,] {
                { 1, 0 },
                { 0, 0 },
                { 1, 0 }
            },
            checkMinoPlacement = false,
            pivot = new Vector2Int(0, 0),
            fieldHeightRestriction = CheckHeightPreTsd
        },

        new() {
            patternType = PatternType.PreNormalTst,
            pattern = new[,] {
                { 0, 0, 0, 0, 0 },
                { 2, 0, 1, 0, 0 },
                { 2, 2, 2, 0, 0 }
            },
            checkMinoPlacement = false,
            pivot = new Vector2Int(1, 2),
            fieldHeightRestriction = CheckHeightPreTst
        },
        
        new() {
            patternType = PatternType.PreNormalTst,
            pattern = new[,] {
                { 2, 2, 2, 0, 0 },
                { 2, 0, 1, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            checkMinoPlacement = false,
            pivot = new Vector2Int(1, 2),
            fieldHeightRestriction = CheckHeightPreTstMirror
        },
    };

    private static bool CheckHeightTsd(ref Span<int> height, GameField field, int x, int y) {
        return height[2] > height[1] && height[0] > height[2] + 2;
    }
    
    private static bool CheckHeightTsdMirror(ref Span<int> height, GameField field, int x, int y) {
        return height[0] > height[1] && height[2] > height[0] + 2;
    }

    private static bool CheckHeightTst(ref Span<int> height, GameField field, int x, int y) {
        return height[1] >= height[2] && height[0] >= height[1] + 2 && field[x + 3, y + 3] == field[x + 3, y + 4];
    }

    private static bool CheckHeightTstMirror(ref Span<int> height, GameField field, int x, int y) {
        return height[1] >= height[0] && height[2] >= height[1] + 2 && field[x - 1, y + 3] == field[x - 1, y + 4];
    }

    private static bool CheckHeightPreTsd(ref Span<int> height, GameField field, int x, int y) {
        return height[0] == height[2] && height[0] > height[1];
    }

    private static bool CheckHeightPreSkyTsd1(ref Span<int> height, GameField field, int x, int y) {
        return height[1] + 2 < height[0] && height[2] + 2 < height[0];
    }

    private static bool CheckHeightPreSkyTsd1Mirror(ref Span<int> height, GameField field, int x, int y) {
        return height[0] + 2 < height[2] && height[1] + 2 < height[2];
    }

    private static bool CheckHeightPreSkyTsd2(ref Span<int> height, GameField field, int x, int y) {
        return height[1] + 3 < height[0] && height[2] + 3 < height[0];
    }

    private static bool CheckHeightPreSkyTsd2Mirror(ref Span<int> height, GameField field, int x, int y) {
        return height[0] + 3 < height[2] && height[1] + 3 < height[2];
    }

    private static bool CheckHeightPreTst(ref Span<int> height, GameField field, int x, int y) {
        return height[1] >= height[2] && height[0] < height[1] && field[x + 3, y + 3] == field[x + 3, y + 4];
    }

    private static bool CheckHeightPreTstMirror(ref Span<int> height, GameField field, int x, int y) {
        return height[1] >= height[0] && height[2] < height[1] && field[x - 1, y + 3] == field[x - 1, y + 4];
    }

    public static List<Pattern> GetAllPatterns() {
        List<Pattern> patterns = new List<Pattern>();
        foreach (PatternDefinition patternDefinition in Patterns) {
            patterns.Add(patternDefinition.ConvertToPattern(patterns.Count));
        }

        return patterns;
    }

    private PatternType patternType;
    private int[,] pattern;
    private bool checkMinoPlacement;
    private int minoType;
    private MinoState minoState;
    private Vector2Int pivot;

    private FieldHeightRestrictionDelegate fieldHeightRestriction;
    
    private Pattern ConvertToPattern(int indexOffset) {
        BitMatrixPattern matrix = new BitMatrixPattern(pattern, new Vector2Int(pattern.GetLength(0), pattern.GetLength(1)));
        
        return new Pattern(patternType, matrix, minoType, indexOffset, minoState, pivot, fieldHeightRestriction, checkMinoPlacement);
    }
}