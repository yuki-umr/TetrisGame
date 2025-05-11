using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public class ColoredGameField : GameField {
    private readonly BlockMatrix fieldColor;

    public ColoredGameField(BlockMatrix fieldColor = null, BitMatrix initialField = null) : base(initialField) {
        if (fieldColor != null) this.fieldColor = fieldColor;
        else {
            this.fieldColor = new BlockMatrix(Size);
            for (int x = 0; x < Size.x; x++) {
                for (int y = 0; y < Size.y; y++) {
                    if (!this[x, y]) continue;
                    this.fieldColor[x, y, 0] = Constants.GarbageBlock;
                }
            }
        }
    }

    protected override void OnLineClear(List<int> nonCleared) {
        for (int y = 0; y < FieldHeight; y++) {
            for (int x = 0; x < Size.x; x++) {
                fieldColor[x, y, 0] = y < nonCleared.Count ? fieldColor[x, nonCleared[y], 0] : 0;
            }
        }
    }

    public override void SetBlock(int x, int y, int block) {
        base.SetBlock(x, y, block);
        fieldColor[x, y, 0] = block;
    }

    public void Draw(SpriteBatch spriteBatch, int xOffset, int yOffset, int blockSize, bool ignoreOutside = true, bool drawHeight = false) {
        int drawHeightRange = ignoreOutside ? Constants.GameFieldHeight : Size.y;
        Primitives.DrawRectangle(spriteBatch, xOffset, yOffset, blockSize * Size.x, blockSize * drawHeightRange, Constants.BackgroundColor);
        for (int x = 0; x < Size.x; x++) {
            for (int y = 0; y < drawHeightRange; y++) {
                if (!this[x, y]) continue;
                int dx = xOffset + x * blockSize, dy = yOffset + (drawHeightRange - y - 1) * blockSize;
                uint minoColor = Constants.MinoColorCodes[fieldColor[x, y, 0]];
                Primitives.DrawRectangle(spriteBatch, dx, dy, blockSize, blockSize, minoColor);
            }
        }

        if (drawHeight) {
            for (int x = 0; x < Size.x; x++) {
                int y = ColumnHeight(x);
                int dx = xOffset + x * blockSize, dy = yOffset + (drawHeightRange - y) * blockSize - 2;
                Primitives.DrawRectangle(spriteBatch, dx, dy, blockSize, 2, 0xff0000ff);
            }
        }
    }
}