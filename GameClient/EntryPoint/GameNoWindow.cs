using GameClient.Tetris;

namespace GameClient.EntryPoint;

public class GameNoWindow {
    public GameNoWindow() {
        WindowManager manager = (WindowManager)Program.GetOptions().GetSubClassInstance();
        Initialize(manager);
    }
    
    public GameNoWindow(WindowManager manager) {
        Initialize(manager);
    }

    private static void Initialize(WindowManager manager) {
        manager.Initialize();
        Primitives.Initialize(null);
        manager.LoadContent(null);
        while (manager.Update() != -1) { }
        Primitives.Unload();
    }
}