using System;
using CPUgame.Core.Primitives;
using CPUgame.Rendering.Abstractions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// MonoGame implementation of IDrawContext.
/// Wraps SpriteBatch operations for platform-agnostic rendering.
/// </summary>
public class MonoGameDrawContext : IDrawContext
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;

    public MonoGameDrawContext(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _font = font;
    }

    public void FillRect(Rect rect, ColorRgba color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(rect.X, rect.Y, rect.Width, rect.Height),
            ToColor(color));
    }

    public void DrawRect(Rect rect, ColorRgba color, int thickness = 1)
    {
        var c = ToColor(color);

        // Top
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), c);
        // Bottom
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), c);
        // Left
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), c);
        // Right
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), c);
    }

    public void DrawLine(Vector2f start, Vector2f end, ColorRgba color, int thickness = 2)
    {
        var startVec = new Vector2(start.X, start.Y);
        var endVec = new Vector2(end.X, end.Y);
        var edge = endVec - startVec;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
            null,
            ToColor(color),
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0);
    }

    public void FillCircle(Vector2f center, float radius, ColorRgba color)
    {
        // Approximate circle with filled rectangles (simple approach)
        var c = ToColor(color);
        int r = (int)radius;

        for (int y = -r; y <= r; y++)
        {
            int halfWidth = (int)Math.Sqrt(r * r - y * y);
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)center.X - halfWidth, (int)center.Y + y, halfWidth * 2, 1),
                c);
        }
    }

    public void DrawCircle(Vector2f center, float radius, ColorRgba color, int thickness = 1)
    {
        // Draw circle outline using line segments
        var c = ToColor(color);
        const int segments = 32;
        float angleStep = MathF.PI * 2 / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            var p1 = new Vector2f(
                center.X + MathF.Cos(angle1) * radius,
                center.Y + MathF.Sin(angle1) * radius);
            var p2 = new Vector2f(
                center.X + MathF.Cos(angle2) * radius,
                center.Y + MathF.Sin(angle2) * radius);

            DrawLine(p1, p2, color, thickness);
        }
    }

    public void DrawText(string text, Vector2f position, ColorRgba color, float scale = 1f)
    {
        _spriteBatch.DrawString(_font, text,
            new Vector2(position.X, position.Y),
            ToColor(color),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public void DrawTextCentered(string text, Vector2f center, ColorRgba color, float scale = 1f)
    {
        var size = _font.MeasureString(text) * scale;
        var position = new Vector2(center.X - size.X / 2, center.Y - size.Y / 2);
        _spriteBatch.DrawString(_font, text, position, ToColor(color),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public Vector2f MeasureText(string text, float scale = 1f)
    {
        var size = _font.MeasureString(text) * scale;
        return new Vector2f(size.X, size.Y);
    }

    private static Color ToColor(ColorRgba c) => new(c.R, c.G, c.B, c.A);
}
