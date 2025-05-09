using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameClient.Tetris.Replay;

public class GameStateNode {
    public GameFieldState field;
    public List<MinoRecord> minoRecords;
    public MovementResult movementResult;
    
    [JsonProperty] private int currentMino, holdMino;
    [JsonProperty] public int[] nextMinos;
    
    public bool TryGetCurrentMino(out int mino) => ReplayConstants.TryConvertMinoType(currentMino, out mino);
    
    public bool TryGetHoldMino(out int mino) => ReplayConstants.TryConvertMinoType(holdMino, out mino);
    
    public bool TryGetNextMino(int index, out int mino) {
        mino = 0;
        if (index < 0 || nextMinos.Length < index) return false;
        return ReplayConstants.TryConvertMinoType(nextMinos[index], out mino);
    }
}