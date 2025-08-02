using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace GameClient.Tetris; 

public interface IEvaluator {
    public Evaluation EvaluateMove(GameState gameState, Mino mino, MinoState minoState, IEnumerable<PatternMatchData> previousPatterns = null);
    public int EvaluateField(GameField gameField, out List<PatternMatchData> patternsFound);
}
