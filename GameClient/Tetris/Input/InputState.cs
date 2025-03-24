namespace GameClient.Tetris.Input; 

public struct InputState {
    private int inputFlags, previousFlags;

    public void SetState(InputKey key, bool state) {
        if (state) inputFlags |= 1 << (byte)key;
        else inputFlags &= ~(1 << (byte)key);
    }

    public void ResetState(bool resetAll = false) {
        previousFlags = resetAll ? 0 : inputFlags;
        inputFlags = 0;
    }

    public bool IsDown(InputKey key) => (inputFlags & (1 << (byte)key)) != 0;
    public bool StateChanged(InputKey key) => ((inputFlags ^ previousFlags) & (1 << (byte)key)) != 0;
    public bool IsPressed(InputKey key) => StateChanged(key) && IsDown(key);
    public bool IsReleased(InputKey key) => StateChanged(key) && !IsDown(key);
}

public enum InputKey {
    None, Left, Right, SoftDrop, HardDrop, HardGround,
    SpinClock, SpinCounterClock, Hold, Pause
}