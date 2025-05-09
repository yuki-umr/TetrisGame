using Newtonsoft.Json;

namespace GameClient.Tetris.Replay;

public class GameFieldState {
    [JsonProperty] private string[] field;

    public bool IsFilled(int x, int y, out int blockType) {
        blockType = 0;
        if (field is null || x < 0 || y < 0 || x >= field.Length || y >= field.Length) return false;
        string column = field[x];
        if (column is null || column.Length <= y) return false;
        char blockValue = column[y];
        
        return ReplayConstants.IsFieldBlock(blockValue, out blockType);
    }
}