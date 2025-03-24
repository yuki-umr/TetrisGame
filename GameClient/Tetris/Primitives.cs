using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;

namespace GameClient.Tetris; 

public static class Primitives {
    private static byte[] TtfBytes { get; set; }
    private static Dictionary<int, SpriteFont> Fonts { get; } = new();
    private static Texture2D Rectangle { get; set; }
    private static GraphicsDevice _graphicsDevice;
    public static bool DrawOutsideWindow = false;

    public static void Initialize(GraphicsDevice graphicsDevice) {
        _graphicsDevice = graphicsDevice;
        if (_graphicsDevice == null) return;
        Rectangle = new Texture2D(graphicsDevice, 1, 1);
        Rectangle.SetData(new[] { Color.White });
        TtfBytes = File.ReadAllBytes("C:\\Windows\\Fonts\\consola.ttf");
    }

    public static void Unload() {
        if (_graphicsDevice == null) return;
        Rectangle.Dispose();
    }

    public static void DrawSquare(SpriteBatch spriteBatch, int x, int y, int size, uint color, int outline = 0) {
        if (_graphicsDevice == null) return;
        DrawRectangle(spriteBatch, x, y, size, size, color, outline);
    }

    public static void DrawRectangle(SpriteBatch spriteBatch, int x, int y, int width, int height, uint color, int outline = 0) {
        if (_graphicsDevice == null) return;
        if (!DrawOutsideWindow && (x < -width || Constants.WindowWidth <= x || y < -height || Constants.WindowHeight <= y)) return;
        if (outline == 0) spriteBatch.Draw(Rectangle, new Rectangle(x, y, width, height), new Color(color));
        else {
            DrawRectangle(spriteBatch, x, y, width, outline, color);
            DrawRectangle(spriteBatch, x, y + height - outline, width, outline, color);
            DrawRectangle(spriteBatch, x, y + outline, outline, height - outline * 2, color);
            DrawRectangle(spriteBatch, x + width - outline, y + outline, outline, height - outline * 2, color);
        }
    }

    public static void DrawLine(SpriteBatch spriteBatch, int xStart, int yStart, int xEnd, int yEnd, int lineWidth, uint color) {
        float rotation = Convert.ToSingle(Math.Atan2(yEnd - yStart, xEnd - xStart));
        int lineLength = Convert.ToInt32(Vector2.Distance(new Vector2(xStart, yStart), new Vector2(xEnd, yEnd)));
        spriteBatch.Draw(Rectangle, new Rectangle(xStart, yStart, lineLength, lineWidth), null,
            new Color(color), rotation, new Vector2(0, lineWidth / 2f), SpriteEffects.None, 0);
    }

    public static void DrawText(SpriteBatch spriteBatch, string text, int x, int y, int size, uint color) {
        if (_graphicsDevice == null) return;
        DrawText(spriteBatch, text, x, y, size, new Color(color));
    }

    public static void DrawText(SpriteBatch spriteBatch, string text, int x, int y, int size, Color color) {
        if (_graphicsDevice == null) return;
        spriteBatch.DrawString(GetFont(size), text, new Vector2(x, y), color);
    }

    private static SpriteFont GetFont(int size) {
        if (!Fonts.ContainsKey(size)) {
            TtfFontBakerResult result = TtfFontBaker.Bake(TtfBytes, size, 1024, 1024, new[] {
                CharacterRange.BasicLatin,
            });
            Fonts[size] = result.CreateSpriteFont(_graphicsDevice);
        }

        return Fonts[size];
    }
}