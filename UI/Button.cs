using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

public class Button
{
    public Rectangle Bounds { get; set; }
    public string Text { get; set; }
    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    public Action? OnClick { get; set; }

    private static readonly Color NormalColor = new(70, 70, 80);
    private static readonly Color HoverColor = new(90, 90, 100);
    private static readonly Color PressedColor = new(50, 50, 60);
    private static readonly Color BorderColor = new(100, 100, 120);
    private static readonly Color TextColor = new(220, 220, 230);

    public Button(Rectangle bounds, string text)
    {
        Bounds = bounds;
        Text = text;
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed)
    {
        IsHovered = Bounds.Contains(mousePos);

        if (IsHovered && mouseJustPressed)
        {
            IsPressed = true;
        }

        if (IsPressed && !mousePressed)
        {
            if (IsHovered)
            {
                OnClick?.Invoke();
            }
            IsPressed = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        var color = IsPressed ? PressedColor : (IsHovered ? HoverColor : NormalColor);

        // Background
        spriteBatch.Draw(pixel, Bounds, color);

        // Border
        spriteBatch.Draw(pixel, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 2), BorderColor);
        spriteBatch.Draw(pixel, new Rectangle(Bounds.X, Bounds.Bottom - 2, Bounds.Width, 2), BorderColor);
        spriteBatch.Draw(pixel, new Rectangle(Bounds.X, Bounds.Y, 2, Bounds.Height), BorderColor);
        spriteBatch.Draw(pixel, new Rectangle(Bounds.Right - 2, Bounds.Y, 2, Bounds.Height), BorderColor);

        // Text
        var textSize = font.MeasureString(Text);
        var textPos = new Vector2(
            Bounds.X + (Bounds.Width - textSize.X) / 2,
            Bounds.Y + (Bounds.Height - textSize.Y) / 2);
        font.DrawText(spriteBatch, Text, textPos, TextColor);
    }
}
