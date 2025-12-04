using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Helper class for drawing primitive shapes without textures
/// </summary>
public class PrimitiveDrawer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private Texture2D? _pixel;

    public PrimitiveDrawer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(
                0, graphicsDevice.Viewport.Width,
                graphicsDevice.Viewport.Height, 0,
                0, 1)
        };
    }

    public Texture2D Pixel
    {
        get
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(_graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            return _pixel;
        }
    }

    public void UpdateProjection()
    {
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height, 0,
            0, 1);
    }

    public void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        spriteBatch.Draw(Pixel, rect, color);
    }

    public void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
    {
        // Top
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness = 2)
    {
        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        spriteBatch.Draw(Pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)distance, thickness),
            null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
    }

    public void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 16)
    {
        var vertices = new VertexPositionColor[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)(i * 2 * Math.PI / segments);
            vertices[i] = new VertexPositionColor(
                new Vector3(center.X + radius * MathF.Cos(angle), center.Y + radius * MathF.Sin(angle), 0),
                color);
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, segments);
        }
    }

    public void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        // Simple filled circle using rectangles
        int r = (int)radius;
        for (int y = -r; y <= r; y++)
        {
            int halfWidth = (int)Math.Sqrt(r * r - y * y);
            spriteBatch.Draw(Pixel,
                new Rectangle((int)center.X - halfWidth, (int)center.Y + y, halfWidth * 2, 1),
                color);
        }
    }
}
