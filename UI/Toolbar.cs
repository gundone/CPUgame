using System;
using System.Collections.Generic;
using CPUgame.Core.Designer;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

public enum ToolType
{
    PlaceNand,
    PlaceSwitch,
    PlaceLed,
    PlaceClock,
    PlaceBusInput,
    PlaceBusOutput
}

public class Toolbox
{
    public Rectangle Bounds { get; private set; }
    public bool IsVisible { get; set; } = true;
    public bool IsDraggingWindow { get; private set; }
    public bool IsUserComponentsToolbox { get; }

    // Drag-to-place state
    public bool IsDraggingItem { get; private set; }
    public ToolType? DraggingTool { get; private set; }
    public string? DraggingCustomComponent { get; private set; }
    public Point DragPosition { get; private set; }

    public string? HoveredComponentForDelete { get; private set; }

    private readonly List<ToolboxItem> _items = new();
    private readonly List<ToolboxItem> _customItems = new();
    private readonly IAppearanceService? _appearanceService;
    private Point _windowDragOffset;

    private const int _itemWidth = 80;
    private const int _itemHeight = 50;
    private const int _padding = 8;
    private const int _titleHeight = 24;
    private const int _deleteButtonSize = 16;

    private static readonly Color _backgroundColor = new(45, 45, 55, 240);
    private static readonly Color _titleColor = new(55, 55, 65);
    private static readonly Color _borderColor = new(80, 80, 100);
    private static readonly Color _textColor = new(220, 220, 230);
    private static readonly Color _itemNormalColor = new(60, 60, 75);
    private static readonly Color _itemHoverColor = new(80, 80, 100);
    private static readonly Color _dragPreviewColor = new(70, 120, 180, 180);
    private static readonly Color _deleteButtonColor = new(180, 60, 60);
    private static readonly Color _deleteButtonHoverColor = new(220, 80, 80);

    public int BusInputBits { get; set; } = 4;
    public int BusOutputBits { get; set; } = 4;

    public Toolbox(int x, int y, bool isUserComponents = false, IAppearanceService? appearanceService = null)
    {
        IsUserComponentsToolbox = isUserComponents;
        _appearanceService = appearanceService;

        if (!isUserComponents)
        {
            // Built-in components
            _items.Add(new ToolboxItem("!&", ToolType.PlaceNand));
            _items.Add(new ToolboxItem("Switch", ToolType.PlaceSwitch));
            _items.Add(new ToolboxItem("LED", ToolType.PlaceLed));
            _items.Add(new ToolboxItem("Clock", ToolType.PlaceClock));
            _items.Add(new ToolboxItem("Input", ToolType.PlaceBusInput));
            _items.Add(new ToolboxItem("Output", ToolType.PlaceBusOutput));
        }

        UpdateBounds(x, y);
    }

    public bool HasCustomComponents => _customItems.Count > 0;

    private void UpdateBounds(int x, int y)
    {
        int totalItems = _items.Count + _customItems.Count;
        int rows = (totalItems + 1) / 2; // 2 columns
        int width = _itemWidth * 2 + _padding * 3;
        int height = _titleHeight + rows * (_itemHeight + _padding) + _padding;
        Bounds = new Rectangle(x, y, width, height);
    }

    public void AddCustomComponent(string name)
    {
        if (!_customItems.Exists(i => i.CustomName == name))
        {
            _customItems.Add(new ToolboxItem(name));
            UpdateBounds(Bounds.X, Bounds.Y);
        }
    }

    public void RemoveCustomComponent(string name)
    {
        _customItems.RemoveAll(i => i.CustomName == name);
        UpdateBounds(Bounds.X, Bounds.Y);
    }

    // Event for delete button clicks
    public event Action<string>? OnDeleteComponent;

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased)
    {
        if (!IsVisible) return;

        // For user components toolbox, skip if no custom components
        if (IsUserComponentsToolbox && !HasCustomComponents) return;

        DragPosition = mousePos;
        HoveredComponentForDelete = null;

        // Handle releasing drag
        if (mouseJustReleased && IsDraggingItem)
        {
            // Keep dragging state for one frame so Game1 can read it and place component
            // It will be cleared next frame
            return;
        }

        // Clear drag state if mouse is released
        if (!mousePressed && IsDraggingItem)
        {
            IsDraggingItem = false;
            DraggingTool = null;
            DraggingCustomComponent = null;
        }

        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, _titleHeight);

        // Handle dragging the window
        if (mouseJustPressed && titleBar.Contains(mousePos) && !IsDraggingItem)
        {
            IsDraggingWindow = true;
            _windowDragOffset = new Point(mousePos.X - Bounds.X, mousePos.Y - Bounds.Y);
        }

        if (IsDraggingWindow)
        {
            if (mousePressed)
            {
                UpdateBounds(mousePos.X - _windowDragOffset.X, mousePos.Y - _windowDragOffset.Y);
            }
            else
            {
                IsDraggingWindow = false;
            }
            return;
        }

        // Handle starting item drag or delete button click
        if (mouseJustPressed && Bounds.Contains(mousePos) && !IsDraggingWindow)
        {
            int index = 0;
            foreach (var item in GetAllItems())
            {
                var itemRect = GetItemRect(index);
                if (itemRect.Contains(mousePos))
                {
                    // Check if clicking delete button (user components only)
                    if (IsUserComponentsToolbox && item.IsCustom)
                    {
                        var deleteRect = GetDeleteButtonRect(itemRect);
                        if (deleteRect.Contains(mousePos))
                        {
                            OnDeleteComponent?.Invoke(item.CustomName!);
                            break;
                        }
                    }

                    IsDraggingItem = true;
                    if (item.IsCustom)
                    {
                        DraggingTool = null;
                        DraggingCustomComponent = item.CustomName;
                    }
                    else
                    {
                        DraggingTool = item.Tool;
                        DraggingCustomComponent = null;
                    }
                    break;
                }
                index++;
            }
        }
    }

    /// <summary>
    /// Call this after placing a component to reset drag state
    /// </summary>
    public void ClearDragState()
    {
        IsDraggingItem = false;
        DraggingTool = null;
        DraggingCustomComponent = null;
    }

    private IEnumerable<ToolboxItem> GetAllItems()
    {
        foreach (var item in _items) yield return item;
        foreach (var item in _customItems) yield return item;
    }

    private Rectangle GetItemRect(int index)
    {
        int col = index % 2;
        int row = index / 2;
        int x = Bounds.X + _padding + col * (_itemWidth + _padding);
        int y = Bounds.Y + _titleHeight + _padding + row * (_itemHeight + _padding);
        return new Rectangle(x, y, _itemWidth, _itemHeight);
    }

    public bool ContainsPoint(Point p)
    {
        if (!IsVisible) return false;
        if (IsUserComponentsToolbox && !HasCustomComponents) return false;
        return Bounds.Contains(p);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Point mousePos)
    {
        if (!IsVisible) return;

        // For user components toolbox, hide if no custom components
        if (IsUserComponentsToolbox && !HasCustomComponents) return;

        // Background
        spriteBatch.Draw(pixel, Bounds, _backgroundColor);

        // Title bar
        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, _titleHeight);
        spriteBatch.Draw(pixel, titleBar, _titleColor);

        // Title text
        var titleText = IsUserComponentsToolbox ? "User Components" : "Toolbox";
        var titleSize = font.MeasureString(titleText);
        font.DrawText(spriteBatch, titleText,
            new Vector2(Bounds.X + (Bounds.Width - titleSize.X) / 2, Bounds.Y + (_titleHeight - titleSize.Y) / 2),
            _textColor);

        // Border
        DrawBorder(spriteBatch, pixel, Bounds, _borderColor, 2);

        // Items
        int index = 0;
        foreach (var item in GetAllItems())
        {
            var itemRect = GetItemRect(index);
            bool isHovered = itemRect.Contains(mousePos);

            var baseColor = GetItemFillColor(item);
            var color = isHovered ? LightenColor(baseColor, 0.2f) : baseColor;
            spriteBatch.Draw(pixel, itemRect, color);
            DrawBorder(spriteBatch, pixel, itemRect, _borderColor, 1);

            // Item label
            var label = GetItemLabel(item);
            var labelSize = font.MeasureString(label);
            font.DrawText(spriteBatch, label,
                new Vector2(itemRect.X + (itemRect.Width - labelSize.X) / 2,
                           itemRect.Y + (itemRect.Height - labelSize.Y) / 2),
                _textColor);

            // Draw delete button for user components toolbox items
            if (IsUserComponentsToolbox && item.IsCustom)
            {
                var deleteRect = GetDeleteButtonRect(itemRect);
                bool deleteHovered = deleteRect.Contains(mousePos);
                spriteBatch.Draw(pixel, deleteRect, deleteHovered ? _deleteButtonHoverColor : _deleteButtonColor);

                // Draw X
                var xText = "X";
                var xSize = font.MeasureString(xText);
                font.DrawText(spriteBatch, xText,
                    new Vector2(deleteRect.X + (deleteRect.Width - xSize.X) / 2,
                               deleteRect.Y + (deleteRect.Height - xSize.Y) / 2),
                    _textColor);
            }

            index++;
        }

        // Draw drag preview
        if (IsDraggingItem)
        {
            var label = DraggingCustomComponent ?? GetToolLabel(DraggingTool);
            var previewRect = new Rectangle(DragPosition.X - 30, DragPosition.Y - 20, 60, 40);
            spriteBatch.Draw(pixel, previewRect, _dragPreviewColor);
            DrawBorder(spriteBatch, pixel, previewRect, _borderColor, 1);

            var labelSize = font.MeasureString(label);
            font.DrawText(spriteBatch, label,
                new Vector2(previewRect.X + (previewRect.Width - labelSize.X) / 2,
                           previewRect.Y + (previewRect.Height - labelSize.Y) / 2),
                _textColor);
        }
    }

    /// <summary>
    /// Gets the fill color for a toolbox item, checking for custom appearance.
    /// </summary>
    private Color GetItemFillColor(ToolboxItem item)
    {
        if (_appearanceService == null)
        {
            return _itemNormalColor;
        }

        string componentType = GetComponentType(item);
        var appearance = _appearanceService.GetAppearance(componentType);

        if (appearance?.FillColor != null)
        {
            return HexToColor(appearance.FillColor);
        }

        return _itemNormalColor;
    }

    /// <summary>
    /// Gets the display label for a toolbox item, checking for custom title.
    /// </summary>
    private string GetItemLabel(ToolboxItem item)
    {
        if (item.IsCustom)
        {
            if (_appearanceService != null)
            {
                string componentType = $"Custom:{item.CustomName}";
                var appearance = _appearanceService.GetAppearance(componentType);
                if (appearance != null && !string.IsNullOrEmpty(appearance.Title))
                {
                    return appearance.Title;
                }
            }
            return item.CustomName!;
        }

        // For built-in components, check for custom title
        if (_appearanceService != null)
        {
            string componentType = GetComponentType(item);
            var appearance = _appearanceService.GetAppearance(componentType);
            if (appearance != null && !string.IsNullOrEmpty(appearance.Title))
            {
                return appearance.Title;
            }
        }

        return item.Label;
    }

    private static string GetComponentType(ToolboxItem item)
    {
        if (item.IsCustom)
        {
            return $"Custom:{item.CustomName}";
        }

        return item.Tool switch
        {
            ToolType.PlaceNand => "NAND",
            ToolType.PlaceSwitch => "Switch",
            ToolType.PlaceLed => "LED",
            ToolType.PlaceClock => "Clock",
            ToolType.PlaceBusInput => "BusInput",
            ToolType.PlaceBusOutput => "BusOutput",
            _ => ""
        };
    }

    private static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length < 7)
        {
            return _itemNormalColor;
        }

        try
        {
            hex = hex.TrimStart('#');
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new Color(r, g, b);
        }
        catch
        {
            return _itemNormalColor;
        }
    }

    private static Color LightenColor(Color color, float amount)
    {
        int r = Math.Min(255, (int)(color.R + (255 - color.R) * amount));
        int g = Math.Min(255, (int)(color.G + (255 - color.G) * amount));
        int b = Math.Min(255, (int)(color.B + (255 - color.B) * amount));
        return new Color(r, g, b, color.A);
    }

    private Rectangle GetDeleteButtonRect(Rectangle itemRect)
    {
        return new Rectangle(
            itemRect.Right - _deleteButtonSize - 2,
            itemRect.Y + 2,
            _deleteButtonSize,
            _deleteButtonSize);
    }

    private string GetToolLabel(ToolType? tool)
    {
        return tool switch
        {
            ToolType.PlaceNand => "NAND",
            ToolType.PlaceSwitch => "Switch",
            ToolType.PlaceLed => "LED",
            ToolType.PlaceClock => "Clock",
            ToolType.PlaceBusInput => $"In[{BusInputBits}]",
            ToolType.PlaceBusOutput => $"Out[{BusOutputBits}]",
            _ => "?"
        };
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public List<string> GetCustomComponentNames()
    {
        var names = new List<string>();
        foreach (var item in _customItems)
        {
            if (item.CustomName != null)
                names.Add(item.CustomName);
        }
        return names;
    }
}

public class ToolboxItem
{
    public string Label { get; }
    public ToolType? Tool { get; }
    public string? CustomName { get; }
    public bool IsCustom => CustomName != null;

    public ToolboxItem(string label, ToolType tool)
    {
        Label = label;
        Tool = tool;
        CustomName = null;
    }

    public ToolboxItem(string customName)
    {
        Label = customName;
        Tool = null;
        CustomName = customName;
    }
}
