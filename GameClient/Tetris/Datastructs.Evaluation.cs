using System;
using System.Collections.Generic;

namespace GameClient.Tetris;

public readonly struct Evaluation : IComparable<Evaluation> {
    public readonly int field, movement;
    public readonly MovementResult result;
    public readonly GameState gameStateAfterMove;
    public readonly List<PatternMatchData> patternsFound;

    public int Value => field + movement;

    public Evaluation(int field, int movement, MovementResult result, GameState gameStateAfterMove, List<PatternMatchData> patternsFound) {
        this.field = field;
        this.movement = movement;
        this.result = result;
        this.gameStateAfterMove = gameStateAfterMove;
        this.patternsFound = patternsFound;
    }

    public int CompareTo(Evaluation other) {
        // why not just compare the value?
        // int fieldComparison = field.CompareTo(other.field);
        // if (fieldComparison != 0) return fieldComparison;
        // return movement.CompareTo(other.movement);
        return Value.CompareTo(other.Value);
    }
    
    
    public static bool operator <(Evaluation left, Evaluation right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Evaluation left, Evaluation right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Evaluation left, Evaluation right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Evaluation left, Evaluation right) {
        return left.CompareTo(right) >= 0;
    }

    public override string ToString() {
        return Value.ToString();
    }
}