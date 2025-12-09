using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Interface for drawing primitive shapes.
/// </summary>
public interface IPrimitiveDrawer
{
    Texture2D Pixel { get; }

    void Initialize(GraphicsDevice graphicsDevice);
    void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color);
    void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1);
    void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness = 2);
    void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int thickness = 1);
    void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color);
}
