using System;
using System.Collections.Generic;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Popup that shows level description when a level starts
/// </summary>
public class LevelDescriptionPopup
{
    public bool IsVisible { get; private set; }

    private GameLevel? _level;
    private bool _startButtonHovered;

    private const int PopupWidth = 500;
    private const int PopupHeight = 350;
    private const int TitleHeight = 36;
    private const int ButtonHeight = 32;
    private const int ButtonWidth = 120;
    private const int Padding = 16;
    private const int ButtonAreaHeight = 60;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color DescriptionColor = new(180, 180, 195);
    private static readonly Color ButtonColor = new(60, 120, 80);
    private static readonly Color ButtonHoverColor = new(80, 150, 100);

    private Rectangle _bounds;
    private Rectangle _startButtonRect;

    public event Action? OnStartLevel;

    public void Show(GameLevel level)
    {
        _level = level;
        IsVisible = true;
        _startButtonHovered = false;
    }

    public void Hide()
    {
        IsVisible = false;
        _level = null;
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _level == null)
        {
            return;
        }

        // Calculate popup position (centered)
        int popupX = (screenWidth - PopupWidth) / 2;
        int popupY = (screenHeight - PopupHeight) / 2;
        _bounds = new Rectangle(popupX, popupY, PopupWidth, PopupHeight);

        // Start button
        _startButtonRect = new Rectangle(
            _bounds.X + (_bounds.Width - ButtonWidth) / 2,
            _bounds.Bottom - Padding - ButtonHeight,
            ButtonWidth,
            ButtonHeight);
        _startButtonHovered = _startButtonRect.Contains(mousePos);

        if (mouseJustPressed && _startButtonHovered)
        {
            Hide();
            OnStartLevel?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _level == null)
        {
            return;
        }

        // Draw overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Draw popup background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 2);

        // Draw title bar
        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        // Draw level name as title (use localized name if available)
        var titleKey = $"level.{_level.Id}.name";
        var titleText = LocalizationManager.Get(titleKey);
        if (titleText == titleKey)
        {
            // Fallback to level name from JSON if localization not found
            titleText = _level.Name;
        }
        var titleSize = font.MeasureString(titleText);
        font.DrawText(spriteBatch, titleText,
            new Vector2(_bounds.X + (_bounds.Width - titleSize.X) / 2, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw description
        int contentY = _bounds.Y + TitleHeight + Padding;
        int contentWidth = PopupWidth - Padding * 2;

        // Use localized description if available, otherwise fall back to JSON
        var descKey = $"level.{_level.Id}.description";
        string description = LocalizationManager.Get(descKey);
        if (description == descKey)
        {
            // Fallback to JSON description
            description = !string.IsNullOrEmpty(_level.FullDescription)
                ? _level.FullDescription
                : _level.Description;
        }

        // Word wrap the description
        var lines = WrapText(font, description, contentWidth);
        int maxContentY = _bounds.Bottom - ButtonAreaHeight - Padding;
        foreach (var line in lines)
        {
            int lineHeight = (int)font.MeasureString(line).Y + 4;
            if (contentY + lineHeight > maxContentY)
            {
                break; // Stop drawing to avoid overlapping with button
            }
            font.DrawText(spriteBatch, line, new Vector2(_bounds.X + Padding, contentY), DescriptionColor);
            contentY += lineHeight;
        }

        // Draw start button
        spriteBatch.Draw(pixel, _startButtonRect, _startButtonHovered ? ButtonHoverColor : ButtonColor);
        DrawBorder(spriteBatch, pixel, _startButtonRect, BorderColor, 1);

        var startText = LocalizationManager.Get("level.start");
        var startSize = font.MeasureString(startText);
        font.DrawText(spriteBatch, startText,
            new Vector2(_startButtonRect.X + (_startButtonRect.Width - startSize.X) / 2,
                       _startButtonRect.Y + (_startButtonRect.Height - startSize.Y) / 2),
            TextColor);
    }

    private List<string> WrapText(SpriteFontBase font, string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = font.MeasureString(testLine);

            if (testSize.X > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public bool ContainsPoint(Point p)
    {
        return IsVisible && _bounds.Contains(p);
    }
}
