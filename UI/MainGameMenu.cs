using System;
using System.Collections.Generic;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using CPUgame.Core.Services;
using CPUgame.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Main game menu modal dialog with keyboard navigation
/// </summary>
public class MainGameMenu
{
    public bool IsVisible { get; private set; }

    private readonly IPreferencesService _preferencesService;
    private readonly IFontService _fontService;

    private int _selectedIndex;
    private List<MenuOption> _menuOptions = new();

    private const int DialogWidth = 400;
    private const int MenuItemHeight = 48;
    private const int TitleHeight = 80;
    private const int HintHeight = 40;
    private const int Padding = 20;

    private static readonly Color OverlayColor = new(0, 0, 0, 220);
    private static readonly Color BackgroundColor = new(40, 40, 50);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color TitleColor = new(200, 200, 220);
    private static readonly Color ItemColor = new(50, 50, 65);
    private static readonly Color ItemSelectedColor = new(80, 120, 160);
    private static readonly Color HintColor = new(140, 140, 160);
    private static readonly Color DisabledColor = new(100, 100, 110);

    private Rectangle _bounds;

    public event Action? OnContinue;
    public event Action? OnNewGame;
    public event Action? OnSandbox;
    public event Action? OnDesigner;
    public event Action? OnOptions;
    public event Action? OnQuit;

    private class MenuOption
    {
        public string Label { get; set; } = "";
        public Action? Action { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public MainGameMenu(IPreferencesService preferencesService, IFontService fontService)
    {
        _preferencesService = preferencesService;
        _fontService = fontService;
    }

    public void Show()
    {
        IsVisible = true;
        _selectedIndex = 0;
        BuildMenuOptions();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private void BuildMenuOptions()
    {
        _menuOptions.Clear();

        // Continue option
        string continueLabel = LocalizationManager.Get("mainmenu.continue");
        string? lastProfile = _preferencesService.LastProfile;
        bool hasContinue = !string.IsNullOrEmpty(lastProfile);

        if (hasContinue)
        {
            continueLabel = $"{continueLabel} ({lastProfile})";
        }

        _menuOptions.Add(new MenuOption
        {
            Label = continueLabel,
            Action = () => OnContinue?.Invoke(),
            Enabled = hasContinue
        });

        // New Game
        _menuOptions.Add(new MenuOption
        {
            Label = LocalizationManager.Get("mainmenu.newgame"),
            Action = () => OnNewGame?.Invoke(),
            Enabled = true
        });

        // Sandbox
        _menuOptions.Add(new MenuOption
        {
            Label = LocalizationManager.Get("mainmenu.sandbox"),
            Action = () => OnSandbox?.Invoke(),
            Enabled = true
        });

        // Designer
        _menuOptions.Add(new MenuOption
        {
            Label = LocalizationManager.Get("mainmenu.designer"),
            Action = () => OnDesigner?.Invoke(),
            Enabled = true
        });

        // Options
        _menuOptions.Add(new MenuOption
        {
            Label = LocalizationManager.Get("mainmenu.options"),
            Action = () => OnOptions?.Invoke(),
            Enabled = true
        });

        // Quit
        _menuOptions.Add(new MenuOption
        {
            Label = LocalizationManager.Get("mainmenu.quit"),
            Action = () => OnQuit?.Invoke(),
            Enabled = true
        });

        // Ensure selected index is valid and points to enabled option
        if (_selectedIndex >= _menuOptions.Count)
        {
            _selectedIndex = 0;
        }

        if (!_menuOptions[_selectedIndex].Enabled)
        {
            MoveSelectionDown();
        }
    }

    public void Update(InputState input, int screenWidth, int screenHeight)
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
        int dialogHeight = TitleHeight + (_menuOptions.Count * MenuItemHeight) + HintHeight + Padding * 2;
        int x = (screenWidth - DialogWidth) / 2;
        int y = (screenHeight - dialogHeight) / 2;
        _bounds = new Rectangle(x, y, DialogWidth, dialogHeight);

        // Get mouse position
        var mousePos = input.PointerPosition;
        int currentY = y + TitleHeight + Padding;

        // Check mouse hover over menu items
        bool mouseOverAnyItem = false;
        for (int i = 0; i < _menuOptions.Count; i++)
        {
            var itemRect = new Rectangle(
                x + Padding,
                currentY,
                DialogWidth - Padding * 2,
                MenuItemHeight
            );

            if (itemRect.Contains(mousePos.X, mousePos.Y))
            {
                if (_menuOptions[i].Enabled)
                {
                    _selectedIndex = i;
                    mouseOverAnyItem = true;

                    // Handle mouse click
                    if (input.PrimaryJustPressed)
                    {
                        _menuOptions[i].Action?.Invoke();
                    }
                }
                break;
            }

            currentY += MenuItemHeight;
        }

        // Handle arrow keys (only if mouse is not hovering)
        if (!mouseOverAnyItem)
        {
            if (input.MoveUp)
            {
                MoveSelectionUp();
            }

            if (input.MoveDown)
            {
                MoveSelectionDown();
            }
        }

        // Handle Enter key
        if (input.EnterPressed)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _menuOptions.Count)
            {
                var option = _menuOptions[_selectedIndex];
                if (option.Enabled)
                {
                    option.Action?.Invoke();
                }
            }
        }
    }

    private void MoveSelectionUp()
    {
        int startIndex = _selectedIndex;

        do
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                _selectedIndex = _menuOptions.Count - 1;
            }

            if (_menuOptions[_selectedIndex].Enabled)
            {
                break;
            }
        }
        while (_selectedIndex != startIndex);
    }

    private void MoveSelectionDown()
    {
        int startIndex = _selectedIndex;

        do
        {
            _selectedIndex++;
            if (_selectedIndex >= _menuOptions.Count)
            {
                _selectedIndex = 0;
            }

            if (_menuOptions[_selectedIndex].Enabled)
            {
                break;
            }
        }
        while (_selectedIndex != startIndex);
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

        int currentY = _bounds.Y;

        // Draw title
        string title = LocalizationManager.Get("mainmenu.title");
        var titleFont = _fontService.GetFontAtSize(64);
        var titleSize = titleFont.MeasureString(title);
        var titlePos = new Vector2(
            _bounds.X + (_bounds.Width - titleSize.X) / 2,
            currentY + (TitleHeight - titleSize.Y) / 2
        );
        spriteBatch.DrawString(titleFont, title, titlePos, TitleColor);

        currentY += TitleHeight + Padding;

        // Draw menu items
        for (int i = 0; i < _menuOptions.Count; i++)
        {
            var option = _menuOptions[i];
            bool isSelected = i == _selectedIndex;

            var itemRect = new Rectangle(
                _bounds.X + Padding,
                currentY,
                _bounds.Width - Padding * 2,
                MenuItemHeight
            );

            // Draw item background
            if (isSelected && option.Enabled)
            {
                spriteBatch.Draw(pixel, itemRect, ItemSelectedColor);
            }
            else if (option.Enabled)
            {
                spriteBatch.Draw(pixel, itemRect, ItemColor);
            }

            // Draw item text
            var textColor = option.Enabled ? TextColor : DisabledColor;
            var textSize = font.MeasureString(option.Label);
            var textPos = new Vector2(
                itemRect.X + (itemRect.Width - textSize.X) / 2,
                itemRect.Y + (itemRect.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, option.Label, textPos, textColor);

            currentY += MenuItemHeight;
        }

        currentY += Padding;

        // Draw hint
        string hint = LocalizationManager.Get("mainmenu.hint");
        var hintFont = _fontService.GetFontAtSize(12);
        var hintSize = hintFont.MeasureString(hint);
        var hintPos = new Vector2(
            _bounds.X + (_bounds.Width - hintSize.X) / 2,
            currentY + (HintHeight - hintSize.Y) / 2
        );
        spriteBatch.DrawString(hintFont, hint, hintPos, HintColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
