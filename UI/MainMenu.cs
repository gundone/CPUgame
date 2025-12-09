using System;
using System.Collections.Generic;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

public class MainMenu
{
    private readonly List<MenuItem> _menuItems = new();
    private MenuItem? _openMenu;
    private int _hoveredSubmenuIndex = -1;
    private SpriteFont? _font;
    private List<GameLevel> _levels = new();
    private GameMode _currentMode = GameMode.Sandbox;
    private string? _profileName;

    private const int MenuHeight = 24;
    private const int SubmenuItemHeight = 26;
    private const int SubmenuPadding = 4;

    private static readonly Color MenuBarColor = new(40, 40, 50);
    private static readonly Color MenuItemColor = new(50, 50, 60);
    private static readonly Color MenuItemHoverColor = new(70, 70, 85);
    private static readonly Color SubmenuColor = new(45, 45, 55, 250);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color SeparatorColor = new(70, 70, 80);

    public event Action? OnNewCircuit;
    public event Action? OnLoadCircuit;
    public event Action? OnSaveCircuit;
    public event Action? OnSaveCircuitAs;
    public event Action? OnExit;
    public event Action<string>? OnLanguageChanged;
    public event Action<float>? OnTitleFontSizeChanged;
    public event Action? OnToggleTruthTable;
    public event Action? OnSandboxMode;
    public event Action? OnLevelsMode;
    public event Action? OnSelectLevelPopup;
    public event Action? OnShowControls;

    public int Height => MenuHeight;

    public MainMenu()
    {
        BuildMenu();
        LocalizationManager.LanguageChanged += BuildMenu;
    }

    public void SetFont(SpriteFont font)
    {
        _font = font;
    }

    public void SetLevels(List<GameLevel> levels)
    {
        _levels = levels;
        BuildMenu();
    }

    public void SetCurrentMode(GameMode mode)
    {
        _currentMode = mode;
        BuildMenu();
    }

    public void SetProfileName(string? name)
    {
        _profileName = name;
        BuildMenu();
    }

    private int GetTextWidth(string text)
    {
        if (_font != null)
            return (int)_font.MeasureString(text).X;
        return text.Length * 8; // Fallback
    }

    private void BuildMenu()
    {
        _menuItems.Clear();

        if (_currentMode == GameMode.Levels && !string.IsNullOrEmpty(_profileName))
        {
            // In levels mode, show profile name (no submenu)
            var profileItem = new MenuItem(_profileName);
            _menuItems.Add(profileItem);
        }
        else
        {
            // File menu (sandbox mode only)
            var fileMenu = new MenuItem(LocalizationManager.Get("menu.file"));
            fileMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.file.new"), () => OnNewCircuit?.Invoke()));
            fileMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.file.load"), () => OnLoadCircuit?.Invoke()));
            fileMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.file.save"), () => OnSaveCircuit?.Invoke()));
            fileMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.file.saveas"), () => OnSaveCircuitAs?.Invoke()));
            fileMenu.SubItems.Add(new MenuItem("-")); // Separator
            fileMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.file.exit"), () => OnExit?.Invoke()));
            _menuItems.Add(fileMenu);
        }

        // Game menu
        var gameMenu = new MenuItem(LocalizationManager.Get("menu.game"));

        // Mode header
        gameMenu.SubItems.Add(new MenuItem($"-- {LocalizationManager.Get("menu.game.mode")} --"));

        // Sandbox mode
        string sandboxLabel = LocalizationManager.Get("menu.game.sandbox");
        if (_currentMode == GameMode.Sandbox)
        {
            sandboxLabel = "* " + sandboxLabel;
        }
        gameMenu.SubItems.Add(new MenuItem(sandboxLabel, () => OnSandboxMode?.Invoke()));

        // Levels mode
        string levelsLabel = LocalizationManager.Get("menu.game.levels");
        if (_currentMode == GameMode.Levels)
        {
            levelsLabel = "* " + levelsLabel;
        }
        gameMenu.SubItems.Add(new MenuItem(levelsLabel, () => OnLevelsMode?.Invoke()));

        // Add select level option (only in levels mode)
        if (_currentMode == GameMode.Levels)
        {
            gameMenu.SubItems.Add(new MenuItem("-")); // Separator
            gameMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.game.selectlevel"), () => OnSelectLevelPopup?.Invoke()));
        }

        _menuItems.Add(gameMenu);

        // View menu
        var viewMenu = new MenuItem(LocalizationManager.Get("menu.view"));
        viewMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.view.truthtable"), () => OnToggleTruthTable?.Invoke()));
        _menuItems.Add(viewMenu);

        // Options menu
        var optionsMenu = new MenuItem(LocalizationManager.Get("menu.options"));

        // Add title font size header
        optionsMenu.SubItems.Add(new MenuItem($"-- {LocalizationManager.Get("menu.options.titleFontSize")} --"));

        // Add font size options
        optionsMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.options.fontSmall"), () => OnTitleFontSizeChanged?.Invoke(0.6f)));
        optionsMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.options.fontMedium"), () => OnTitleFontSizeChanged?.Invoke(0.8f)));
        optionsMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.options.fontLarge"), () => OnTitleFontSizeChanged?.Invoke(1.0f)));
        optionsMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.options.fontExtraLarge"), () => OnTitleFontSizeChanged?.Invoke(1.2f)));

        optionsMenu.SubItems.Add(new MenuItem("-")); // Separator

        // Add language header
        optionsMenu.SubItems.Add(new MenuItem($"-- {LocalizationManager.Get("menu.options.language")} --"));

        // Add language options directly
        foreach (var lang in LocalizationManager.GetAvailableLanguages())
        {
            var langCode = lang;
            var langName = LocalizationManager.GetLanguageName(lang);
            if (lang == LocalizationManager.CurrentLanguage)
            {
                langName = "* " + langName;
            }
            optionsMenu.SubItems.Add(new MenuItem(langName, () => OnLanguageChanged?.Invoke(langCode)));
        }

        optionsMenu.SubItems.Add(new MenuItem("-")); // Separator
        optionsMenu.SubItems.Add(new MenuItem(LocalizationManager.Get("menu.options.controls"), () => OnShowControls?.Invoke()));

        _menuItems.Add(optionsMenu);
    }

    public void Update(Point mousePos, bool mouseJustPressed, bool mouseJustReleased, int screenWidth)
    {
        // Check if clicking on menu bar
        if (mouseJustPressed && mousePos.Y < MenuHeight)
        {
            int x = 0;
            foreach (var item in _menuItems)
            {
                int itemWidth = GetTextWidth(item.Label) + 20;
                if (mousePos.X >= x && mousePos.X < x + itemWidth)
                {
                    // Only toggle menu if it has subitems
                    if (item.SubItems.Count > 0)
                    {
                        if (_openMenu == item)
                        {
                            _openMenu = null;
                        }
                        else
                        {
                            _openMenu = item;
                        }
                        _hoveredSubmenuIndex = -1;
                    }
                    return;
                }
                x += itemWidth;
            }
        }

        // Check submenu interaction
        if (_openMenu != null)
        {
            var submenuRect = GetSubmenuRect(_openMenu);
            if (submenuRect.Contains(mousePos))
            {
                int index = (mousePos.Y - submenuRect.Y - SubmenuPadding) / SubmenuItemHeight;
                if (index >= 0 && index < _openMenu.SubItems.Count)
                {
                    _hoveredSubmenuIndex = index;

                    if (mouseJustPressed)
                    {
                        var subItem = _openMenu.SubItems[index];
                        if (!subItem.IsSeparator && !subItem.IsHeader && subItem.SubItems.Count == 0)
                        {
                            subItem.Action?.Invoke();
                            _openMenu = null;
                            _hoveredSubmenuIndex = -1;
                        }
                    }
                }
            }
            else
            {
                _hoveredSubmenuIndex = -1;

                // Close menu if clicking outside
                if (mouseJustPressed && mousePos.Y >= MenuHeight)
                {
                    _openMenu = null;
                }
            }
        }
    }

    public bool ContainsPoint(Point p)
    {
        if (p.Y < MenuHeight)
            return true;

        if (_openMenu != null)
        {
            var submenuRect = GetSubmenuRect(_openMenu);
            if (submenuRect.Contains(p))
                return true;
        }

        return false;
    }

    private Rectangle GetSubmenuRect(MenuItem menu)
    {
        int x = 0;
        foreach (var item in _menuItems)
        {
            if (item == menu)
                break;
            x += GetTextWidth(item.Label) + 20;
        }

        int width = 0;
        foreach (var sub in menu.SubItems)
        {
            int itemWidth = GetTextWidth(sub.Label) + 40;
            if (itemWidth > width) width = itemWidth;
        }
        width = Math.Max(width, 150);

        int height = menu.SubItems.Count * SubmenuItemHeight + SubmenuPadding * 2;

        return new Rectangle(x, MenuHeight, width, height);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int screenWidth, Point mousePos)
    {
        // Store font reference for width calculations
        if (_font == null)
            _font = font;

        // Draw menu bar background
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, MenuHeight), MenuBarColor);

        // Draw menu items
        int x = 0;
        foreach (var item in _menuItems)
        {
            int itemWidth = GetTextWidth(item.Label) + 20;
            var itemRect = new Rectangle(x, 0, itemWidth, MenuHeight);

            bool isHovered = itemRect.Contains(mousePos) || _openMenu == item;
            if (isHovered)
            {
                spriteBatch.Draw(pixel, itemRect, MenuItemHoverColor);
            }

            var textSize = font.MeasureString(item.Label);
            spriteBatch.DrawString(font, item.Label,
                new Vector2(x + (itemWidth - textSize.X) / 2, (MenuHeight - textSize.Y) / 2),
                TextColor);

            x += itemWidth;
        }

        // Draw open submenu
        if (_openMenu != null)
        {
            DrawSubmenu(spriteBatch, pixel, font, _openMenu, mousePos);
        }
    }

    private void DrawSubmenu(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, MenuItem menu, Point mousePos)
    {
        var rect = GetSubmenuRect(menu);

        // Background
        spriteBatch.Draw(pixel, rect, SubmenuColor);

        // Border
        DrawBorder(spriteBatch, pixel, rect, SeparatorColor, 1);

        // Items
        int y = rect.Y + SubmenuPadding;
        for (int i = 0; i < menu.SubItems.Count; i++)
        {
            var item = menu.SubItems[i];
            var itemRect = new Rectangle(rect.X, y, rect.Width, SubmenuItemHeight);

            if (item.IsSeparator)
            {
                // Draw separator line
                int sepY = y + SubmenuItemHeight / 2;
                spriteBatch.Draw(pixel, new Rectangle(rect.X + 8, sepY, rect.Width - 16, 1), SeparatorColor);
            }
            else if (item.IsHeader)
            {
                // Draw header (non-clickable, dimmed)
                var textSize = font.MeasureString(item.Label);
                spriteBatch.DrawString(font, item.Label,
                    new Vector2(rect.X + 12, y + (SubmenuItemHeight - textSize.Y) / 2),
                    SeparatorColor);
            }
            else
            {
                // Highlight hovered item
                if (i == _hoveredSubmenuIndex)
                {
                    spriteBatch.Draw(pixel, itemRect, MenuItemHoverColor);
                }

                var textSize = font.MeasureString(item.Label);
                spriteBatch.DrawString(font, item.Label,
                    new Vector2(rect.X + 12, y + (SubmenuItemHeight - textSize.Y) / 2),
                    TextColor);

                // Draw arrow for submenus
                if (item.SubItems.Count > 0)
                {
                    spriteBatch.DrawString(font, ">",
                        new Vector2(rect.Right - 16, y + (SubmenuItemHeight - textSize.Y) / 2),
                        TextColor);
                }
            }

            y += SubmenuItemHeight;
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}

public class MenuItem
{
    public string Label { get; }
    public Action? Action { get; }
    public List<MenuItem> SubItems { get; } = new();
    public bool IsSeparator => Label == "-";
    public bool IsHeader => Label.StartsWith("--");

    public MenuItem(string label, Action? action = null)
    {
        Label = label;
        Action = action;
    }
}
