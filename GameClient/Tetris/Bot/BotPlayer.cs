using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Tetris; 

public abstract class BotPlayer {
    public abstract void Update();
    public abstract void Draw(SpriteBatch spriteBatch);
    public abstract string SearchSpeed { get; }
}