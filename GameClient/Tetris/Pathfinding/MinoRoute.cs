using System.Collections.Generic;
using System.Text;
using GameClient.Tetris.Input;

namespace GameClient.Tetris.Pathfinding; 

public class MinoRoute {
    private List<InputKey> inputOrder;
    private InputKey lastInput;
    private int lastSrs;
    private bool useHold;
    private int cursor;

    public MinoRoute(List<InputKey> inputOrder, InputKey lastInput, int lastSrs) {
        this.inputOrder = inputOrder;
        this.lastInput = lastInput;
        this.lastSrs = lastSrs;
    }

    public bool HasRoute => inputOrder.Count > 0;

    public bool IsLastSpin => lastInput is InputKey.SpinClock or InputKey.SpinCounterClock;

    public bool IsLastSrs4 => lastSrs == 4;

    public int Length => inputOrder.Count - cursor;

    public InputKey PopKey() {
        if (cursor >= inputOrder.Count) return InputKey.None;
        InputKey nextKey = cursor == -1 ? InputKey.Hold : inputOrder[cursor];
        cursor++;
        return nextKey;
    }

    public void SetUseHold() {
        if (useHold) return;
        useHold = true;
        cursor = -1;
    }

    public static MinoRoute GetDefault() {
        return new MinoRoute(new List<InputKey>(), InputKey.None, -1);
    }

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
}