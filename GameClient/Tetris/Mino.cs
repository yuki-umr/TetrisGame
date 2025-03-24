using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public readonly struct Mino {
    public readonly int type;
    public readonly int rotation;

    public Vector2Int Size => type > 4 ? new Vector2Int(4, 4) : new Vector2Int(3, 3);
    public Vector2Int[] BlockPos => AllBlockPos[type, rotation];

    public Mino(int type, int rotation) {
        this.type = type;
        this.rotation = rotation;
    }

    private bool IsEmpty(int x, int y) {
        foreach (Vector2Int blockPos in BlockPos) {
            if (blockPos.x == x && blockPos.y == y) return false;
        }

        return true;
    }

    public Mino Rotated(bool clockwise) {
        return new Mino(type, ((rotation + (clockwise ? 1 : -1)) % 4 + 4) % 4);
    }

    public Mino Rotated(int rotation) {
        return new Mino(type, rotation);
    }

    public int RotationAfterSpin(bool clockwise, int fromRotation = -1) {
        int rot = fromRotation != -1 ? fromRotation : rotation;
        return ((rot + (clockwise ? 1 : -1)) % 4 + 4) % 4;
    }

    // TODO this can be static
    public Vector2Int WallKickDelta(bool clockwise, int attempt, int fromRotation = -1) {
        if (attempt == 0) return Vector2Int.Zero;
        int rot = fromRotation != -1 ? fromRotation : rotation;
        int lastRotation = RotationAfterSpin(!clockwise, rot);
        if (Size.x == 3) {
            int hShift = rot % 2 == 0 ? (lastRotation == 1 ? 1 : -1) : (rot == 1 ? -1 : 1);
            int vShift = rot % 2 == 0 ? -1 : 1;
            return attempt switch {
                1 => new Vector2Int(hShift, 0),
                2 => new Vector2Int(hShift, vShift),
                3 => new Vector2Int(0, -vShift * 2),
                4 => new Vector2Int(hShift, -vShift * 2),
                _ => throw new ArgumentOutOfRangeException(nameof(attempt), attempt, $"SRS pattern {attempt} is not defined (Size {Size})")
            };
        }

        if (Size.x == 4) {
            int previousSide = (clockwise && rot == 2) || (!clockwise && rot == 0) ? 1 : 0;
            int hShift = rot % 2 == 0 ? (clockwise ? -1 : 1) : (rot == 1 ? 1 : -1);
            int vShift = rot % 2 == 0 ? (previousSide == 1 ? 1 : -1) : (rot == 1 ? -1 : 1);
            int v;
            switch (attempt) {
                case 1:
                    return new Vector2Int(hShift * (rot == 0 ? 2 : 1), 0);
                case 2:
                    return new Vector2Int(-hShift * (rot == 2 ? 2 : 1), 0);
                case 3:
                    v = vShift * (clockwise ? 1 : 2);
                    return previousSide == 0
                        ? new Vector2Int(-hShift * (rot == 2 ? 2 : 1), v)
                        : new Vector2Int(hShift * (rot == 0 ? 2 : 1), v);
                case 4:
                    v = -vShift * (clockwise ? 2 : 1);
                    return previousSide == 0
                        ? new Vector2Int(hShift * (rot == 0 ? 2 : 1), v)
                        : new Vector2Int(-hShift * (rot == 2 ? 2 : 1), v);
                default:
                    throw new ArgumentOutOfRangeException(nameof(attempt), attempt, $"SRS pattern {attempt} is not defined (Size {Size})");
            }
        }
        
        return Vector2Int.Zero;
    }
    
    public void Draw(SpriteBatch spriteBatch, int xOffset, int yOffset, int blockSize, int outline = 0, uint overrideColor = 0) {
        uint minoColor = overrideColor == 0 ? Constants.MinoColorCodes[type] : overrideColor;
        if (outline == 0) {
            foreach (Vector2Int pos in BlockPos) {
                int dx = xOffset + pos.x * blockSize, dy = yOffset - pos.y * blockSize;
                Primitives.DrawSquare(spriteBatch, dx, dy, blockSize, minoColor);
            }
        } else {
            for (int x = 0; x < Size.x; x++) {
                for (int y = 0; y < Size.y; y++) {
                    // upper left position of block
                    int dx = xOffset + x * blockSize, dy = yOffset - y * blockSize;
                    if (IsEmpty(x, y)) continue;
                    if (IsEmpty(x - 1, y))
                        Primitives.DrawRectangle(spriteBatch, dx, dy, outline, blockSize, minoColor);
                    if (IsEmpty(x + 1, y))
                        Primitives.DrawRectangle(spriteBatch, dx + blockSize - outline, dy, outline, blockSize, minoColor);
                    if (IsEmpty(x, y - 1))
                        Primitives.DrawRectangle(spriteBatch, dx, dy + blockSize - outline, blockSize, outline, minoColor);
                    if (IsEmpty(x, y + 1))
                        Primitives.DrawRectangle(spriteBatch, dx, dy, blockSize, outline, minoColor);
                    if (IsEmpty(x - 1, y - 1))
                        Primitives.DrawRectangle(spriteBatch, dx, dy + blockSize - outline, outline, outline, minoColor);
                    if (IsEmpty(x + 1, y - 1))
                        Primitives.DrawRectangle(spriteBatch, dx + blockSize - outline, dy + blockSize - outline, outline, outline, minoColor);
                    if (IsEmpty(x - 1, y + 1))
                        Primitives.DrawRectangle(spriteBatch, dx, dy, outline, outline, minoColor);
                    if (IsEmpty(x + 1, y + 1))
                        Primitives.DrawRectangle(spriteBatch, dx + blockSize - outline, dy, outline, outline, minoColor);
                }
            }
        }
    }

    public override string ToString() {
        return BlockPos.ToString();
    }

    private static readonly Vector2Int[,][] AllBlockPos = GenerateBlockPos();

    public static readonly int[] RotatedVariations = { 4, 2, 2, 4, 4, 1, 2 };

    private static Vector2Int[,][] GenerateBlockPos() {
        Vector2Int[,][] blockPos = {
            {   // T
                new Vector2Int[] { new(0, 1), new(1, 1), new(1, 2), new(2, 1) }, null, null, null
            },
            {   // S
                new Vector2Int[] { new(0, 1), new(1, 1), new(1, 2), new(2, 2) }, null, null, null
            },
            {   // Z
                new Vector2Int[] { new(0, 2), new(1, 1), new(1, 2), new(2, 1) }, null, null, null
            },
            {   // L
                new Vector2Int[] { new(0, 1), new(1, 1), new(2, 1), new(2, 2) }, null, null, null
            },
            {   // J
                new Vector2Int[] { new(0, 1), new(0, 2), new(1, 1), new(2, 1) }, null, null, null
            },
            {   // O
                new Vector2Int[] { new(1, 1), new(1, 2), new(2, 1), new(2, 2) }, null, null, null
            },
            {   // I
                new Vector2Int[] { new(0, 2), new(1, 2), new(2, 2), new(3, 2) }, null, null, null
            }
        };
        
        Vector2Int GetRotatedIndex(Vector2Int pos, int rotation, int size) {
            return rotation switch {
                1 => new Vector2Int(pos.y, size - pos.x - 1),
                2 => new Vector2Int(size - pos.x - 1, size - pos.y - 1),
                3 => new Vector2Int(size - pos.y - 1, pos.x),
                _ => pos
            };
        }
        
        for (int type = 0; type < blockPos.GetLength(0); type++) {
            int size = type > 4 ? 4 : 3;
            for (int rot = 1; rot < 4; rot++) {
                blockPos[type, rot] = blockPos[type, 0].Select(pos => GetRotatedIndex(pos, rot, size)).ToArray();
            }
        }

        return blockPos;
    }
}