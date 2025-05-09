using System.Collections.Generic;

namespace GameClient.Tetris.Replay;

public static class ReplayConstants {
    public const char MinoTypeS = 'S';
    public const char MinoTypeZ = 'Z';
    public const char MinoTypeJ = 'J';
    public const char MinoTypeL = 'L';
    public const char MinoTypeT = 'T';
    public const char MinoTypeO = 'O';
    public const char MinoTypeI = 'I';
    public const char MinoTypeGarbage = 'G';
    public const char MinoTypeLineDelete = 'X';
    public const char MinoTypeNull = '-';
    public const char MinoTypeUnknown = '?';
    
    // replay mino type: -SZJLTOI?
    // custom mino type: TSZLJOI

    private static readonly Dictionary<char, int> MinoCharacterBlock = new() {
        { MinoTypeT, 0 },
        { MinoTypeS, 1 },
        { MinoTypeZ, 2 },
        { MinoTypeL, 3 },
        { MinoTypeJ, 4 },
        { MinoTypeO, 5 },
        { MinoTypeI, 6 },
        { MinoTypeGarbage, 7 },
        { MinoTypeUnknown, 9 },
    };
    
    private static readonly Dictionary<int, int> MinoTypeConversion = new() {
        { 1, 1 },
        { 2, 2 },
        { 3, 4 },
        { 4, 3 },
        { 5, 0 },
        { 6, 5 },
        { 7, 6 },
    };
    
    public static bool TryConvertMinoType(int replayType, out int gameType) => MinoTypeConversion.TryGetValue(replayType, out gameType);
    
    public static bool IsFieldBlock(char fieldValue, out int minoType) => MinoCharacterBlock.TryGetValue(fieldValue, out minoType);
}