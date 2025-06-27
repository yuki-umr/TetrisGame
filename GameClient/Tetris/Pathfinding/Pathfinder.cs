using System.Collections.Generic;

namespace GameClient.Tetris.Pathfinding;

public abstract class Pathfinder {
    public static Pathfinder Instance { get; set; } = new PathfinderBFS();

    public abstract List<MinoPlacement> ListAllPossiblePlacements(int minoType, GameField field, bool useHold);

}