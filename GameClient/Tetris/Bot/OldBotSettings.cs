using System;

namespace GameClient.Tetris; 

public class OldBotSettings {
    private static readonly int EnumCount = Enum.GetValues<Flag>().Length;
        
    public int FlagBits { get; private set; }

    public OldBotSettings(int flagBits) {
        FlagBits = flagBits;
    }

    public OldBotSettings(params Flag[] flags) {
        foreach (Flag flag in flags) FlagBits |= 1 << (int) flag;
    }

    public bool HasFlag(Flag flag) => (FlagBits & (1 << (int)flag)) != 0;

    public static int[] GetAllTestFlagPatterns(params Flag[] flags) {
        int[] bitFlags = new int[1 << flags.Length];
        for (int i = 0; i < bitFlags.Length; i++) {
            for (var j = 0; j < flags.Length; j++) {
                if ((i & (1 << j)) != 0) {
                    bitFlags[i] |= 1 << (int)flags[j];
                }
            }
        }

        return bitFlags;
    }

    public void AddFlag(Flag flag) => FlagBits |= 1 << (int)flag;
    public void RemoveFlag(Flag flag) => FlagBits &= ~(1 << (int)flag);
    
    public int BeamWidth => HasFlag(Flag.BeamWidth12) ? 12 : 8;
    public int BeamDepth => HasFlag(Flag.BeamDepth12) ? 12 : HasFlag(Flag.BeamDepth8) ? 8 : 5;

    public override string ToString() {
        return Convert.ToString(FlagBits, 2).PadLeft(EnumCount, '0');
    }

    public static OldBotSettings GetDefault() => new(Flag.FixedWellHeight, Flag.BeamWidth12, Flag.BeamDepth12, Flag.TsdWeightModV1);

    public enum Flag {
        NoTsd,
        NoTst,
        NoPatternCheck,
        NoWallWellPenalty,
        NoInnerWellBonus,
        FixedWellHeight,
        CheckTWaste,
        WallWell2X,
        BeamWidth12,
        BeamDepth8,
        BeamDepth12,
        WallWellCcBased,
        NoPreTsd,
        TsdWeightModV1
    }
}