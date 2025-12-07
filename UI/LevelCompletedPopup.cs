using System;
using CPUgame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Popup that shows when a level is completed
/// </summary>
public class LevelCompletedPopup
{
    public bool IsVisible { get; private set; }

    private GameLevel? _completedLevel;
    private GameLevel? _nextLevel;
    private bool _yesButtonHovered;
    private bool _noButtonHovered;

    private const int PopupWidth = 420;
    private const int PopupHeight = 220;
    private const int TitleHeight = 36;
    private const int ButtonHeight = 32;
    private const int ButtonWidth = 100;
    private const int Padding = 16;
    private const int ButtonSpacing = 20;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 55, 50);
    private static readonly Color TitleColor = new(50, 70, 55);
    private static readonly Color BorderColor = new(80, 120, 90);
    private static readonly Color TextColor = new(220, 230, 220);
    private static readonly Color HighlightColor = new(120, 200, 140);
    private static readonly Color YesButtonColor = new(60, 120, 80);
    private static readonly Color YesButtonHoverColor = new(80, 150, 100);
    private static readonly Color NoButtonColor = new(100, 60, 60);
    private static readonly Color NoButtonHoverColor = new(130, 80, 80);

    private Rectangle _bounds;
    private Rectangle _yesButtonRect;
    private Rectangle _noButtonRect;

    public event Action? OnNextLevel;
    public event Action? OnClose;

    public void Show(GameLevel completedLevel, GameLevel? nextLevel)
    {
        _completedLevel = completedLevel;
        _nextLevel = nextLevel;
        IsVisible = true;
        _yesButtonHovered = false;
        _noButtonHovered = false;
    }

    public void Hide()
    {
        IsVisible = false;
        _completedLevel = null;
        _nextLevel = null;
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _completedLevel == null)
        {
            return;
        }

        // Calculate popup position (centered)
        int popupX = (screenWidth - PopupWidth) / 2;
        int popupY = (screenHeight - PopupHeight) / 2;
        _bounds = new Rectangle(popupX, popupY, PopupWidth, PopupHeight);

        // Calculate button positions
        int buttonsY = _bounds.Bottom - Padding - ButtonHeight;
        int totalButtonsWidth = ButtonWidth * 2 + ButtonSpacing;
        int buttonsStartX = _bounds.X + (_bounds.Width - totalButtonsWidth) / 2;

        if (_nextLevel != null)
        {
            _yesButtonRect = new Rectangle(buttonsStartX, buttonsY, ButtonWidth, ButtonHeight);
            _noButtonRect = new Rectangle(buttonsStartX + ButtonWidth + ButtonSpacing, buttonsY, ButtonWidth, ButtonHeight);
            _yesButtonHovered = _yesButtonRect.Contains(mousePos);
            _noButtonHovered = _noButtonRect.Contains(mousePos);

            if (mouseJustPressed)
            {
                if (_yesButtonHovered)
                {
                    Hide();
                    OnNextLevel?.Invoke();
                }
                else if (_noButtonHovered)
                {
                    Hide();
                    OnClose?.Invoke();
                }
            }
        }
        else
        {
            // Only show OK button if no next level
            _noButtonRect = new Rectangle(_bounds.X + (_bounds.Width - ButtonWidth) / 2, buttonsY, ButtonWidth, ButtonHeight);
            _noButtonHovered = _noButtonRect.Contains(mousePos);
            _yesButtonHovered = false;

            if (mouseJustPressed && _noButtonHovered)
            {
                Hide();
                OnClose?.Invoke();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _completedLevel == null)
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

        // Draw "Level Completed!" title
        var titleText = LocalizationManager.Get("level.completed");
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(_bounds.X + (_bounds.Width - titleSize.X) / 2, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw content
        int contentY = _bounds.Y + TitleHeight + Padding;

        // "You completed level: <name>" - use localized name
        var levelNameKey = $"level.{_completedLevel.Id}.name";
        var levelName = LocalizationManager.Get(levelNameKey);
        if (levelName == levelNameKey)
        {
            levelName = _completedLevel.Name;
        }
        var completedText = LocalizationManager.Get("level.completed_message", levelName);
        var completedSize = font.MeasureString(completedText);
        spriteBatch.DrawString(font, completedText,
            new Vector2(_bounds.X + (_bounds.Width - completedSize.X) / 2, contentY),
            TextColor);
        contentY += (int)completedSize.Y + 8;

        // "You unlocked: <component name>" - use localized name for component too
        var componentName = LocalizationManager.Get(levelNameKey);
        if (componentName == levelNameKey)
        {
            componentName = _completedLevel.ComponentName;
        }
        var unlockedText = LocalizationManager.Get("level.unlocked", componentName);
        var unlockedSize = font.MeasureString(unlockedText);
        spriteBatch.DrawString(font, unlockedText,
            new Vector2(_bounds.X + (_bounds.Width - unlockedSize.X) / 2, contentY),
            HighlightColor);
        contentY += (int)unlockedSize.Y + 8;

        // "It will be added to your collection."
        var addedText = LocalizationManager.Get("level.added_to_collection");
        var addedSize = font.MeasureString(addedText);
        spriteBatch.DrawString(font, addedText,
            new Vector2(_bounds.X + (_bounds.Width - addedSize.X) / 2, contentY),
            TextColor);
        contentY += (int)addedSize.Y + 12;

        if (_nextLevel != null)
        {
            // "Start the next level?"
            var nextText = LocalizationManager.Get("level.start_next");
            var nextSize = font.MeasureString(nextText);
            spriteBatch.DrawString(font, nextText,
                new Vector2(_bounds.X + (_bounds.Width - nextSize.X) / 2, contentY),
                TextColor);

            // Draw Yes button
            spriteBatch.Draw(pixel, _yesButtonRect, _yesButtonHovered ? YesButtonHoverColor : YesButtonColor);
            DrawBorder(spriteBatch, pixel, _yesButtonRect, BorderColor, 1);

            var yesText = LocalizationManager.Get("level.yes");
            var yesSize = font.MeasureString(yesText);
            spriteBatch.DrawString(font, yesText,
                new Vector2(_yesButtonRect.X + (_yesButtonRect.Width - yesSize.X) / 2,
                           _yesButtonRect.Y + (_yesButtonRect.Height - yesSize.Y) / 2),
                TextColor);

            // Draw No button
            spriteBatch.Draw(pixel, _noButtonRect, _noButtonHovered ? NoButtonHoverColor : NoButtonColor);
            DrawBorder(spriteBatch, pixel, _noButtonRect, BorderColor, 1);

            var noText = LocalizationManager.Get("level.no");
            var noSize = font.MeasureString(noText);
            spriteBatch.DrawString(font, noText,
                new Vector2(_noButtonRect.X + (_noButtonRect.Width - noSize.X) / 2,
                           _noButtonRect.Y + (_noButtonRect.Height - noSize.Y) / 2),
                TextColor);
        }
        else
        {
            // All levels completed message
            var allDoneText = LocalizationManager.Get("level.all_completed");
            var allDoneSize = font.MeasureString(allDoneText);
            spriteBatch.DrawString(font, allDoneText,
                new Vector2(_bounds.X + (_bounds.Width - allDoneSize.X) / 2, contentY),
                HighlightColor);

            // Draw OK button
            spriteBatch.Draw(pixel, _noButtonRect, _noButtonHovered ? YesButtonHoverColor : YesButtonColor);
            DrawBorder(spriteBatch, pixel, _noButtonRect, BorderColor, 1);

            var okText = LocalizationManager.Get("level.ok");
            var okSize = font.MeasureString(okText);
            spriteBatch.DrawString(font, okText,
                new Vector2(_noButtonRect.X + (_noButtonRect.Width - okSize.X) / 2,
                           _noButtonRect.Y + (_noButtonRect.Height - okSize.Y) / 2),
                TextColor);
        }
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
