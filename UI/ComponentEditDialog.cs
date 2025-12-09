using System;
using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Dialog for editing component title and pin titles.
/// </summary>
public class ComponentEditDialog
{
    public bool IsVisible { get; private set; }

    private Component? _component;
    private int _selectedFieldIndex; // 0 = component title, 1+ = pin titles
    private List<string> _fieldValues = new();
    private List<string> _fieldLabels = new();
    private int _hoveredFieldIndex = -1;
    private bool _saveButtonHovered;
    private bool _cancelButtonHovered;

    private const int DialogWidth = 400;
    private const int TitleHeight = 32;
    private const int FieldHeight = 28;
    private const int ButtonHeight = 32;
    private const int ButtonWidth = 100;
    private const int Padding = 12;
    private const int LabelWidth = 80;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color LabelColor = new(160, 160, 180);
    private static readonly Color FieldBgColor = new(35, 35, 45);
    private static readonly Color FieldSelectedColor = new(50, 60, 80);
    private static readonly Color FieldHoverColor = new(40, 45, 55);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color CancelButtonColor = new(100, 60, 60);
    private static readonly Color CancelButtonHoverColor = new(130, 80, 80);

    private Rectangle _bounds;
    private Rectangle _saveButtonRect;
    private Rectangle _cancelButtonRect;
    private List<Rectangle> _fieldRects = new();

    public event Action? OnSave;
    public event Action? OnCancel;

    public void Show(Component component)
    {
        _component = component;
        IsVisible = true;
        _selectedFieldIndex = 0;
        _hoveredFieldIndex = -1;

        // Build field list: component title + all pin titles
        _fieldLabels.Clear();
        _fieldValues.Clear();
        _fieldRects.Clear();

        _fieldLabels.Add(LocalizationManager.Get("dialog.component_title"));
        _fieldValues.Add(component.Title);

        foreach (var pin in component.Inputs)
        {
            _fieldLabels.Add($"{LocalizationManager.Get("dialog.input")} {pin.Name}:");
            _fieldValues.Add(pin.Title);
        }

        foreach (var pin in component.Outputs)
        {
            _fieldLabels.Add($"{LocalizationManager.Get("dialog.output")} {pin.Name}:");
            _fieldValues.Add(pin.Title);
        }
    }

    public void Hide()
    {
        IsVisible = false;
        _component = null;
        _fieldValues.Clear();
        _fieldLabels.Clear();
        _fieldRects.Clear();
        _selectedFieldIndex = 0;
    }

    public void HandleInput(InputState input, IInputHandler inputHandler)
    {
        if (!IsVisible || _fieldValues.Count == 0)
        {
            return;
        }

        // Handle escape to cancel
        if (input.EscapeCommand)
        {
            Hide();
            inputHandler.EndTextInput();
            OnCancel?.Invoke();
            return;
        }

        // Handle enter to save
        if (input.EnterPressed)
        {
            Save();
            inputHandler.EndTextInput();
            return;
        }

        // Handle tab to move between fields
        if (input.TabPressed)
        {
            if (input.ShiftHeld)
            {
                _selectedFieldIndex = (_selectedFieldIndex - 1 + _fieldValues.Count) % _fieldValues.Count;
            }
            else
            {
                _selectedFieldIndex = (_selectedFieldIndex + 1) % _fieldValues.Count;
            }
            return;
        }

        // Ensure selected field index is valid
        if (_selectedFieldIndex >= _fieldValues.Count)
        {
            _selectedFieldIndex = 0;
        }

        // Handle backspace
        if (input.BackspacePressed && _fieldValues[_selectedFieldIndex].Length > 0)
        {
            _fieldValues[_selectedFieldIndex] = _fieldValues[_selectedFieldIndex][..^1];
            return;
        }

        // Handle character input
        if (input.CharacterInput.HasValue && _fieldValues[_selectedFieldIndex].Length < 20)
        {
            _fieldValues[_selectedFieldIndex] += input.CharacterInput.Value;
        }
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _component == null || _fieldValues.Count == 0)
        {
            return;
        }

        // Calculate dialog height based on number of fields
        int contentHeight = _fieldValues.Count * (FieldHeight + 8) + ButtonHeight + Padding * 4;
        int dialogHeight = TitleHeight + contentHeight;

        // Calculate dialog position (centered)
        int dialogX = (screenWidth - DialogWidth) / 2;
        int dialogY = (screenHeight - dialogHeight) / 2;
        _bounds = new Rectangle(dialogX, dialogY, DialogWidth, dialogHeight);

        // Build field rectangles
        _fieldRects.Clear();
        int y = _bounds.Y + TitleHeight + Padding;
        for (int i = 0; i < _fieldValues.Count; i++)
        {
            var fieldRect = new Rectangle(
                _bounds.X + Padding + LabelWidth + 8,
                y,
                DialogWidth - Padding * 2 - LabelWidth - 8,
                FieldHeight);
            _fieldRects.Add(fieldRect);
            y += FieldHeight + 8;
        }

        // Check field hover/click
        _hoveredFieldIndex = -1;
        for (int i = 0; i < _fieldRects.Count; i++)
        {
            if (_fieldRects[i].Contains(mousePos))
            {
                _hoveredFieldIndex = i;
                if (mouseJustPressed)
                {
                    _selectedFieldIndex = i;
                }
            }
        }

        // Buttons
        int buttonY = _bounds.Bottom - Padding - ButtonHeight;
        _cancelButtonRect = new Rectangle(
            _bounds.X + Padding,
            buttonY,
            ButtonWidth,
            ButtonHeight);
        _saveButtonRect = new Rectangle(
            _bounds.Right - Padding - ButtonWidth,
            buttonY,
            ButtonWidth,
            ButtonHeight);

        _cancelButtonHovered = _cancelButtonRect.Contains(mousePos);
        _saveButtonHovered = _saveButtonRect.Contains(mousePos);

        if (mouseJustPressed)
        {
            if (_cancelButtonHovered)
            {
                Hide();
                OnCancel?.Invoke();
            }
            else if (_saveButtonHovered)
            {
                Save();
            }
        }
    }

    private void Save()
    {
        if (_component == null)
        {
            return;
        }

        // Apply values
        _component.Title = _fieldValues[0];

        int fieldIndex = 1;
        foreach (var pin in _component.Inputs)
        {
            if (fieldIndex < _fieldValues.Count)
            {
                pin.Title = _fieldValues[fieldIndex];
            }
            fieldIndex++;
        }

        foreach (var pin in _component.Outputs)
        {
            if (fieldIndex < _fieldValues.Count)
            {
                pin.Title = _fieldValues[fieldIndex];
            }
            fieldIndex++;
        }

        Hide();
        OnSave?.Invoke();
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight)
    {
        if (!IsVisible || _component == null || _fieldValues.Count == 0)
        {
            return;
        }

        // Draw overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Draw dialog background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 2);

        // Draw title bar
        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        var titleText = LocalizationManager.Get("dialog.edit_component");
        var titleSize = font.MeasureString(titleText);
        font.DrawText(spriteBatch, titleText,
            new Vector2(_bounds.X + Padding, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw fields
        int y = _bounds.Y + TitleHeight + Padding;
        for (int i = 0; i < _fieldValues.Count; i++)
        {
            // Skip if field rects not yet populated
            if (i >= _fieldRects.Count)
            {
                break;
            }

            // Draw label
            var labelText = _fieldLabels[i];
            var labelSize = font.MeasureString(labelText);
            font.DrawText(spriteBatch, labelText,
                new Vector2(_bounds.X + Padding, y + (FieldHeight - labelSize.Y) / 2),
                LabelColor);

            // Draw field background
            var fieldRect = _fieldRects[i];
            Color fieldBg;
            if (i == _selectedFieldIndex)
            {
                fieldBg = FieldSelectedColor;
            }
            else if (i == _hoveredFieldIndex)
            {
                fieldBg = FieldHoverColor;
            }
            else
            {
                fieldBg = FieldBgColor;
            }

            spriteBatch.Draw(pixel, fieldRect, fieldBg);
            DrawBorder(spriteBatch, pixel, fieldRect, BorderColor, 1);

            // Draw field text
            var fieldText = _fieldValues[i];
            if (i == _selectedFieldIndex)
            {
                fieldText += "_"; // Cursor
            }
            var fieldSize = font.MeasureString(fieldText);
            font.DrawText(spriteBatch, fieldText,
                new Vector2(fieldRect.X + 8, fieldRect.Y + (fieldRect.Height - fieldSize.Y) / 2),
                TextColor);

            y += FieldHeight + 8;
        }

        // Draw cancel button
        spriteBatch.Draw(pixel, _cancelButtonRect, _cancelButtonHovered ? CancelButtonHoverColor : CancelButtonColor);
        DrawBorder(spriteBatch, pixel, _cancelButtonRect, BorderColor, 1);
        var cancelText = LocalizationManager.Get("dialog.cancel");
        var cancelSize = font.MeasureString(cancelText);
        font.DrawText(spriteBatch, cancelText,
            new Vector2(_cancelButtonRect.X + (_cancelButtonRect.Width - cancelSize.X) / 2,
                       _cancelButtonRect.Y + (_cancelButtonRect.Height - cancelSize.Y) / 2),
            TextColor);

        // Draw save button
        spriteBatch.Draw(pixel, _saveButtonRect, _saveButtonHovered ? ButtonHoverColor : ButtonColor);
        DrawBorder(spriteBatch, pixel, _saveButtonRect, BorderColor, 1);
        var saveText = LocalizationManager.Get("dialog.save");
        var saveSize = font.MeasureString(saveText);
        font.DrawText(spriteBatch, saveText,
            new Vector2(_saveButtonRect.X + (_saveButtonRect.Width - saveSize.X) / 2,
                       _saveButtonRect.Y + (_saveButtonRect.Height - saveSize.Y) / 2),
            TextColor);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public bool ContainsPoint(Point p)
    {
        return IsVisible && _bounds.Contains(p);
    }
}
