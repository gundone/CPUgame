using System;
using System.Linq;
using CPUgame.Core.Designer;
using CPUgame.Core.Localization;
using CPUgame.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Panel for visual preview and pin/title dragging in the designer.
/// </summary>
public class PreviewPanel : IPreviewPanel
{
    private ComponentAppearance? _appearance;
    private string? _componentType;
    private int _selectedPinIndex = -1;
    private bool _isDraggingPin;
    private bool _isDraggingTitle;
    private Rectangle _titleRect;

    public int SelectedPinIndex => _selectedPinIndex;

    public event Action<int>? OnPinSelected;
    public event Action? OnAppearanceChanged;

    public void SetAppearance(ComponentAppearance? appearance, string? componentType)
    {
        _appearance = appearance;
        _componentType = componentType;
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, Rectangle bounds)
    {
        if (_appearance == null)
        {
            return;
        }

        // Calculate component position in preview
        int compWidth = _appearance.Width * DesignerLayout.PreviewScale;
        int compHeight = _appearance.Height * DesignerLayout.PreviewScale;
        int compX = bounds.X + (bounds.Width - compWidth) / 2;
        int compY = bounds.Y + (bounds.Height - compHeight) / 2;

        // Check for click on title or pins
        if (mouseJustPressed && bounds.Contains(mousePos))
        {
            // Check title first (stored in _titleRect from last draw)
            if (_titleRect.Contains(mousePos))
            {
                _isDraggingTitle = true;
                _selectedPinIndex = -1;
                OnPinSelected?.Invoke(-1);
            }
            else
            {
                // Check all pins
                int pinIndex = 0;
                foreach (var pin in _appearance.InputPins.Concat(_appearance.OutputPins))
                {
                    int pinX = compX + pin.LocalX * DesignerLayout.PreviewScale;
                    int pinY = compY + pin.LocalY * DesignerLayout.PreviewScale;
                    int pinRadius = 8 * DesignerLayout.PreviewScale;

                    if (Math.Abs(mousePos.X - pinX) < pinRadius && Math.Abs(mousePos.Y - pinY) < pinRadius)
                    {
                        _selectedPinIndex = pinIndex;
                        _isDraggingPin = true;
                        _isDraggingTitle = false;
                        OnPinSelected?.Invoke(pinIndex);
                        break;
                    }
                    pinIndex++;
                }
            }
        }

        // Handle title dragging (no grid snap)
        if (_isDraggingTitle && mousePressed)
        {
            // Calculate new title offset relative to component center
            int centerX = compX + compWidth / 2;
            int centerY = compY + compHeight / 2;

            // Convert mouse position to local coordinates (in game pixels, not preview pixels)
            _appearance.TitleOffsetX = (mousePos.X - centerX) / DesignerLayout.PreviewScale;
            _appearance.TitleOffsetY = (mousePos.Y - centerY) / DesignerLayout.PreviewScale;
            OnAppearanceChanged?.Invoke();
        }

        // Handle pin dragging
        if (_isDraggingPin && mousePressed && _selectedPinIndex >= 0)
        {
            // Update pin position
            int localX = (mousePos.X - compX) / DesignerLayout.PreviewScale;
            int localY = (mousePos.Y - compY) / DesignerLayout.PreviewScale;

            // Snap to grid (use game's grid size)
            localX = (int)Math.Round((double)localX / DesignerLayout.GridSize) * DesignerLayout.GridSize;
            localY = (int)Math.Round((double)localY / DesignerLayout.GridSize) * DesignerLayout.GridSize;

            // Clamp to component bounds
            localX = Math.Clamp(localX, 0, _appearance.Width);
            localY = Math.Clamp(localY, 0, _appearance.Height);

            // Update the pin
            int inputCount = _appearance.InputPins.Count;
            if (_selectedPinIndex < inputCount)
            {
                _appearance.InputPins[_selectedPinIndex].LocalX = localX;
                _appearance.InputPins[_selectedPinIndex].LocalY = localY;
            }
            else
            {
                int outputIndex = _selectedPinIndex - inputCount;
                _appearance.OutputPins[outputIndex].LocalX = localX;
                _appearance.OutputPins[outputIndex].LocalY = localY;
            }
            OnAppearanceChanged?.Invoke();
        }

        if (mouseJustReleased)
        {
            _isDraggingPin = false;
            _isDraggingTitle = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect, IFontService fontService)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, DesignerColors.PanelColor);
        DesignerDrawing.DrawBorder(spriteBatch, pixel, rect, DesignerColors.BorderColor, 1);

        if (_appearance == null)
        {
            // Show hint
            var hint = LocalizationManager.Get("designer.select_component");
            var hintSize = font.MeasureString(hint);
            font.DrawText(
                spriteBatch,
                hint,
                new Vector2(rect.X + (rect.Width - hintSize.X) / 2, rect.Y + (rect.Height - hintSize.Y) / 2),
                DesignerColors.DimTextColor);
            return;
        }

        // Calculate component position first (needed for grid alignment)
        int compWidth = _appearance.Width * DesignerLayout.PreviewScale;
        int compHeight = _appearance.Height * DesignerLayout.PreviewScale;
        int compX = rect.X + (rect.Width - compWidth) / 2;
        int compY = rect.Y + (rect.Height - compHeight) / 2;

        // Draw grid aligned with component position
        int gridStep = DesignerLayout.GridSize * DesignerLayout.PreviewScale;

        // Draw vertical grid lines (aligned with component's left edge)
        int startX = compX - ((compX - rect.X) / gridStep + 1) * gridStep;
        for (int x = startX; x < rect.Right; x += gridStep)
        {
            if (x >= rect.X)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, rect.Y, 1, rect.Height), DesignerColors.GridColor);
            }
        }

        // Draw horizontal grid lines (aligned with component's top edge)
        int startY = compY - ((compY - rect.Y) / gridStep + 1) * gridStep;
        for (int y = startY; y < rect.Bottom; y += gridStep)
        {
            if (y >= rect.Y)
            {
                spriteBatch.Draw(pixel, new Rectangle(rect.X, y, rect.Width, 1), DesignerColors.GridColor);
            }
        }

        // Draw component body with selected fill color
        var fillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, new Rectangle(compX, compY, compWidth, compHeight), fillColor);
        DesignerDrawing.DrawBorder(spriteBatch, pixel, new Rectangle(compX, compY, compWidth, compHeight), DesignerColors.BorderColor, 2);

        // Draw title (use custom title if set, otherwise component type)
        string displayName = GetDisplayName();

        // Use font with scale based on both preview scale and title font scale
        float titleFontScale = DesignerLayout.PreviewScale * _appearance.TitleFontScale;
        var previewFont = fontService.GetFont(titleFontScale);
        var nameSize = previewFont.MeasureString(displayName);

        // Calculate title position with offset (offset is in game pixels, convert to preview)
        float centerX = compX + compWidth / 2f;
        float centerY = compY + compHeight / 2f;
        float nameX = centerX - nameSize.X / 2 + _appearance.TitleOffsetX * DesignerLayout.PreviewScale;
        float nameY = centerY - nameSize.Y / 2 + _appearance.TitleOffsetY * DesignerLayout.PreviewScale;

        // Store title rect for hit detection (with some padding for easier dragging)
        int titlePadding = 4;
        _titleRect = new Rectangle(
            (int)nameX - titlePadding,
            (int)nameY - titlePadding,
            (int)nameSize.X + titlePadding * 2,
            (int)nameSize.Y + titlePadding * 2);

        // Highlight title if dragging
        if (_isDraggingTitle)
        {
            spriteBatch.Draw(pixel, _titleRect, new Color(100, 150, 200, 80));
            DesignerDrawing.DrawBorder(spriteBatch, pixel, _titleRect, DesignerColors.PinSelectedColor, 1);
        }

        previewFont.DrawText(spriteBatch, displayName, new Vector2(nameX, nameY), DesignerColors.TextColor);

        // Draw pins (use preview font without title scale)
        var pinFont = fontService.GetFont(DesignerLayout.PreviewScale);
        int pinIndex = 0;
        foreach (var pin in _appearance.InputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * DesignerLayout.PreviewScale,
                compY + pin.LocalY * DesignerLayout.PreviewScale, pin.Name, pinIndex == _selectedPinIndex, true);
            pinIndex++;
        }
        foreach (var pin in _appearance.OutputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * DesignerLayout.PreviewScale,
                compY + pin.LocalY * DesignerLayout.PreviewScale, pin.Name, pinIndex == _selectedPinIndex, false);
            pinIndex++;
        }

        // Draw hint
        var dragHint = LocalizationManager.Get("designer.drag_pins");
        var dragHintSize = font.MeasureString(dragHint);
        font.DrawText(
            spriteBatch,
            dragHint,
            new Vector2(rect.X + (rect.Width - dragHintSize.X) / 2, rect.Bottom - dragHintSize.Y - DesignerLayout.Padding),
            DesignerColors.DimTextColor);
    }

    public void Reset()
    {
        _appearance = null;
        _componentType = null;
        _selectedPinIndex = -1;
        _isDraggingPin = false;
        _isDraggingTitle = false;
    }

    private string GetDisplayName()
    {
        if (_appearance == null)
        {
            return "";
        }

        if (!string.IsNullOrEmpty(_appearance.Title))
        {
            return _appearance.Title;
        }

        if (_componentType?.StartsWith("Custom:") == true)
        {
            return _componentType.Substring(7);
        }

        return _componentType ?? "";
    }

    private Color GetCurrentFillColor()
    {
        if (_appearance?.FillColor == null)
        {
            return DesignerColors.PresetColors[0].color;
        }
        return DesignerColors.HexToColor(_appearance.FillColor);
    }

    private static void DrawPin(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font,
        int x, int y, string name, bool isSelected, bool isInput)
    {
        // Scale pin size to match preview scale
        int basePinSize = isSelected ? 12 : 8;
        int pinSize = basePinSize * DesignerLayout.PreviewScale / 2; // Scale but not too large
        var pinColor = isSelected ? DesignerColors.PinSelectedColor : DesignerColors.PinColor;

        spriteBatch.Draw(pixel, new Rectangle(x - pinSize / 2, y - pinSize / 2, pinSize, pinSize), pinColor);

        // Draw pin name (font is already at preview scale size)
        var nameSize = font.MeasureString(name);
        int offset = 8 * DesignerLayout.PreviewScale;
        float nameX = isInput ? x - nameSize.X - offset : x + offset;
        float nameY = y - nameSize.Y / 2;
        font.DrawText(spriteBatch, name, new Vector2(nameX, nameY), DesignerColors.TextColor);
    }
}
