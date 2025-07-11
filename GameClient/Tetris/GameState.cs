using System;
using System.IO;

namespace GameClient.Tetris;

public class GameState : IStateSerializable, IEquatable<GameState> {
    public GameField Field { get; private init; }
    private MinoBag minoBag;

    public int CurrentMino { get; private set; }
    public int HoldingMino { get; private set; }
    public int RenCount { get; private set; }
    public bool BackToBack { get; private set; }

    public GameState(GameField defaultField = null, int defaultMino = -1, int holdMino = -1, MinoBag minoBag = default,
        bool backToBack = false, int renCount = -1, int bagSeed = -1) {
        // Clone class types
        Field = defaultField ?? new GameField();
        this.minoBag = minoBag.Count == 0 ? bagSeed == -1 ? new MinoBag() : new MinoBag(bagSeed) : minoBag;

        // Copy primitive types
        CurrentMino = defaultMino;
        HoldingMino = holdMino;
        RenCount = renCount;
        BackToBack = backToBack;

        if (CurrentMino == -1) SetNextMino();
    }

    public int PeekNextMino(int next) => minoBag[next];

    private void SetNextMino() {
        CurrentMino = minoBag.Pop();
    }

    public void HoldMino() {
        if (HoldingMino != -1) {
            (HoldingMino, CurrentMino) = (CurrentMino, HoldingMino);
        } else {
            HoldingMino = CurrentMino;
            SetNextMino();
        }
    }

    public int PeekMinoAfterHold() {
        if (HoldingMino != -1) return HoldingMino;
        return minoBag.Next;
    }

    public MovementResult LockMino(bool lastSpin, bool lastSrs4, MinoState state) {
        MovementResult result = new MovementResult();
        Mino mino = new Mino(CurrentMino, state.rotation);

        Field.PlaceMino(mino, state.x, state.y);
        
        // line clear
        int lineClear = Field.CheckClearLine();
        result.lineClear = lineClear;

        if (lineClear == 0) {
            RenCount = 0;
        } else {
            RenCount++;
            result.attackLine = CalculateRenBonus(RenCount);
            int backToBackBonus = BackToBack ? Constants.BackToBackEnabled ? Constants.BackToBackBonus : 0 : 0;

            // calculate attack
            if (CurrentMino == 0) {
                // check t spin
                bool tSpin = true, mini = true;
                // last move has to be SPIN
                if (!lastSpin) tSpin = false;
                // more than 3 corners has to be filled
                int cornerFillCount = 0;
                for (int i = 0; i < 4; i++) {
                    if (!CornerColliding(Field, state, i)) cornerFillCount++;
                    if (cornerFillCount != 2) continue;
                    tSpin = false;
                    break;
                }
                // both of front corners has to be filled to be a full t spin
                Span<int> leftCorner = stackalloc int[] { 0, 2, 3, 1 }, rightCorner = stackalloc int[] { 2, 3, 1, 0 };
                if (CornerColliding(Field, state, leftCorner[state.rotation]) && CornerColliding(Field, state, rightCorner[state.rotation]))
                    mini = false;
                // if srs pattern == 4 -> not mini
                if (lastSrs4) mini = false;

                (result.tSpin, result.tSpinMini) = (tSpin, mini);
                if (tSpin) {
                    BackToBack = true;
                    if (mini) {
                        result.attackLine += backToBackBonus + lineClear - 1;
                    } else {
                        result.attackLine += backToBackBonus + lineClear * 2;
                    }
                } else {
                    // not t spin
                    BackToBack = false;
                    result.attackLine += lineClear + Constants.SmallLinePenalty;
                }
            } else {
                // not t mino
                if (lineClear < 4) {
                    BackToBack = false;
                    result.attackLine += lineClear + Constants.SmallLinePenalty;
                } else {
                    BackToBack = true;
                    result.attackLine += backToBackBonus + lineClear;
                }
            }
            
            // add perfect clear bonus
            if (Field.IsPerfectCleared && Constants.PerfectBonusEnabled) {
                result.perfectCleared = true;
                result.attackLine += Constants.PerfectClearBonus;
                BackToBack = true;
            }
        }
        
        SetNextMino();
        return result;
        
        // function to check if the corner of T mino is blocked (used for t spin mini checks)
        static bool CornerColliding(GameField field, MinoState state, int i) => field[state.x + ((i & 0b10) >> 1) * 2, state.y - (i & 0b01) * 2];
    }

    public GameState Copy() {
        GameField fieldCopy = Field.Copy();
        return new GameState(fieldCopy, CurrentMino, HoldingMino, minoBag, BackToBack, RenCount);
    }

    private static int CalculateRenBonus(int renCount) {
        return 0;
        // throw new NotImplementedException();
    }

    public bool Equals(GameState other) {
        return Equals(Field, other.Field) && Equals(minoBag, other.minoBag) && CurrentMino == other.CurrentMino
               && HoldingMino == other.HoldingMino && RenCount == other.RenCount && BackToBack == other.BackToBack;
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((GameState)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Field, minoBag, CurrentMino, HoldingMino, RenCount, BackToBack);
    }
    
    public static bool operator ==(GameState a, GameState b) => Equals(a, b);
    
    public static bool operator !=(GameState a, GameState b) => !Equals(a, b);

    public string SerializeToString() {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        Serialize(writer);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static GameState DeserializeFromString(string serializedString) {
        byte[] bytes = Convert.FromBase64String(serializedString);
        using MemoryStream stream = new MemoryStream(bytes);
        using BinaryReader reader = new BinaryReader(stream);
        return Deserialize(reader);
    }
    
    public void Serialize(BinaryWriter writer) {
        Field.Serialize(writer);
        minoBag.Serialize(writer);
        writer.Write(CurrentMino);
        writer.Write(HoldingMino);
        writer.Write(RenCount);
        writer.Write(BackToBack);
    }

    public static GameState Deserialize(BinaryReader reader) {
        GameField field = GameField.Deserialize(reader);
        MinoBag minoBag = MinoBag.Deserialize(reader);
        int currentMino = reader.ReadInt32();
        int holdingMino = reader.ReadInt32();
        int renCount = reader.ReadInt32();
        bool backToBack = reader.ReadBoolean();
        return new GameState(field, currentMino, holdingMino, minoBag, backToBack, renCount);
    }
}

public struct MovementResult {
    public int attackLine, lineClear;
    public bool tSpin, tSpinMini, perfectCleared;
}
