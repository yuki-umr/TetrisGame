using System.Collections.Generic;
using System.Text;
using GameClient.Tetris.Input;

namespace GameClient.Tetris.Pathfinding; 

public class MinoRoute {
    private readonly List<InputKey> inputOrder;
    private readonly InputKey lastInput;
    private readonly int lastSrs;
    private readonly bool useHold;

    public MinoRoute(List<InputKey> inputOrder, InputKey lastInput, int lastSrs, bool useHold) {
        this.inputOrder = inputOrder;
        this.lastInput = lastInput;
        this.lastSrs = lastSrs;
        this.useHold = useHold;
    }

    public bool HasRoute => inputOrder.Count > 0;

    public bool IsLastSpin => lastInput is InputKey.SpinClock or InputKey.SpinCounterClock;

    public bool IsLastSrs4 => lastSrs == 4;

    public int GetLength(ref Cursor cursor) => inputOrder.Count - cursor.position + (cursor.holdQueued ? 1 : 0);

    public InputKey PopKey(ref Cursor cursor) {
        if (cursor.holdQueued) {
            cursor.holdQueued = false;
            return InputKey.Hold;
        }
        
        if (cursor.position >= inputOrder.Count) return InputKey.None;
        InputKey nextKey = inputOrder[cursor.position];
        cursor.position++;
        return nextKey;
    }

    public static MinoRoute GetDefault() {
        return new MinoRoute(new List<InputKey>(), InputKey.None, -1, false);
    }

    public Cursor CreateCursor() => new() { position = 0, holdQueued = useHold };

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < inputOrder.Count; i++) {
            sb.Append(i == -1 ? InputKey.Hold : inputOrder[i]);
            if (i != inputOrder.Count - 1) sb.Append(", ");
        }

        sb.Append(']');
        return sb.ToString();
    }

    public struct Cursor {
        public int position;
        public bool holdQueued;
    }
}