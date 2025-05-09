using System.Collections.Generic;

namespace GameClient.Tetris.Replay;

public class ReplayData {
    public int playerCount;
    public List<List<GameStateNode>> gameStates;
}