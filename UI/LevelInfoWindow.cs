using System;
using System.Collections.Generic;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Floating window that displays level description.
/// Can be minimized to title bar only.
/// </summary>
public class LevelInfoWindow
{
    private const int _titleBarHeight = 26;
    private const int _padding = 10;
    private const int _minWidth = 250;
    private const int _maxWidth = 400;
    private const int _buttonSize = 20;

    private static readonly Color _backgroundColor = new(40, 40, 50, 240);
    private static readonly Color _titleBarColor = new(50, 60, 80);
    private static readonly Color _borderColor = new(70, 80, 100);
    private static readonly Color _textColor = new(220, 220, 230);
    private static readonly Color _titleColor = new(255, 255, 255);
    private static readonly Color _buttonColor = new(80, 90, 110);
    private static readonly Color _buttonHoverColor = new(100, 120, 150);

    private GameLevel? _currentLevel;
    private Rectangle _bounds;
    private bool _isMinimized;
    private bool _isDragging;
    private Point _dragOffset;
    private bool _minimizeButtonHovered;
    private List<string> _wrappedLines = new();
    private int _contentHeight;

    public bool IsVisible { get; set; }

    public LevelInfoWindow(int x, int y)
    {
        _bounds = new Rectangle(x, y, _minWidth, _titleBarHeight);
        _isMinimized = false;
    }

    public void SetLevel(GameLevel? level, SpriteFontBase? font)
    {
        _currentLevel = level;
        if (level != null && font != null)
        {
            RecalculateLayout(font);
        }
        IsVisible = level != null;
    }

    private void RecalculateLayout(SpriteFontBase font)
    {
        if (_currentLevel == null)
        {
            return;
        }

        _wrappedLines.Clear();
        int maxTextWidth = _maxWidth - _padding * 2;

        // Get localized description using level id
        string localizationKey = $"level.{_currentLevel.Id}.description";
        string text = LocalizationManager.Get(localizationKey);

        // Fallback to level's fullDescription if no localization found
        if (text == localizationKey || string.IsNullOrEmpty(text))
        {
            text = _currentLevel.FullDescription;
            if (string.IsNullOrEmpty(text))
            {
                text = _currentLevel.Description;
            }
        }

        WrapText(font, text, maxTextWidth);

        // Calculate content height
        float lineHeight = font.LineHeight;
        _contentHeight = (int)(_wrappedLines.Count * lineHeight) + _padding * 2;

        // Calculate window width based on longest line
        int maxLineWidth = 0;
        foreach (var line in _wrappedLines)
        {
            int lineWidth = (int)font.MeasureString(line).X;
            if (lineWidth > maxLineWidth)
            {
                maxLineWidth = lineWidth;
            }
        }

        int titleWidth = (int)font.MeasureString(_currentLevel.Name).X + _buttonSize + _padding * 3;
        int width = Math.Max(Math.Max(maxLineWidth + _padding * 2, titleWidth), _minWidth);
        width = Math.Min(width, _maxWidth);

        _bounds.Width = width;
        _bounds.Height = _isMinimized ? _titleBarHeight : _titleBarHeight + _contentHeight;
    }

    private void WrapText(SpriteFontBase font, string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            Vector2 size = font.MeasureString(testLine);

            if (size.X > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                _wrappedLines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            _wrappedLines.Add(currentLine);
        }
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed,
        bool mouseJustReleased, int screenWidth, int screenHeight, SpriteFontBase font)
    {
        if (!IsVisible || _currentLevel == null)
        {
            return;
        }

        // Calculate minimize button rect
        var minimizeButtonRect = new Rectangle(
            _bounds.Right - _buttonSize - 4,
            _bounds.Y + (_titleBarHeight - _buttonSize) / 2,
            _buttonSize,
            _buttonSize);

        _minimizeButtonHovered = minimizeButtonRect.Contains(mousePos);

        // Handle minimize button click
        if (mouseJustPressed && _minimizeButtonHovered)
        {
            _isMinimized = !_isMinimized;
            RecalculateLayout(font);
            return;
        }

        // Handle dragging
        var titleBarRect = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width - _buttonSize - 8, _titleBarHeight);

        if (mouseJustPressed && titleBarRect.Contains(mousePos))
        {
            _isDragging = true;
            _dragOffset = new Point(mousePos.X - _bounds.X, mousePos.Y - _bounds.Y);
        }

        if (_isDragging)
        {
            if (mousePressed)
            {
                _bounds.X = mousePos.X - _dragOffset.X;
                _bounds.Y = mousePos.Y - _dragOffset.Y;

                // Keep window on screen
                _bounds.X = Math.Max(0, Math.Min(_bounds.X, screenWidth - _bounds.Width));
                _bounds.Y = Math.Max(0, Math.Min(_bounds.Y, screenHeight - _bounds.Height));
            }
            else
            {
                _isDragging = false;
            }
        }

        if (mouseJustReleased)
        {
            _isDragging = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        if (!IsVisible || _currentLevel == null)
        {
            return;
        }

        // Draw background
        spriteBatch.Draw(pixel, _bounds, _backgroundColor);

        // Draw border
        DrawBorder(spriteBatch, pixel, _bounds, _borderColor, 1);

        // Draw title bar
        var titleBarRect = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, _titleBarHeight);
        spriteBatch.Draw(pixel, titleBarRect, _titleBarColor);

        // Draw title text (use localized name if available)
        string nameKey = $"level.{_currentLevel.Id}.name";
        string title = LocalizationManager.Get(nameKey);
        if (title == nameKey)
        {
            title = _currentLevel.Name;
        }
        var titleSize = font.MeasureString(title);
        float titleX = _bounds.X + _padding;
        float titleY = _bounds.Y + (_titleBarHeight - titleSize.Y) / 2;
        font.DrawText(spriteBatch, title, new Vector2(titleX, titleY), _titleColor);

        // Draw minimize button
        var minimizeButtonRect = new Rectangle(
            _bounds.Right - _buttonSize - 4,
            _bounds.Y + (_titleBarHeight - _buttonSize) / 2,
            _buttonSize,
            _buttonSize);

        var buttonColor = _minimizeButtonHovered ? _buttonHoverColor : _buttonColor;
        spriteBatch.Draw(pixel, minimizeButtonRect, buttonColor);

        // Draw minimize/maximize icon
        string icon = _isMinimized ? "+" : "-";
        var iconSize = font.MeasureString(icon);
        float iconX = minimizeButtonRect.X + (minimizeButtonRect.Width - iconSize.X) / 2;
        float iconY = minimizeButtonRect.Y + (minimizeButtonRect.Height - iconSize.Y) / 2;
        font.DrawText(spriteBatch, icon, new Vector2(iconX, iconY), _textColor);

        // Draw content if not minimized
        if (!_isMinimized)
        {
            float lineHeight = font.LineHeight;
            float y = _bounds.Y + _titleBarHeight + _padding;

            foreach (var line in _wrappedLines)
            {
                font.DrawText(spriteBatch, line, new Vector2(_bounds.X + _padding, y), _textColor);
                y += lineHeight;
            }
        }
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
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

    public void SetPosition(int x, int y)
    {
        _bounds.X = x;
        _bounds.Y = y;
    }
}
