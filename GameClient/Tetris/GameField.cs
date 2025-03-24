using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace GameClient.Tetris; 

public class GameField : IStateSerializable {
    private readonly BitMatrix field;
    private readonly FieldIndex index;

    public Vector2Int Size { get; } = Constants.DefaultGameFieldSize;

    public GameField(BitMatrix initialField = null) {
        if (initialField != null) {
            field = initialField;
            Size = field.Size;
        }
        else field = new BitMatrix(Size);

        index = new FieldIndex(this);
    }

    public uint this[int x] => field[x];
    public bool this[int x, int y] => !InRange(x, y) || field[x, y];

    public int FieldHeight => index.HighestHeight;

    public bool InRange(int x, int y) => 0 <= x && x < Size.x && 0 <= y && y < Size.y;

    public void PlaceMino(Mino mino, int x, int y) {
        foreach (Vector2Int pos in mino.BlockPos) {
            SetBlock(x + pos.x, y + pos.y, mino.type);
        }
    }

    protected virtual void SetBlock(int x, int y, int block) {
        field[x, y] = true;
        index.OnSetBlock(x, y);
    }
    
    public bool WillCollideMino(Mino mino, int x, int y) {
        foreach (Vector2Int pos in mino.BlockPos) {
            if (this[x + pos.x, y + pos.y]) return true;
        }

        return false;
    }

    public bool LineFilled(int row) {
        for (int i = 0; i < Size.x; i++) {
            if (!this[i, row]) return false;
        }

        return true;
    }

    public bool LineEmpty(int row) {
        for (int i = 0; i < Size.x; i++) {
            if (this[i, row]) return false;
        }

        return true;
    }

    public int CheckClearLine() {
        uint lineClearFlag = ~0u;
        for (int x = 0; x < field.Size.x; x++) {
            lineClearFlag &= field[x];
        }

        if (lineClearFlag == 0) return 0;
        int lineCleared = 0;
        List<int> nonCleared = new(), clearHeight = new(), clearCount = new();
        for (int y = 0; y < FieldHeight; y++) {
            if ((lineClearFlag & (1u << y)) == 0) {
                nonCleared.Add(y);
            } else {
                int continuousClear = BitOperations.TrailingZeroCount(~(lineClearFlag >> y));
                clearHeight.Add(y);
                clearCount.Add(continuousClear);
                y += continuousClear - 1;
                lineCleared += continuousClear;
            }
        }

        OnLineClear(nonCleared);
        for (int i = 0; i < Size.x; i++) {
            for (int j = clearHeight.Count - 1; j >= 0; j--) {
                field[i] = (field[i] & ~(~0u << clearHeight[j])) | ((field[i] & (~0u << (clearHeight[j] + clearCount[j]))) >> clearCount[j]);
            }
        }
        
        index.Recalculate();
        return lineCleared;
    }

    public int SimulateMinoClear(Mino mino, int x, int y) {
        uint allColumn = ~0u;
        for (int i = 0; i < field.Size.x; i++) {
            uint column = field[i];
            if (x <= i && i < x + mino.Size.x) {
                foreach (Vector2Int pos in mino.BlockPos) {
                    if (pos.x == i - x) column |= 1u << (y + pos.y);
                }
            }

            allColumn &= column;
        }

        return BitOperations.PopCount(allColumn);
    }

    protected virtual void OnLineClear(List<int> nonCleared) { }

    public bool CheckPatternMatch(BitMatrixPattern pattern, int x, int y) => field.CheckPatternMatch(pattern, x, y);

    private void AddGarbageLine() {
        throw new NotImplementedException();
    }

    public bool IsPerfectCleared => FieldHeight == 0;

    public int HighestColumn => index.HighestColumn;

    public int LowestColumn => index.LowestColumn;

    public int[] ColumnHeights => index.columnHeight;

    public int ColumnHeight(int column) => 0 <= column && column < Size.x ? index.columnHeight[column] : Constants.GameFieldTotalHeight;

    public int LowestDepth {
        get {
            int lowestColumn = LowestColumn, wellDepth = Size.y;
            for (int i = 0; i < Size.x; i++) {
                if (i == lowestColumn) continue;
                wellDepth = Math.Min(wellDepth, index.columnHeight[i] - index.columnHeight[lowestColumn]);
            }

            return wellDepth;
        }
    }

    public GameField Copy() {
        return new GameField(field.Copy());
    }

    public ColoredGameField ConvertToColored() => new(initialField: field);

    private bool Equals(GameField other) {
        return Equals(field, other.field);
    }

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || obj is GameField other && Equals(other);
    }

    public override int GetHashCode() {
        return (field != null ? field.GetHashCode() : 0);
    }
    
    public static bool operator ==(GameField a, GameField b) => Equals(a, b);
    
    public static bool operator !=(GameField a, GameField b) => !Equals(a, b);

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        for (int i = FieldHeight; i >= 0; i--) {
            sb.Append('[');
            for (int j = 0; j < Size.x; j++) {
                sb.Append(this[j, i] ? 'X' : ' ');
            }

            sb.Append(']');
            if (i != 0) sb.Append('\n');
        }

        return sb.ToString();
    }

    public void Serialize(BinaryWriter writer) {
        writer.Write(field);
    }

    public static GameField Deserialize(BinaryReader reader) {
        return new GameField(reader.ReadBitMatrix());
    }

    private class FieldIndex {
        private readonly GameField gameField;
        public readonly int[] columnHeight;
        private int highestColumn;
        private int lowestColumn;

        public int HighestHeight => columnHeight[HighestColumn];
        public int LowestHeight => columnHeight[LowestColumn];
        public int HighestColumn => highestColumn;
        public int LowestColumn => lowestColumn;

        public FieldIndex(GameField gameField) {
            this.gameField = gameField;
            columnHeight = new int[gameField.Size.x];
            Recalculate();
        }

        public void OnSetBlock(int x, int y) {
            // do not do anything if there is no update
            if (y + 1 <= columnHeight[x]) return;
            bool recalculate = false;

            if (y == LowestHeight) recalculate = true;
            else if (y + 1 > HighestHeight) highestColumn = x;

            columnHeight[x] = y + 1;
            if (recalculate) Recalculate();
        }

        public void Recalculate() {
            for (int i = 0; i < columnHeight.Length; i++) {
                columnHeight[i] = 32 - BitOperations.LeadingZeroCount(gameField.field[i]);
                if (columnHeight[i] > columnHeight[highestColumn]) highestColumn = i;
                if (columnHeight[i] < columnHeight[lowestColumn]) lowestColumn = i;
            }
        }
        
        
    }
}