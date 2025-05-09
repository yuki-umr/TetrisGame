namespace GameClient.Tetris.Replay;

public struct MovementResult {
    public int resultType, lineClearCount, renCount;
    public bool isTSpin, isMini, isPerfectClear, isBackToBack;
}