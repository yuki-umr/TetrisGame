using System;
using System.Runtime.InteropServices;

namespace GameClient.Tetris; 

public class Pattern {
    public readonly PatternType type;
    public readonly BitMatrixPattern pattern;
    public readonly int minoType, patternIndex;
    public readonly MinoState minoState;
    public readonly Vector2Int pivot;
    public readonly bool checkMinoPlacement;
    private readonly FieldHeightRestrictionDelegate heightRestriction;

    public Pattern(PatternType type, BitMatrixPattern pattern, int minoType, int patternIndex, MinoState minoState, Vector2Int pivot,
        FieldHeightRestrictionDelegate heightRestriction, bool checkMinoPlacement) {
        this.type = type;
        this.pattern = pattern;
        this.minoType = minoType;
        this.patternIndex = patternIndex;
        this.minoState = minoState;
        this.pivot = pivot;
        this.heightRestriction = heightRestriction;
        this.checkMinoPlacement = checkMinoPlacement;
    }

    public bool TestFieldRestriction(ref Span<int> columnHeights, GameField gameField, int x, int y) {
        Span<int> heightWindow = columnHeights.Slice(x, pattern.Size.x);
        return heightRestriction(ref heightWindow, gameField, x, y);
    }
}


public delegate bool FieldHeightRestrictionDelegate(ref Span<int> height, GameField field, int x, int y);

public readonly struct PatternMatchData {
    public readonly MinoState state;
    public readonly Pattern pattern;

    public PatternMatchData(MinoState state, Pattern pattern) {
        this.state = state;
        this.pattern = pattern;
    }
}

public enum PatternType {
    NormalTsd,
    NormalTst,
    PreNormalTsd,
    PreNormalTst
}
