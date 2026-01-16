using System;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Context menu for custom components
/// </summary>
public class ComponentContextMenu
{
    public bool IsVisible { get; private set; }

    private Rectangle _bounds;
    private int _hoveredItemIndex = -1;
    private const int ItemWidth = 120;
    private const int ItemHeight = 28;
    private const int Padding = 4;

    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color HoverColor = new(70, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);

    public event Action? OnEdit;
    public event Action? OnDelete;

    public void Show(int screenX, int screenY, int screenWidth, int screenHeight)
    {
        IsVisible = true;

        // Position the menu at the mouse cursor
        int x = screenX;
        int y = screenY;

        int menuWidth = ItemWidth;
        int menuHeight = ItemHeight * 2; // Edit + Delete

        // Keep menu on screen
        if (x + menuWidth > screenWidth)
        {
            x = screenWidth - menuWidth;
        }
        if (y + menuHeight > screenHeight)
        {
            y = screenHeight - menuHeight;
        }

        _bounds = new Rectangle(x, y, menuWidth, menuHeight);
    }

    public void Hide()
    {
        IsVisible = false;
        _hoveredItemIndex = -1;
    }

    public void Update(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed)
    {
        if (!IsVisible)
        {
            return;
        }

        // Update hover state
        _hoveredItemIndex = -1;
        if (_bounds.Contains(mousePos))
        {
            int localY = mousePos.Y - _bounds.Y;
            _hoveredItemIndex = localY / ItemHeight;
        }

        // Handle clicks
        if (mouseJustPressed)
        {
            if (_hoveredItemIndex == 0)
            {
                OnEdit?.Invoke();
            }
            else if (_hoveredItemIndex == 1)
            {
                OnDelete?.Invoke();
            }
            Hide();
        }
        else if (rightMouseJustPressed)
        {
            Hide();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        if (!IsVisible)
        {
            return;
        }

        // Draw background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 1);

        // Draw Edit item
        var editRect = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, ItemHeight);
        if (_hoveredItemIndex == 0)
        {
            spriteBatch.Draw(pixel, editRect, HoverColor);
        }
        string editText = LocalizationManager.Get("contextmenu.edit");
        var editSize = font.MeasureString(editText);
        font.DrawText(spriteBatch, editText, new Vector2(editRect.X + Padding, editRect.Y + (ItemHeight - editSize.Y) / 2), TextColor);

        // Draw Delete item
        var deleteRect = new Rectangle(_bounds.X, _bounds.Y + ItemHeight, _bounds.Width, ItemHeight);
        if (_hoveredItemIndex == 1)
        {
            spriteBatch.Draw(pixel, deleteRect, HoverColor);
        }
        string deleteText = LocalizationManager.Get("contextmenu.delete");
        var deleteSize = font.MeasureString(deleteText);
        font.DrawText(spriteBatch, deleteText, new Vector2(deleteRect.X + Padding, deleteRect.Y + (ItemHeight - deleteSize.Y) / 2), TextColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public bool ContainsPoint(Point point)
    {
        return IsVisible && _bounds.Contains(point);
    }
}
