using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameClient.Tetris; 

public struct Vector2Int : IEquatable<Vector2Int> {
    public int x, y;

    public Vector2Int(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public readonly Vector2Int Inverse() => new(y, x);

    public bool Equals(Vector2Int other) {
        return x == other.x && y == other.y;
    }

    public override bool Equals(object obj) {
        return obj is Vector2Int other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(x, y);
    }

    public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
    
    public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);

    public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
    
    public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);

    public static implicit operator Vector2(Vector2Int vector) {
        return new Vector2(vector.x, vector.y);
    }

    public static readonly Vector2Int Zero = new(0, 0);

    public override string ToString() {
        return $"({x}, {y})";
    }
}

public struct Bounds : IEquatable<Bounds> {
    public int x, y, width, height;

    public Bounds(int x, int y, int width, int height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public bool Equals(Bounds other) {
        return x == other.x && y == other.y && width == other.width && height == other.height;
    }

    public override bool Equals(object obj) {
        return obj is Bounds other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(x, y, width, height);
    }

    public static bool operator ==(Bounds left, Bounds right) {
        return left.Equals(right);
    }

    public static bool operator !=(Bounds left, Bounds right) {
        return !left.Equals(right);
    }
}

public static class DataStructSerializer {
    public static void Write(this BinaryWriter writer, Vector2Int vec) {
        writer.Write(vec.x);
        writer.Write(vec.y);
    }

    public static Vector2Int ReadVector2Int(this BinaryReader reader) {
        return new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
    }
    
    public static void Write(this BinaryWriter writer, BitMatrix matrix) {
        writer.Write(matrix.Size);
        for (int i = 0; i < matrix.Size.x; i++) writer.Write(matrix[i]);
    }

    public static BitMatrix ReadBitMatrix(this BinaryReader reader) {
        Vector2Int size = reader.ReadVector2Int();
        uint[] field = new uint[size.x];
        for (int i = 0; i < field.Length; i++) field[i] = reader.ReadUInt32();
        return new BitMatrix(field, size);
    }
}

public class BitMatrix {
    private readonly uint[] field;
    
    public Vector2Int Size { get; }

    public BitMatrix(Vector2Int size) {
        Size = size;
        field = new uint[size.x];
    }

    public BitMatrix(int width, int height) : this(new Vector2Int(width, height)) { }

    public BitMatrix(uint[] initialField, Vector2Int size) {
        Size = size;
        field = (uint[])initialField.Clone();
    }

    public uint this[int x] {
        get => field[x];
        set => field[x] = value;
    }

    public bool this[int x, int y] {
        get => (field[x] & (1u << y)) != 0;
        set {
            if (value) field[x] |= 1u << y;
            else field[x] &= ~(1u << y);
        }
    }

    public bool CheckPatternMatch(BitMatrixPattern pattern, int x, int y) {
        for (int i = 0; i < pattern.Size.x; i++) {
            uint mask = pattern.GetMask(i);
            int columnIndex = x + i;
            uint column = (0 <= columnIndex && columnIndex < Size.x) ? field[columnIndex] : ~0u;
            column = (y >= 0) ? ~(~column >> y) : ~(~column << -y);
            if ((column & mask) != pattern.field[i]) return false;
        }

        return true;
    }

    public BitMatrix Copy() => new(field, Size);

    private bool Equals(BitMatrix other) {
        if (!Size.Equals(other.Size)) return false;
        for (int i = 0; i < field.Length; i++)
            if (field[i] != other.field[i]) return false;
        return true;
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((BitMatrix)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(field, Size);
    }
    
    public static bool operator ==(BitMatrix a, BitMatrix b) => Equals(a, b);
    
    public static bool operator !=(BitMatrix a, BitMatrix b) => !Equals(a, b);

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < Size.y; i++) {
            sb.Append('[');
            for (int j = 0; j < Size.x; j++) sb.Append(this[j, i] ? 'X' : ' ');
            sb.Append(']');
            if (i != Size.y - 1) sb.Append('\n');
        }

        return sb.ToString();
    }
}

public class BitMatrixPattern : BitMatrix {
    private readonly uint[] bitMask;
    
    public BitMatrixPattern(int[,] pattern, Vector2Int size, bool mirrored = false) : base(size) {
        bitMask = new uint[Size.x];
        
        for (int x = 0; x < Size.x; x++) {
            for (int y = 0; y < Size.y; y++) {
                if (pattern[x, y] == 0) {
                    this[x, y] = false;
                    bitMask[x] |= 1u << y;
                } else if (pattern[x, y] == 1) {
                    this[x, y] = true;
                    bitMask[x] |= 1u << y;
                } else if (pattern[x, y] == 2) {
                    // don't care
                }
            }
        }
    }

    public uint GetMask(int x) => 0 <= x && x < Size.x ? bitMask[x] : 0u;
}

public class BlockMatrix {
    private readonly Vector2Int size;
    private readonly int[,] field;

    public BlockMatrix(Vector2Int size) {
        this.size = size;
        field = new int[size.x, size.y];
    }

    public BlockMatrix(int width, int height) : this(new Vector2Int(width, height)) { }

    public BlockMatrix(int[,] initialField) {
        size = new Vector2Int(initialField.GetLength(0), initialField.GetLength(1));
        field = (int[,])initialField.Clone();
    }

    public BlockMatrix(BlockMatrix matrix) : this(matrix.field) { }

    public Vector2Int GetSize(int rotation = 0) => rotation % 2 == 0 ? size : size.Inverse();

    public int this[int x, int y, int rotation] {
        get {
            GetRotatedIndex(x, y, rotation, out int rx, out int ry);
            return field[rx, ry];
        }
        set {
            GetRotatedIndex(x, y, rotation, out int rx, out int ry);
            field[rx, ry] = value;
        }
    }

    private void GetRotatedIndex(int x, int y, int rotation, out int rx, out int ry) {
        if (rotation == 0) {
            rx = x;
            ry = y;
        } else if (rotation == 1) {
            rx = size.y - y - 1;
            ry = x;
        } else if (rotation == 2) {
            rx = size.x - x - 1;
            ry = size.y - y - 1;
        } else {
            rx = y;
            ry = size.x - x - 1;
        }
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < GetSize().y; i++) {
            sb.Append('[');
            for (int j = 0; j < GetSize().x; j++) {
                sb.Append(this[j, i, 0]);
                if (j != GetSize().x - 1) sb.Append(", ");
            }

            sb.Append(']');
            if (i != GetSize().y - 1) sb.Append('\n');
        }

        return sb.ToString();
    }
}

public readonly struct MinoState : IEquatable<MinoState> {
    public readonly int x, y, rotation;

    public MinoState(int x, int y, int rotation) {
        this.x = x;
        this.y = y;
        this.rotation = rotation;
    }

    public bool Equals(MinoState other) {
        return x == other.x && y == other.y && rotation == other.rotation;
    }

    public override bool Equals(object obj) {
        return obj is MinoState other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(x, y, rotation);
    }

    public static bool operator ==(MinoState a, MinoState b) => Equals(a, b);
    public static bool operator !=(MinoState a, MinoState b) => !Equals(a, b);

    public override string ToString() {
        return $"(x:{x}, y:{y}, rot:{rotation})";
    }
}

public readonly struct MinoPlacement {
    public readonly MinoState state;
    
}

public class GameStatistics {
    public int steps, lineClearTotal, attackTotal, rawAttackTotal, minoCountTotal, tWasted, iWasted;
    public int[] lineClearCount, tSpinCount, tSpinMiniCount;
    public int perfectCount;
    public bool dead;

    public GameStatistics() {
        lineClearCount = new int[4];
        tSpinCount = new int[3];
        tSpinMiniCount = new int[3];
    }

    public void OnStep() => steps++;

    public void OnMinoLocked(int minoType, MovementResult result) {
        minoCountTotal++;
        lineClearTotal += result.lineClear;
        attackTotal += result.attackLine;
        rawAttackTotal += result.attackLine;
        if (result.perfectCleared) rawAttackTotal -= Constants.PerfectClearBonus;

        if (result.lineClear > 0) {
            int[] targetArray = result.tSpin ? (result.tSpinMini ? tSpinMiniCount : tSpinCount) : lineClearCount;
            targetArray[result.lineClear - 1]++;
            if (result.perfectCleared) perfectCount++;
        }

        if (minoType == 0 && !result.tSpin) tWasted++;
        if (minoType == 6 && result.lineClear != 4) iWasted++;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append($"Steps: {steps}\n" +
                  $"Total Clear: {lineClearTotal} lines\n" +
                  $"Total Attack: {attackTotal} lines ({rawAttackTotal} raw)\n" +
                  $"Total Placed: {minoCountTotal}\n\n");
        
        for (var i = 0; i < lineClearCount.Length; i++) {
            sb.Append($"{i + 1} line: {lineClearCount[i]}\n");
        }
        
        for (var i = 0; i < tSpinCount.Length; i++) {
            sb.Append($"T-spin {i + 1}: {tSpinCount[i]}\n");
        }
        
        for (var i = 0; i < tSpinMiniCount.Length; i++) {
            sb.Append($"T-spin mini {i + 1}: {tSpinMiniCount[i]}\n");
        }

        sb.Append($"\nPerfect Clear: {perfectCount}\n");
        sb.Append($"\nT Wasted: {tWasted}\n");
        sb.Append($"\nI Wasted: {iWasted}");
        return sb.ToString();
    }
}
