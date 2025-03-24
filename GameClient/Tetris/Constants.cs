using Microsoft.Xna.Framework;

namespace GameClient.Tetris; 

public static class Constants {
    public const int WindowWidth = 1280, WindowHeight = 720;
    
    public const int GameFieldWidth = 10, GameFieldHeight = 20, GameFieldOutside = 12;
    public const int GameFieldTotalHeight = GameFieldHeight + GameFieldOutside;
    public const int MinoCount = 7;
    public static readonly Vector2Int DefaultGameFieldSize = new(GameFieldWidth, GameFieldTotalHeight);

    public const bool PerfectBonusEnabled = false, BackToBackEnabled = false;
    public const int BackToBackBonus = 1, PerfectClearBonus = 10;
    public const int SmallLinePenalty = -1;

    public const int DrawFieldOffsetX = 128, DrawFieldOffsetY = 16, DrawFieldOffsetShift = 640, DrawBlockSize = 32;
    public const int DrawHoldOffsetX = 32, DrawHoldOffsetY = 136, DrawHoldBlockSize = 24;
    public const int DrawNextOffsetX = 504, DrawNextOffsetY = 180, DrawNextSpace = 84, DrawNextBlockSize = 24, DrawNextCount = 5;

    public const int FallTime = 60, LockTime = 30, SoftDropSpeed = 20;
    public const int MinoStartX = 3, MinoStartY = 18;
    public static readonly Vector2Int MinoSpawnPosition = new(MinoStartX, MinoStartY);

    public const int AutoRepeatRate = 2, DelayedAutoShift = 10, SoftDropFactor = 20;

    public const int GarbageBlock = 7;

    public static readonly uint[] MinoColorCodes = {
        0xff974582U,
        0xff5da52aU,
        0xff3c4bdaU,
        0xff237adcU,
        0xffab6e24U,
        0xff22bde7U,
        0xffca8f34U,
        0xff888888U
    };

    public const uint BackgroundColor = 0xff222222U;

    public const string PatternFolder = "Patterns/";
}