using System.IO;

namespace GameClient.Tetris; 

public class StateWriter {
    
}

public class StateReader {
    
}

public interface IStateSerializable {
    public void Serialize(BinaryWriter writer);
}