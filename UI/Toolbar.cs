using System.Collections.Generic;
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

    // Delete mode for user components
    public bool IsDeleteMode { get; private set; }
    public string? HoveredComponentForDelete { get; private set; }

    private readonly List<ToolboxItem> _items = new();
    private readonly List<ToolboxItem> _customItems = new();
    private Point _windowDragOffset;

    private const int ItemWidth = 80;
    private const int ItemHeight = 50;
    private const int Padding = 8;
    private const int TitleHeight = 24;
    private const int DeleteButtonSize = 16;

    private static readonly Color BackgroundColor = new(45, 45, 55, 240);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color ItemNormalColor = new(60, 60, 75);
    private static readonly Color ItemHoverColor = new(80, 80, 100);
    private static readonly Color DragPreviewColor = new(70, 120, 180, 180);
    private static readonly Color DeleteButtonColor = new(180, 60, 60);
    private static readonly Color DeleteButtonHoverColor = new(220, 80, 80);

    public int BusInputBits { get; set; } = 4;
    public int BusOutputBits { get; set; } = 4;

    public Toolbox(int x, int y, bool isUserComponents = false)
    {
        IsUserComponentsToolbox = isUserComponents;

        if (!isUserComponents)
        {
            // Built-in components
            _items.Add(new ToolboxItem("NAND", ToolType.PlaceNand));
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
        int width = ItemWidth * 2 + Padding * 3;
        int height = TitleHeight + rows * (ItemHeight + Padding) + Padding;
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
    public event System.Action<string>? OnDeleteComponent;

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

        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TitleHeight);

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
        int x = Bounds.X + Padding + col * (ItemWidth + Padding);
        int y = Bounds.Y + TitleHeight + Padding + row * (ItemHeight + Padding);
        return new Rectangle(x, y, ItemWidth, ItemHeight);
    }

    public bool ContainsPoint(Point p)
    {
        if (!IsVisible) return false;
        if (IsUserComponentsToolbox && !HasCustomComponents) return false;
        return Bounds.Contains(p);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos)
    {
        if (!IsVisible) return;

        // For user components toolbox, hide if no custom components
        if (IsUserComponentsToolbox && !HasCustomComponents) return;

        // Background
        spriteBatch.Draw(pixel, Bounds, BackgroundColor);

        // Title bar
        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        // Title text
        var titleText = IsUserComponentsToolbox ? "User Components" : "Toolbox";
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(Bounds.X + (Bounds.Width - titleSize.X) / 2, Bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Border
        DrawBorder(spriteBatch, pixel, Bounds, BorderColor, 2);

        // Items
        int index = 0;
        foreach (var item in GetAllItems())
        {
            var itemRect = GetItemRect(index);
            bool isHovered = itemRect.Contains(mousePos);

            var color = isHovered ? ItemHoverColor : ItemNormalColor;
            spriteBatch.Draw(pixel, itemRect, color);
            DrawBorder(spriteBatch, pixel, itemRect, BorderColor, 1);

            // Item label
            var label = item.IsCustom ? item.CustomName! : item.Label;
            var labelSize = font.MeasureString(label);
            spriteBatch.DrawString(font, label,
                new Vector2(itemRect.X + (itemRect.Width - labelSize.X) / 2,
                           itemRect.Y + (itemRect.Height - labelSize.Y) / 2),
                TextColor);

            // Draw delete button for user components toolbox items
            if (IsUserComponentsToolbox && item.IsCustom)
            {
                var deleteRect = GetDeleteButtonRect(itemRect);
                bool deleteHovered = deleteRect.Contains(mousePos);
                spriteBatch.Draw(pixel, deleteRect, deleteHovered ? DeleteButtonHoverColor : DeleteButtonColor);

                // Draw X
                var xText = "X";
                var xSize = font.MeasureString(xText);
                spriteBatch.DrawString(font, xText,
                    new Vector2(deleteRect.X + (deleteRect.Width - xSize.X) / 2,
                               deleteRect.Y + (deleteRect.Height - xSize.Y) / 2),
                    TextColor);
            }

            index++;
        }

        // Draw drag preview
        if (IsDraggingItem)
        {
            var label = DraggingCustomComponent ?? GetToolLabel(DraggingTool);
            var previewRect = new Rectangle(DragPosition.X - 30, DragPosition.Y - 20, 60, 40);
            spriteBatch.Draw(pixel, previewRect, DragPreviewColor);
            DrawBorder(spriteBatch, pixel, previewRect, BorderColor, 1);

            var labelSize = font.MeasureString(label);
            spriteBatch.DrawString(font, label,
                new Vector2(previewRect.X + (previewRect.Width - labelSize.X) / 2,
                           previewRect.Y + (previewRect.Height - labelSize.Y) / 2),
                TextColor);
        }
    }

    private Rectangle GetDeleteButtonRect(Rectangle itemRect)
    {
        return new Rectangle(
            itemRect.Right - DeleteButtonSize - 2,
            itemRect.Y + 2,
            DeleteButtonSize,
            DeleteButtonSize);
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
