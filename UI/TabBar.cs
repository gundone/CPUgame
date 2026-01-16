using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Tab bar for managing multiple open editor tabs
/// </summary>
public class TabBar
{
    private readonly List<EditorTab> _tabs = new();
    private int _activeTabIndex = 0;
    private int _hoveredTabIndex = -1;
    private int _hoveredCloseButtonIndex = -1;

    private const int TabHeight = 32;
    private const int TabMinWidth = 100;
    private const int TabMaxWidth = 200;
    private const int TabPadding = 8;
    private const int CloseButtonSize = 16;
    private const int CloseButtonPadding = 4;
    private const int TrapezoidInset = 8; // How much the bottom is inset from top
    private const int CornerRadius = 6; // Radius for rounded top corners

    private static readonly Color TabActiveColor = new(60, 60, 75);
    private static readonly Color TabInactiveColor = new(45, 45, 55);
    private static readonly Color TabHoverColor = new(55, 55, 70);
    private static readonly Color TabBorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color DirtyColor = new(255, 180, 100);
    private static readonly Color CloseButtonColor = new(180, 180, 190);
    private static readonly Color CloseButtonHoverColor = new(220, 100, 100);

    public event Action<int>? OnTabSelected;
    public event Action<int>? OnTabCloseRequested;

    public int ActiveTabIndex => _activeTabIndex;
    public EditorTab? ActiveTab => _tabs.Count > 0 && _activeTabIndex >= 0 && _activeTabIndex < _tabs.Count ? _tabs[_activeTabIndex] : null;
    public IReadOnlyList<EditorTab> Tabs => _tabs;
    public bool IsVisible => _tabs.Count > 1; // Only show tab bar when multiple tabs exist

    public void AddTab(EditorTab tab)
    {
        _tabs.Add(tab);
        _activeTabIndex = _tabs.Count - 1;
        OnTabSelected?.Invoke(_activeTabIndex);
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            return;
        }

        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            _activeTabIndex = -1;
            OnTabSelected?.Invoke(-1);
        }
        else if (_activeTabIndex >= _tabs.Count)
        {
            _activeTabIndex = _tabs.Count - 1;
            OnTabSelected?.Invoke(_activeTabIndex);
        }
        else if (_activeTabIndex == index && _activeTabIndex > 0)
        {
            _activeTabIndex--;
            OnTabSelected?.Invoke(_activeTabIndex);
        }
        else
        {
            OnTabSelected?.Invoke(_activeTabIndex);
        }
    }

    public void SetActiveTab(int index)
    {
        if (index >= 0 && index < _tabs.Count && index != _activeTabIndex)
        {
            _activeTabIndex = index;
            OnTabSelected?.Invoke(_activeTabIndex);
        }
    }

    public EditorTab? GetTab(int index)
    {
        return index >= 0 && index < _tabs.Count ? _tabs[index] : null;
    }

    public void Clear()
    {
        _tabs.Clear();
        _activeTabIndex = -1;
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int yOffset = 0)
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        _hoveredTabIndex = -1;
        _hoveredCloseButtonIndex = -1;

        int x = 0;
        int y = yOffset;

        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            int tabWidth = CalculateTabWidth(tab.GetDisplayName(), screenWidth);

            // Check if mouse is within trapezoid bounds
            bool inTrapezoid = IsPointInTrapezoid(mousePos, x, y, tabWidth, TabHeight);

            var closeButtonRect = new Rectangle(
                x + tabWidth - CloseButtonSize - CloseButtonPadding - TrapezoidInset,
                y + (TabHeight - CloseButtonSize) / 2,
                CloseButtonSize,
                CloseButtonSize
            );

            if (inTrapezoid)
            {
                if (closeButtonRect.Contains(mousePos))
                {
                    _hoveredCloseButtonIndex = i;
                }
                else
                {
                    _hoveredTabIndex = i;
                }
            }

            x += tabWidth;
        }

        if (mouseJustPressed)
        {
            if (_hoveredCloseButtonIndex >= 0)
            {
                OnTabCloseRequested?.Invoke(_hoveredCloseButtonIndex);
            }
            else if (_hoveredTabIndex >= 0)
            {
                SetActiveTab(_hoveredTabIndex);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int yOffset = 0)
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        int x = 0;
        int y = yOffset;

        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            bool isActive = i == _activeTabIndex;
            bool isHovered = i == _hoveredTabIndex;
            int tabWidth = CalculateTabWidth(tab.GetDisplayName(), screenWidth);

            // Draw trapezoid tab background
            Color tabColor = isActive ? TabActiveColor : (isHovered ? TabHoverColor : TabInactiveColor);
            DrawTrapezoidTab(spriteBatch, pixel, x, y, tabWidth, TabHeight, tabColor, TabBorderColor);

            // Draw tab title with dirty indicator
            string displayName = tab.GetDisplayName();
            if (tab is { IsDirty: true, IsMainCircuit: false })
            {
                displayName = "• " + displayName;
            }

            var textSize = font.MeasureString(displayName);
            var textPos = new Vector2(
                x + TabPadding + TrapezoidInset,
                y + (TabHeight - textSize.Y) / 2
            );
            var textColor = tab.IsDirty ? DirtyColor : TextColor;
            font.DrawText(spriteBatch, displayName, textPos, textColor);

            // Draw close button (X) for non-main tabs
            if (!tab.IsMainCircuit)
            {
                var closeButtonRect = new Rectangle(
                    x + tabWidth - CloseButtonSize - CloseButtonPadding - TrapezoidInset,
                    y + (TabHeight - CloseButtonSize) / 2,
                    CloseButtonSize,
                    CloseButtonSize
                );

                bool isCloseHovered = i == _hoveredCloseButtonIndex;
                Color closeColor = isCloseHovered ? CloseButtonHoverColor : CloseButtonColor;

                // Draw X
                int crossPadding = 4;
                DrawLine(spriteBatch, pixel,
                    closeButtonRect.X + crossPadding,
                    closeButtonRect.Y + crossPadding,
                    closeButtonRect.Right - crossPadding,
                    closeButtonRect.Bottom - crossPadding,
                    closeColor, 2);
                DrawLine(spriteBatch, pixel,
                    closeButtonRect.Right - crossPadding,
                    closeButtonRect.Y + crossPadding,
                    closeButtonRect.X + crossPadding,
                    closeButtonRect.Bottom - crossPadding,
                    closeColor, 2);
            }

            x += tabWidth;
        }
    }

    private int CalculateTabWidth(string name, int screenWidth)
    {
        int availableWidth = screenWidth / Math.Max(1, _tabs.Count);
        return Math.Clamp(availableWidth, TabMinWidth, TabMaxWidth);
    }

    private static bool IsPointInTrapezoid(Point point, int x, int y, int width, int height)
    {
        // Check if point is within the trapezoid shape (narrow at top, wide at bottom)
        if (point.Y < y || point.Y >= y + height)
        {
            return false;
        }

        // Calculate the current inset based on Y position
        // At top: inset = TrapezoidInset (narrow)
        // At bottom: inset = 0 (wide)
        float progress = (float)(point.Y - y - CornerRadius) / (height - CornerRadius);
        if (progress < 0)
        {
            progress = 0;
        }

        int currentInset = TrapezoidInset - (int)(progress * TrapezoidInset); // Decrease inset as we go down
        int leftBound = x + currentInset;
        int rightBound = x + width - currentInset;

        return point.X >= leftBound && point.X < rightBound;
    }

    private static void DrawTrapezoidTab(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, int width, int height, Color fillColor, Color borderColor)
    {
        // Trapezoid shape: narrower at top, wider at bottom
        // Top width: width - (2 * TrapezoidInset)
        // Bottom width: full width

        int topInset = TrapezoidInset;

        // Draw rounded top corners at the narrow top
        int topWidth = width - 2 * topInset;
        int topX = x + topInset;

        // Draw rounded top-left corner
        for (int cy = 0; cy < CornerRadius; cy++)
        {
            for (int cx = 0; cx < CornerRadius; cx++)
            {
                float dx = CornerRadius - cx;
                float dy = CornerRadius - cy;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= CornerRadius)
                {
                    spriteBatch.Draw(pixel, new Rectangle(topX + cx, y + cy, 1, 1), fillColor);
                }
            }
        }

        // Draw rounded top-right corner (adjusted for trapezoid angle)
        int rightCornerAdjust = 4; // Shift center left to align with angled edge
        for (int cy = 0; cy < CornerRadius+2; cy++)
        {
            for (int cx = 0; cx < CornerRadius; cx++)
            {
                float dx = cx;
                float dy = CornerRadius - cy;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= CornerRadius)
                {
                    spriteBatch.Draw(pixel, new Rectangle(topX + topWidth - CornerRadius + cx - rightCornerAdjust, y + cy, 1, 1), fillColor);
                }
            }
        }

        // Draw top bar (between rounded corners, adjusted for right corner shift)
        spriteBatch.Draw(pixel, new Rectangle(topX + CornerRadius, y, topWidth - 2 * CornerRadius - rightCornerAdjust, CornerRadius), fillColor);

        // Draw trapezoid body (expanding from narrow top to wide bottom)
        int cornerStartY = y + CornerRadius;
        for (int row = cornerStartY; row < y + height; row++)
        {
            float progress = (float)(row - cornerStartY) / (height - CornerRadius);
            int currentInset = topInset - (int)(progress * topInset); // Decrease inset as we go down
            int currentWidth = width - 2 * currentInset;
            int currentX = x + currentInset;

            spriteBatch.Draw(pixel, new Rectangle(currentX, row, currentWidth, 1), fillColor);
        }

        // Draw border
        // Left edge (angled outward)
        for (int row = y + CornerRadius; row < y + height; row++)
        {
            float progress = (float)(row - y - CornerRadius) / (height - CornerRadius);
            int currentInset = topInset - (int)(progress * topInset);
            spriteBatch.Draw(pixel, new Rectangle(x + currentInset, row, 1, 1), borderColor);
        }

        // Right edge (angled outward)
        for (int row = y + CornerRadius - 1; row < y + height; row++)
        {
            float progress = (float)(row - y - CornerRadius) / (height - CornerRadius);
            int currentInset = topInset - (int)(progress * topInset);
            spriteBatch.Draw(pixel, new Rectangle(x + width - currentInset - 2, row, 1, 1), borderColor);
        }

        // Top edge with rounded corners (border)
        // Top-left corner border
        for (int angle = 180; angle <= 270; angle++)
        {
            float rad = angle * (float)Math.PI / 180f;
            int cx = topX + CornerRadius + (int)(Math.Cos(rad) * CornerRadius);
            int cy = y + CornerRadius + (int)(Math.Sin(rad) * CornerRadius);
            spriteBatch.Draw(pixel, new Rectangle(cx, cy, 1, 1), borderColor);
        }

        // Top-right corner border (adjusted for trapezoid angle)
        for (int angle = 270; angle <= 350; angle++)
        {
            float rad = angle * (float)Math.PI / 180f;
            int cx = 1 + topX + topWidth - CornerRadius + 1  - rightCornerAdjust + (int)(Math.Cos(rad) * CornerRadius);
            int cy = -1 + y + CornerRadius + (int)(Math.Sin(rad) * CornerRadius);
            spriteBatch.Draw(pixel, new Rectangle(cx, cy, 1, 1), borderColor);
        }

        // Top edge (between corners, adjusted for right corner shift)
        spriteBatch.Draw(pixel, new Rectangle(topX + CornerRadius +2 , y, topWidth - 2 * CornerRadius - rightCornerAdjust, 1), borderColor);

        // Bottom edge (full width)
        int bottomY = y + height - 1;
        spriteBatch.Draw(pixel, new Rectangle(x, bottomY, width, 1), borderColor);
    }

    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, int x1, int y1, int x2, int y2, Color color, int thickness)
    {
        var direction = new Vector2(x2 - x1, y2 - y1);
        float length = direction.Length();
        float angle = (float)Math.Atan2(direction.Y, direction.X);

        spriteBatch.Draw(pixel,
            new Rectangle(x1, y1, (int)length, thickness),
            null,
            color,
            angle,
            new Vector2(0, 0),
            SpriteEffects.None,
            0);
    }

    public int GetHeight()
    {
        return _tabs.Count > 1 ? TabHeight : 0; // Only show height when multiple tabs
    }

    public bool ContainsPoint(Point mousePos, int yOffset = 0)
    {
        if (_tabs.Count == 0)
        {
            return false;
        }

        int y = yOffset;
        return mousePos.Y >= y && mousePos.Y < y + TabHeight;
    }
}
