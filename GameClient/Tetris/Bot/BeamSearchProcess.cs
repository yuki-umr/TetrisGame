using System.Collections.Generic;

namespace GameClient.Tetris;

public class BeamSearchProcess : SearchProcess {
    public List<BeamNode>[] NodesTree { get; }

    public BeamSearchProcess(int nextSeek) {
        NodesTree = new List<BeamNode>[nextSeek + 1];

        for (int i = 0; i <= nextSeek; i++) {
            NodesTree[i] = new List<BeamNode>();
        }
    }
    
    public void AddNode(int depth, BeamNode node) {
        if (depth < 0 || depth >= NodesTree.Length) {
            throw new System.ArgumentOutOfRangeException(nameof(depth), "Depth is out of range.");
        }
        
        NodesTree[depth].Add(node);
    }
}