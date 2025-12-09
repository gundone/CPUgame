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
    private Texture2D? _circleTexture;
    private const int CircleTextureSize = 64;

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
        CreateCircleTexture();
    }

    private void CreateCircleTexture()
    {
        // Create anti-aliased circle texture for smooth scaling with LinearClamp
        _circleTexture = new Texture2D(_graphicsDevice, CircleTextureSize, CircleTextureSize);
        var data = new Color[CircleTextureSize * CircleTextureSize];
        float center = CircleTextureSize / 2f;
        float radius = center - 1.5f;

        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distance = MathF.Sqrt(dx * dx + dy * dy);

                // Smooth anti-aliased edge
                float alpha = Math.Clamp(radius - distance + 1f, 0f, 1f);
                byte a = (byte)(alpha * 255);
                data[y * CircleTextureSize + x] = new Color(a, a, a, a); // Premultiplied alpha
            }
        }
        _circleTexture.SetData(data);
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

    public void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 0)
    {
        // Auto-calculate segments based on radius for smooth circles
        // More segments for larger circles (when zoomed in)
        if (segments <= 0)
        {
            segments = Math.Max(16, (int)(radius * 2));
        }

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
        // Use pre-rendered circle texture for smooth anti-aliased circles
        if (_circleTexture != null)
        {
            float diameter = radius * 2;
            float scale = diameter / CircleTextureSize;
            spriteBatch.Draw(
                _circleTexture,
                center,
                null,
                color,
                0f,
                new Vector2(CircleTextureSize / 2f, CircleTextureSize / 2f), // Center origin
                scale,
                SpriteEffects.None,
                0f);
        }
    }
}
