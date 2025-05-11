using System.Collections.Generic;

namespace GameClient.Tetris.Replay;

public class ReplayData {
    public int replayVersion;
    public int playerCount;
    public List<List<GameStateNode>> gameStates;
}