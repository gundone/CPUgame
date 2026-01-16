using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Generic yes/no confirmation dialog
/// </summary>
public class ConfirmationDialog
{
    public bool IsVisible { get; private set; }

    private string _title = "";
    private string _message = "";
    private bool _yesButtonHovered;
    private bool _noButtonHovered;

    private const int DialogWidth = 400;
    private const int DialogHeight = 180;
    private const int TitleHeight = 32;
    private const int ButtonHeight = 32;
    private const int ButtonWidth = 80;
    private const int Padding = 16;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color ButtonNoColor = new(100, 60, 60);
    private static readonly Color ButtonNoHoverColor = new(140, 80, 80);

    private Rectangle _bounds;
    private Rectangle _yesButtonRect;
    private Rectangle _noButtonRect;

    public event Action? OnYes;
    public event Action? OnNo;

    public void Show(string title, string message)
    {
        IsVisible = true;
        _title = title;
        _message = message;
        _yesButtonHovered = false;
        _noButtonHovered = false;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Calculate bounds
        int x = (screenWidth - DialogWidth) / 2;
        int y = (screenHeight - DialogHeight) / 2;
        _bounds = new Rectangle(x, y, DialogWidth, DialogHeight);

        // Calculate button positions
        int buttonY = y + DialogHeight - ButtonHeight - Padding;
        int buttonsWidth = ButtonWidth * 2 + Padding;
        int buttonsX = x + (DialogWidth - buttonsWidth) / 2;

        _yesButtonRect = new Rectangle(buttonsX, buttonY, ButtonWidth, ButtonHeight);
        _noButtonRect = new Rectangle(buttonsX + ButtonWidth + Padding, buttonY, ButtonWidth, ButtonHeight);

        // Update hover states
        _yesButtonHovered = _yesButtonRect.Contains(mousePos);
        _noButtonHovered = _noButtonRect.Contains(mousePos);

        // Handle clicks
        if (mouseJustPressed)
        {
            if (_yesButtonHovered)
            {
                OnYes?.Invoke();
            }
            else if (_noButtonHovered)
            {
                OnNo?.Invoke();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Draw overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Draw background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 2);

        // Draw title bar
        var titleRect = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleRect, TitleColor);

        // Draw title text
        var titleSize = font.MeasureString(_title);
        var titlePos = new Vector2(
            _bounds.X + (_bounds.Width - titleSize.X) / 2,
            _bounds.Y + (TitleHeight - titleSize.Y) / 2
        );
        spriteBatch.DrawString(font, _title, titlePos, TextColor);

        // Draw message text (word-wrapped)
        int messageY = _bounds.Y + TitleHeight + Padding;
        int messageWidth = _bounds.Width - Padding * 2;
        int messageHeight = _bounds.Height - TitleHeight - ButtonHeight - Padding * 3;

        DrawWrappedText(spriteBatch, font, _message,
            new Rectangle(_bounds.X + Padding, messageY, messageWidth, messageHeight),
            TextColor);

        // Draw Yes button
        var yesColor = _yesButtonHovered ? ButtonHoverColor : ButtonColor;
        spriteBatch.Draw(pixel, _yesButtonRect, yesColor);
        var yesText = "Yes";
        var yesSize = font.MeasureString(yesText);
        var yesPos = new Vector2(
            _yesButtonRect.X + (_yesButtonRect.Width - yesSize.X) / 2,
            _yesButtonRect.Y + (_yesButtonRect.Height - yesSize.Y) / 2
        );
        spriteBatch.DrawString(font, yesText, yesPos, TextColor);

        // Draw No button
        var noColor = _noButtonHovered ? ButtonNoHoverColor : ButtonNoColor;
        spriteBatch.Draw(pixel, _noButtonRect, noColor);
        var noText = "No";
        var noSize = font.MeasureString(noText);
        var noPos = new Vector2(
            _noButtonRect.X + (_noButtonRect.Width - noSize.X) / 2,
            _noButtonRect.Y + (_noButtonRect.Height - noSize.Y) / 2
        );
        spriteBatch.DrawString(font, noText, noPos, TextColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private static void DrawWrappedText(SpriteBatch spriteBatch, SpriteFontBase font, string text, Rectangle bounds, Color color)
    {
        var words = text.Split(' ');
        var sb = new System.Text.StringBuilder();
        float lineHeight = font.LineHeight;
        float y = bounds.Y;
        float currentLineWidth = 0;

        foreach (var word in words)
        {
            var wordSize = font.MeasureString(word + " ");

            if (currentLineWidth + wordSize.X > bounds.Width && sb.Length > 0)
            {
                // Draw current line
                spriteBatch.DrawString(font, sb.ToString(), new Vector2(bounds.X, y), color);
                y += lineHeight;
                sb.Clear();
                currentLineWidth = 0;
            }

            sb.Append(word);
            sb.Append(' ');
            currentLineWidth += wordSize.X;
        }

        // Draw remaining text
        if (sb.Length > 0)
        {
            spriteBatch.DrawString(font, sb.ToString(), new Vector2(bounds.X, y), color);
        }
    }
}
