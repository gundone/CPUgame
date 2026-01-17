using System;
using System.Collections.Generic;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Panel for selecting components to edit in the designer.
/// </summary>
public class ComponentSelectorPanel : IComponentSelectorPanel
{
    private List<string> _componentTypes = new();
    private int _selectorScrollOffset;
    private int _hoveredComponentIndex = -1;
    private string? _selectedComponentType;

    public string? SelectedComponentType => _selectedComponentType;

    public event Action<string>? OnComponentSelected;

    public void RefreshComponentList(IReadOnlyDictionary<string, object> customComponents)
    {
        _componentTypes.Clear();

        // Built-in components (exclude BusInput/BusOutput - they have dynamic pins)
        _componentTypes.Add("NAND");
        _componentTypes.Add("Switch");
        _componentTypes.Add("LED");
        _componentTypes.Add("Clock");

        // Custom components
        foreach (var name in customComponents.Keys)
        {
            _componentTypes.Add($"Custom:{name}");
        }
    }

    public void Update(Point mousePos, bool mouseJustPressed, int scrollDelta, Rectangle bounds)
    {
        _hoveredComponentIndex = -1;

        if (!bounds.Contains(mousePos))
        {
            return;
        }

        // Handle scroll
        if (scrollDelta != 0)
        {
            _selectorScrollOffset -= scrollDelta / 40;
            _selectorScrollOffset = Math.Max(0, _selectorScrollOffset);
        }

        // Find hovered item - must match Draw layout exactly
        int y = bounds.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding
            - _selectorScrollOffset * DesignerLayout.ItemHeight;

        // Skip "Built-in" header
        y += DesignerLayout.ItemHeight;

        for (int i = 0; i < _componentTypes.Count; i++)
        {
            var componentType = _componentTypes[i];

            // Skip "Custom" header when transitioning from built-in to custom
            if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
            {
                y += DesignerLayout.ItemHeight;
            }

            var itemRect = new Rectangle(
                bounds.X + DesignerLayout.Padding,
                y,
                bounds.Width - DesignerLayout.Padding * 2,
                DesignerLayout.ItemHeight);

            if (itemRect.Contains(mousePos))
            {
                _hoveredComponentIndex = i;

                if (mouseJustPressed)
                {
                    _selectedComponentType = _componentTypes[i];
                    OnComponentSelected?.Invoke(_componentTypes[i]);
                }
                break;
            }
            y += DesignerLayout.ItemHeight;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, DesignerColors.PanelColor);
        DesignerDrawing.DrawBorder(spriteBatch, pixel, rect, DesignerColors.BorderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, DesignerLayout.HeaderHeight);
        spriteBatch.Draw(pixel, headerRect, DesignerColors.HeaderColor);

        var headerText = LocalizationManager.Get("designer.select_component");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(
            spriteBatch,
            headerText,
            new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (DesignerLayout.HeaderHeight - headerSize.Y) / 2),
            DesignerColors.TextColor);

        // Component list
        int y = rect.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding
            - _selectorScrollOffset * DesignerLayout.ItemHeight;

        // Built-in header
        if (y > rect.Y + DesignerLayout.HeaderHeight)
        {
            font.DrawText(
                spriteBatch,
                LocalizationManager.Get("designer.builtin"),
                new Vector2(rect.X + DesignerLayout.Padding, y),
                DesignerColors.DimTextColor);
        }
        y += DesignerLayout.ItemHeight;

        for (int i = 0; i < _componentTypes.Count; i++)
        {
            var componentType = _componentTypes[i];
            var itemRect = new Rectangle(
                rect.X + DesignerLayout.Padding,
                y,
                rect.Width - DesignerLayout.Padding * 2,
                DesignerLayout.ItemHeight);

            // Skip if outside visible area
            if (y + DesignerLayout.ItemHeight < rect.Y + DesignerLayout.HeaderHeight || y > rect.Bottom)
            {
                y += DesignerLayout.ItemHeight;
                continue;
            }

            // Custom components header
            if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
            {
                font.DrawText(
                    spriteBatch,
                    LocalizationManager.Get("designer.custom"),
                    new Vector2(rect.X + DesignerLayout.Padding, y),
                    DesignerColors.DimTextColor);
                y += DesignerLayout.ItemHeight;
                itemRect = new Rectangle(
                    rect.X + DesignerLayout.Padding,
                    y,
                    rect.Width - DesignerLayout.Padding * 2,
                    DesignerLayout.ItemHeight);
            }

            // Draw item
            bool isSelected = componentType == _selectedComponentType;
            bool isHovered = i == _hoveredComponentIndex;

            if (isSelected)
            {
                spriteBatch.Draw(pixel, itemRect, DesignerColors.SelectedColor);
            }
            else if (isHovered)
            {
                spriteBatch.Draw(pixel, itemRect, DesignerColors.HoverColor);
            }

            string displayName = componentType.StartsWith("Custom:") ? componentType.Substring(7) : componentType;
            font.DrawText(
                spriteBatch,
                displayName,
                new Vector2(itemRect.X + 4, itemRect.Y + (itemRect.Height - font.MeasureString(displayName).Y) / 2),
                DesignerColors.TextColor);

            y += DesignerLayout.ItemHeight;
        }
    }

    public void Reset()
    {
        _selectedComponentType = null;
        _selectorScrollOffset = 0;
        _hoveredComponentIndex = -1;
    }

}
