using System.Collections.Generic;
using CPUgame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Modal popup showing game controls
/// </summary>
public class ControlsPopup
{
    public bool IsVisible { get; set; }

    private const int PopupWidth = 500;
    private const int PopupHeight = 700;
    private const int TitleHeight = 32;
    private const int Padding = 16;
    private const int CloseButtonSize = 24;
    private const int LineHeight = 22;
    private const int SectionSpacing = 8;

    private static readonly Color OverlayColor = new(0, 0, 0, 180);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color HeaderColor = new(150, 180, 220);
    private static readonly Color KeyColor = new(255, 200, 100);
    private static readonly Color CloseButtonColor = new(180, 60, 60);
    private static readonly Color CloseButtonHoverColor = new(220, 80, 80);

    private Rectangle _bounds;
    private Rectangle _closeButtonRect;
    private bool _closeButtonHovered;

    public void Show()
    {
        IsVisible = true;
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

        // Calculate popup position (centered)
        int popupX = (screenWidth - PopupWidth) / 2;
        int popupY = (screenHeight - PopupHeight) / 2;
        _bounds = new Rectangle(popupX, popupY, PopupWidth, PopupHeight);

        // Close button
        _closeButtonRect = new Rectangle(
            _bounds.Right - CloseButtonSize - Padding,
            _bounds.Y + (TitleHeight - CloseButtonSize) / 2,
            CloseButtonSize,
            CloseButtonSize);
        _closeButtonHovered = _closeButtonRect.Contains(mousePos);

        if (mouseJustPressed && _closeButtonHovered)
        {
            Hide();
            return;
        }

        // Click outside to close
        if (mouseJustPressed && !_bounds.Contains(mousePos))
        {
            Hide();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
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

        var titleText = LocalizationManager.Get("controls.title");
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(_bounds.X + Padding, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw close button
        spriteBatch.Draw(pixel, _closeButtonRect, _closeButtonHovered ? CloseButtonHoverColor : CloseButtonColor);
        var xText = "X";
        var xSize = font.MeasureString(xText);
        spriteBatch.DrawString(font, xText,
            new Vector2(_closeButtonRect.X + (_closeButtonRect.Width - xSize.X) / 2,
                       _closeButtonRect.Y + (_closeButtonRect.Height - xSize.Y) / 2),
            TextColor);

        // Draw controls content
        int y = _bounds.Y + TitleHeight + Padding;

        // General section
        y = DrawSection(spriteBatch, font, pixel, y, "controls.section.general", new[]
        {
            ("controls.pan", "controls.pan.key"),
            ("controls.zoom", "controls.zoom.key"),
            ("controls.delete", "controls.delete.key"),
            ("controls.multiselect", "controls.multiselect.key")
        });

        // Wiring section
        y = DrawSection(spriteBatch, font, pixel, y, "controls.section.wiring", new[]
        {
            ("controls.manual_wire", "controls.manual_wire.key"),
            ("controls.auto_wire", "controls.auto_wire.key"),
            ("controls.cancel_wire", "controls.cancel_wire.key"),
            ("controls.undo_point", "controls.undo_point.key")
        });

        // Wire editing section
        y = DrawSection(spriteBatch, font, pixel, y, "controls.section.wire_edit", new[]
        {
            ("controls.select_wire", "controls.select_wire.key"),
            ("controls.move_node", "controls.move_node.key"),
            ("controls.add_node", "controls.add_node.key"),
            ("controls.remove_node", "controls.remove_node.key")
        });

        // IO Buses section
        y = DrawSection(spriteBatch, font, pixel, y, "controls.section.buses", new[]
        {
            ("controls.resize_bus_add", "controls.resize_bus_add.key"),
            ("controls.resize_bus_remove", "controls.resize_bus_remove.key"),
            ("controls.increment", "controls.increment.key"),
            ("controls.decrement", "controls.decrement.key"),
            ("controls.toggle_values", "controls.toggle_values.key")
        });

        // File section
        DrawSection(spriteBatch, font, pixel, y, "controls.section.file", new[]
        {
            ("controls.save", "controls.save.key"),
            ("controls.load", "controls.load.key"),
            ("controls.build", "controls.build.key")
        });
    }

    private int DrawSection(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, int startY, string headerKey, (string descKey, string keyKey)[] items)
    {
        int y = startY;

        // Draw section header
        var headerText = LocalizationManager.Get(headerKey);
        spriteBatch.DrawString(font, headerText,
            new Vector2(_bounds.X + Padding, y),
            HeaderColor);
        y += LineHeight;

        // Draw separator line
        spriteBatch.Draw(pixel, new Rectangle(_bounds.X + Padding, y, _bounds.Width - Padding * 2, 1), BorderColor);
        y += 6;

        // Draw items
        foreach (var (descKey, keyKey) in items)
        {
            var desc = LocalizationManager.Get(descKey);
            var key = LocalizationManager.Get(keyKey);

            spriteBatch.DrawString(font, desc,
                new Vector2(_bounds.X + Padding + 10, y),
                TextColor);

            var keySize = font.MeasureString(key);
            spriteBatch.DrawString(font, key,
                new Vector2(_bounds.Right - Padding - keySize.X, y),
                KeyColor);

            y += LineHeight;
        }

        y += SectionSpacing;
        return y;
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
