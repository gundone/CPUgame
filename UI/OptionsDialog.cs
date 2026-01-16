using System;
using System.Collections.Generic;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Options/settings dialog
/// </summary>
public class OptionsDialog
{
    public bool IsVisible { get; private set; }

    private readonly float[] _fontSizes = { 0.6f, 0.8f, 1.0f, 1.2f };
    private readonly string[] _fontSizeLabels = { "Small", "Medium", "Large", "Extra Large" };

    private List<string> _languages = new();
    private List<Rectangle> _fontSizeRects = new();
    private List<Rectangle> _languageRects = new();
    private Rectangle _controlsButtonRect;

    private int _hoveredFontSizeIndex = -1;
    private int _hoveredLanguageIndex = -1;
    private bool _controlsButtonHovered;

    private const int DialogWidth = 450;
    private const int DialogHeight = 500;
    private const int TitleHeight = 32;
    private const int ItemHeight = 32;
    private const int ButtonHeight = 32;
    private const int Padding = 16;
    private const int SectionSpacing = 8;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color ItemColor = new(55, 55, 70);
    private static readonly Color ItemHoverColor = new(70, 80, 100);
    private static readonly Color ItemSelectedColor = new(60, 100, 140);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color HeaderColor = new(180, 180, 200);

    private Rectangle _bounds;
    private float _currentFontSize = 0.8f;
    private string _currentLanguage = "en";

    public event Action<float>? OnFontSizeChanged;
    public event Action<string>? OnLanguageChanged;
    public event Action? OnShowControls;
    public event Action? OnClose;

    public void Show()
    {
        IsVisible = true;
        _languages = LocalizationManager.GetAvailableLanguages();
        _currentLanguage = LocalizationManager.CurrentLanguage;
    }

    public void Hide()
    {
        IsVisible = false;
        OnClose?.Invoke();
    }

    public void SetCurrentFontSize(float size)
    {
        _currentFontSize = size;
    }

    public void Update(InputState input, Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Handle ESC to close
        if (input.EscapeCommand)
        {
            Hide();
            return;
        }

        // Calculate bounds
        int x = (screenWidth - DialogWidth) / 2;
        int y = (screenHeight - DialogHeight) / 2;
        _bounds = new Rectangle(x, y, DialogWidth, DialogHeight);

        int currentY = y + TitleHeight + Padding;

        // Font size section
        currentY += 24; // Header height
        _fontSizeRects.Clear();
        for (int i = 0; i < _fontSizes.Length; i++)
        {
            var rect = new Rectangle(x + Padding, currentY, DialogWidth - Padding * 2, ItemHeight);
            _fontSizeRects.Add(rect);
            currentY += ItemHeight + 4;
        }

        currentY += SectionSpacing;

        // Language section
        currentY += 24; // Header height
        _languageRects.Clear();
        for (int i = 0; i < _languages.Count; i++)
        {
            var rect = new Rectangle(x + Padding, currentY, DialogWidth - Padding * 2, ItemHeight);
            _languageRects.Add(rect);
            currentY += ItemHeight + 4;
        }

        currentY += SectionSpacing;

        // Controls button
        _controlsButtonRect = new Rectangle(
            x + Padding,
            currentY,
            DialogWidth - Padding * 2,
            ButtonHeight
        );

        // Update hover states
        _hoveredFontSizeIndex = -1;
        for (int i = 0; i < _fontSizeRects.Count; i++)
        {
            if (_fontSizeRects[i].Contains(mousePos))
            {
                _hoveredFontSizeIndex = i;
                break;
            }
        }

        _hoveredLanguageIndex = -1;
        for (int i = 0; i < _languageRects.Count; i++)
        {
            if (_languageRects[i].Contains(mousePos))
            {
                _hoveredLanguageIndex = i;
                break;
            }
        }

        _controlsButtonHovered = _controlsButtonRect.Contains(mousePos);

        // Handle clicks
        if (mouseJustPressed)
        {
            if (_hoveredFontSizeIndex >= 0)
            {
                _currentFontSize = _fontSizes[_hoveredFontSizeIndex];
                OnFontSizeChanged?.Invoke(_currentFontSize);
            }
            else if (_hoveredLanguageIndex >= 0)
            {
                _currentLanguage = _languages[_hoveredLanguageIndex];
                OnLanguageChanged?.Invoke(_currentLanguage);
                _languages = LocalizationManager.GetAvailableLanguages();
            }
            else if (_controlsButtonHovered)
            {
                OnShowControls?.Invoke();
            }
            else if (!_bounds.Contains(mousePos))
            {
                // Click outside to close
                Hide();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Safety check: ensure lists are populated
        if (_fontSizeRects.Count == 0 || _languageRects.Count == 0)
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
        string title = LocalizationManager.Get("options.title");
        var titleSize = font.MeasureString(title);
        var titlePos = new Vector2(
            _bounds.X + (_bounds.Width - titleSize.X) / 2,
            _bounds.Y + (TitleHeight - titleSize.Y) / 2
        );
        spriteBatch.DrawString(font, title, titlePos, TextColor);

        int currentY = _bounds.Y + TitleHeight + Padding;

        // Draw font size section
        string fontSizeHeader = LocalizationManager.Get("options.titleFontSize");
        spriteBatch.DrawString(font, fontSizeHeader, new Vector2(_bounds.X + Padding, currentY), HeaderColor);
        currentY += 24;

        for (int i = 0; i < _fontSizes.Length && i < _fontSizeRects.Count; i++)
        {
            var rect = _fontSizeRects[i];
            bool isSelected = Math.Abs(_fontSizes[i] - _currentFontSize) < 0.01f;
            bool isHovered = _hoveredFontSizeIndex == i;

            var itemColor = isSelected ? ItemSelectedColor : (isHovered ? ItemHoverColor : ItemColor);
            spriteBatch.Draw(pixel, rect, itemColor);

            string label = LocalizationManager.Get($"menu.options.font{_fontSizeLabels[i].Replace(" ", "")}");
            if (isSelected)
            {
                label = "* " + label;
            }

            var textPos = new Vector2(rect.X + 8, rect.Y + (rect.Height - font.LineHeight) / 2);
            spriteBatch.DrawString(font, label, textPos, TextColor);
        }

        if (_fontSizeRects.Count > 0)
        {
            currentY = _fontSizeRects[_fontSizeRects.Count - 1].Bottom + SectionSpacing + 4;
        }

        // Draw language section
        string languageHeader = LocalizationManager.Get("options.language");
        spriteBatch.DrawString(font, languageHeader, new Vector2(_bounds.X + Padding, currentY), HeaderColor);
        currentY += 24;

        for (int i = 0; i < _languages.Count && i < _languageRects.Count; i++)
        {
            var rect = _languageRects[i];
            bool isSelected = _languages[i] == _currentLanguage;
            bool isHovered = _hoveredLanguageIndex == i;

            var itemColor = isSelected ? ItemSelectedColor : (isHovered ? ItemHoverColor : ItemColor);
            spriteBatch.Draw(pixel, rect, itemColor);

            string langName = LocalizationManager.GetLanguageName(_languages[i]);
            if (isSelected)
            {
                langName = "* " + langName;
            }

            var textPos = new Vector2(rect.X + 8, rect.Y + (rect.Height - font.LineHeight) / 2);
            spriteBatch.DrawString(font, langName, textPos, TextColor);
        }

        // Draw controls button
        var buttonColor = _controlsButtonHovered ? ButtonHoverColor : ButtonColor;
        spriteBatch.Draw(pixel, _controlsButtonRect, buttonColor);

        string controlsText = LocalizationManager.Get("options.controls");
        var controlsSize = font.MeasureString(controlsText);
        var controlsPos = new Vector2(
            _controlsButtonRect.X + (_controlsButtonRect.Width - controlsSize.X) / 2,
            _controlsButtonRect.Y + (_controlsButtonRect.Height - controlsSize.Y) / 2
        );
        spriteBatch.DrawString(font, controlsText, controlsPos, TextColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
