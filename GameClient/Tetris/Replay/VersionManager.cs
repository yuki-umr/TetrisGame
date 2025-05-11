using System;
using System.Collections.Generic;

namespace GameClient.Tetris.Replay;

public static class VersionManager {
    public static void Apply(ReplayData data) {
        if (data.replayVersion < 1) {
            // ver1: fixed NEXT mino reference using the wrong range
            foreach (List<GameStateNode> nodes in data.gameStates) {
                foreach (GameStateNode node in nodes) {
                    int[] nextMinos = new int[node.nextMinos.Length - 1];
                    Array.Copy(node.nextMinos, 1, nextMinos, 0, nextMinos.Length);
                    node.nextMinos = nextMinos;
                }
            }
        }
    }
}