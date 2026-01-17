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
/// Panel for editing pin names and viewing pin positions.
/// </summary>
public class PinEditorPanel : IPinEditorPanel
{
    private ComponentAppearance? _appearance;
    private int _selectedPinIndex = -1;
    private int _editingPinIndex = -1;
    private bool _editingPinIsInput;
    private string _editingFieldText = "";

    public bool IsEditingPinName => _editingPinIndex >= 0;

    public event Action? OnPinNameChanged;

    public void SetAppearance(ComponentAppearance? appearance)
    {
        _appearance = appearance;
    }

    public void SetSelectedPinIndex(int index)
    {
        _selectedPinIndex = index;
    }

    public void Update(Point mousePos, bool mouseJustPressed, Rectangle bounds, IFontService fontService)
    {
        if (_appearance == null || !bounds.Contains(mousePos))
        {
            return;
        }

        if (!mouseJustPressed)
        {
            return;
        }

        // Calculate pin name positions (must match Draw layout exactly)
        int x = bounds.X + DesignerLayout.Padding;
        int y = bounds.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding;
        int pinIndex = 0;

        // Get font for measuring text
        var font = fontService.GetFont(1.0f);

        // Check input pins
        foreach (var pin in _appearance.InputPins)
        {
            // Measure just the pin name (without coordinates)
            var nameSize = font.MeasureString(pin.Name);
            var nameRect = new Rectangle(x, y, (int)nameSize.X, (int)nameSize.Y);

            if (nameRect.Contains(mousePos))
            {
                // Start editing this pin name
                _editingPinIndex = pinIndex;
                _editingPinIsInput = true;
                _editingFieldText = pin.Name;
                return;
            }

            x += 150;
            if (x > bounds.Right - 150)
            {
                x = bounds.X + DesignerLayout.Padding;
                y += 20;
            }
            pinIndex++;
        }

        // Check output pins
        foreach (var pin in _appearance.OutputPins)
        {
            // Measure just the pin name (without coordinates)
            var nameSize = font.MeasureString(pin.Name);
            var nameRect = new Rectangle(x, y, (int)nameSize.X, (int)nameSize.Y);

            if (nameRect.Contains(mousePos))
            {
                // Start editing this pin name
                _editingPinIndex = pinIndex;
                _editingPinIsInput = false;
                _editingFieldText = pin.Name;
                return;
            }

            x += 150;
            if (x > bounds.Right - 150)
            {
                x = bounds.X + DesignerLayout.Padding;
                y += 20;
            }
            pinIndex++;
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

        var headerText = LocalizationManager.Get("designer.pin_positions");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(
            spriteBatch,
            headerText,
            new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (DesignerLayout.HeaderHeight - headerSize.Y) / 2),
            DesignerColors.TextColor);

        if (_appearance == null)
        {
            return;
        }

        // Draw pin list
        int x = rect.X + DesignerLayout.Padding;
        int y = rect.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding;
        int pinIndex = 0;

        foreach (var pin in _appearance.InputPins.Concat(_appearance.OutputPins))
        {
            bool isSelected = pinIndex == _selectedPinIndex;
            bool isEditing = _editingPinIndex == pinIndex;

            if (isEditing)
            {
                // Draw pin name as editable field
                var nameSize = font.MeasureString(_editingFieldText);
                var nameRect = new Rectangle(x, y - 2, (int)nameSize.X + 8, 18);
                DesignerDrawing.DrawTextField(spriteBatch, pixel, font, nameRect, _editingFieldText, true);

                // Draw coordinates after the editable field
                var coordText = $": X={pin.LocalX}, Y={pin.LocalY}";
                font.DrawText(spriteBatch, coordText, new Vector2(nameRect.Right + 2, y), DesignerColors.TextColor);
            }
            else
            {
                // Draw pin name (clickable)
                var nameSize = font.MeasureString(pin.Name);
                var nameRect = new Rectangle(x, y - 2, (int)nameSize.X, 18);

                // Highlight if selected
                if (isSelected)
                {
                    spriteBatch.Draw(pixel, nameRect, new Color(70, 100, 140, 100));
                }

                font.DrawText(
                    spriteBatch,
                    pin.Name,
                    new Vector2(x, y),
                    isSelected ? DesignerColors.PinSelectedColor : DesignerColors.TextColor);

                // Draw coordinates
                var coordText = $": X={pin.LocalX}, Y={pin.LocalY}";
                font.DrawText(spriteBatch, coordText, new Vector2(x + nameSize.X, y), DesignerColors.DimTextColor);
            }

            x += 150;
            if (x > rect.Right - 150)
            {
                x = rect.X + DesignerLayout.Padding;
                y += 20;
            }
            pinIndex++;
        }
    }

    public void HandleTextInput(char character)
    {
        if (_editingPinIndex < 0)
        {
            return;
        }

        // Pin name accepts alphanumeric and underscore
        if (char.IsLetterOrDigit(character) || character == '_')
        {
            _editingFieldText += character;
        }
    }

    public void HandleKeyPress(bool backspace, bool enter, bool escape)
    {
        if (_editingPinIndex < 0)
        {
            return;
        }

        if (backspace && _editingFieldText.Length > 0)
        {
            _editingFieldText = _editingFieldText.Substring(0, _editingFieldText.Length - 1);
        }
        else if (enter)
        {
            ApplyPinName();
            _editingPinIndex = -1;
        }
        else if (escape)
        {
            _editingPinIndex = -1;
        }
    }

    public void HandlePaste(string text)
    {
        if (_editingPinIndex < 0 || string.IsNullOrEmpty(text))
        {
            return;
        }

        // Pin name accepts alphanumeric and underscore
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                _editingFieldText += c;
            }
        }
    }

    public void Reset()
    {
        _appearance = null;
        _selectedPinIndex = -1;
        _editingPinIndex = -1;
        _editingFieldText = "";
    }

    private void ApplyPinName()
    {
        if (_appearance == null || _editingPinIndex < 0)
        {
            return;
        }

        string newName = _editingFieldText.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            return;
        }

        // Check for duplicate names
        bool isDuplicate = false;
        foreach (var pin in _appearance.InputPins.Concat(_appearance.OutputPins))
        {
            if (pin.Name == newName && pin != GetEditingPin())
            {
                isDuplicate = true;
                break;
            }
        }

        // Only apply if not duplicate
        if (!isDuplicate)
        {
            if (_editingPinIsInput)
            {
                _appearance.InputPins[_editingPinIndex].Name = newName;
            }
            else
            {
                int outputIndex = _editingPinIndex - _appearance.InputPins.Count;
                _appearance.OutputPins[outputIndex].Name = newName;
            }
            OnPinNameChanged?.Invoke();
        }
    }

    private PinAppearance? GetEditingPin()
    {
        if (_appearance == null || _editingPinIndex < 0)
        {
            return null;
        }

        if (_editingPinIsInput)
        {
            return _editingPinIndex < _appearance.InputPins.Count ? _appearance.InputPins[_editingPinIndex] : null;
        }
        else
        {
            int outputIndex = _editingPinIndex - _appearance.InputPins.Count;
            return outputIndex < _appearance.OutputPins.Count ? _appearance.OutputPins[outputIndex] : null;
        }
    }

}
