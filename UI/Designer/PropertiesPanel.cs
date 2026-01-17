using System;
using CPUgame.Core.Designer;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Panel for editing component properties (width, height, title, color).
/// </summary>
public class PropertiesPanel : IPropertiesPanel
{
    private const int FieldWidth = 0;
    private const int FieldHeight = 1;
    private const int FieldTitle = 2;
    private const int FieldFontScale = 3;
    private const int FieldCustomColor = 4;

    private ComponentAppearance? _appearance;
    private int _editingFieldIndex = -1;
    private string _editingFieldText = "";

    public bool IsEditing => _editingFieldIndex >= 0;

    public event Action? OnAppearanceChanged;
    public event Action<Rectangle>? OnContextMenuRequested;

    public void SetAppearance(ComponentAppearance? appearance)
    {
        _appearance = appearance;
    }

    public void Update(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed,
        Rectangle bounds, int screenWidth, int screenHeight)
    {
        if (!bounds.Contains(mousePos) || _appearance == null)
        {
            return;
        }

        // Calculate property item positions
        int y = bounds.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding;
        int fieldHeight = 26;
        int spacing = 4;
        int labelWidth = bounds.Width / 2 - DesignerLayout.Padding;

        // Width field (display in grid cells)
        var widthRect = new Rectangle(bounds.X + labelWidth + DesignerLayout.Padding, y, bounds.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        if (widthRect.Contains(mousePos))
        {
            if (mouseJustPressed && _editingFieldIndex != FieldWidth)
            {
                StartEditing(FieldWidth, (_appearance.Width / DesignerLayout.GridSize).ToString());
            }
            else if (rightMouseJustPressed)
            {
                StartEditing(FieldWidth, (_appearance.Width / DesignerLayout.GridSize).ToString());
                RequestContextMenu(mousePos, screenWidth, screenHeight);
            }
        }
        y += fieldHeight + spacing;

        // Height field (display in grid cells)
        var heightRect = new Rectangle(bounds.X + labelWidth + DesignerLayout.Padding, y, bounds.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        if (heightRect.Contains(mousePos))
        {
            if (mouseJustPressed && _editingFieldIndex != FieldHeight)
            {
                StartEditing(FieldHeight, (_appearance.Height / DesignerLayout.GridSize).ToString());
            }
            else if (rightMouseJustPressed)
            {
                StartEditing(FieldHeight, (_appearance.Height / DesignerLayout.GridSize).ToString());
                RequestContextMenu(mousePos, screenWidth, screenHeight);
            }
        }
        y += fieldHeight + spacing;

        // Title field
        var titleRect = new Rectangle(bounds.X + labelWidth + DesignerLayout.Padding, y, bounds.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        if (titleRect.Contains(mousePos))
        {
            if (mouseJustPressed && _editingFieldIndex != FieldTitle)
            {
                StartEditing(FieldTitle, _appearance.Title);
            }
            else if (rightMouseJustPressed)
            {
                StartEditing(FieldTitle, _appearance.Title);
                RequestContextMenu(mousePos, screenWidth, screenHeight);
            }
        }
        y += fieldHeight + spacing;

        // Font scale field
        var fontScaleRect = new Rectangle(bounds.X + labelWidth + DesignerLayout.Padding, y, bounds.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        if (fontScaleRect.Contains(mousePos))
        {
            if (mouseJustPressed && _editingFieldIndex != FieldFontScale)
            {
                StartEditing(FieldFontScale, _appearance.TitleFontScale.ToString("0.0"));
            }
            else if (rightMouseJustPressed)
            {
                StartEditing(FieldFontScale, _appearance.TitleFontScale.ToString("0.0"));
                RequestContextMenu(mousePos, screenWidth, screenHeight);
            }
        }
        y += fieldHeight + spacing;

        // Color label row
        y += fieldHeight + spacing;

        // Color swatches
        int swatchSize = 20;
        int swatchSpacing = 4;
        int swatchesPerRow = (bounds.Width - DesignerLayout.Padding * 2) / (swatchSize + swatchSpacing);
        int swatchX = bounds.X + DesignerLayout.Padding;

        for (int i = 0; i < DesignerColors.PresetColors.Length; i++)
        {
            var swatchRect = new Rectangle(swatchX, y, swatchSize, swatchSize);
            if (swatchRect.Contains(mousePos) && mouseJustPressed)
            {
                if (i == 0)
                {
                    _appearance.FillColor = null; // Default
                }
                else
                {
                    _appearance.FillColor = DesignerColors.ColorToHex(DesignerColors.PresetColors[i].color);
                }
                _editingFieldIndex = -1; // Stop editing custom color field
                OnAppearanceChanged?.Invoke();
            }

            swatchX += swatchSize + swatchSpacing;
            if ((i + 1) % swatchesPerRow == 0)
            {
                swatchX = bounds.X + DesignerLayout.Padding;
                y += swatchSize + swatchSpacing;
            }
        }

        // Custom color row (after swatches)
        // Ensure we're on a new row
        if (swatchX != bounds.X + DesignerLayout.Padding)
        {
            y += swatchSize + swatchSpacing;
        }
        y += DesignerLayout.Padding;

        // Custom color field (adjust position to account for preview swatch)
        int previewSize = fieldHeight - 4;
        var customColorRect = new Rectangle(bounds.X + DesignerLayout.Padding + previewSize + 4, y, bounds.Width - DesignerLayout.Padding * 2 - previewSize - 4, fieldHeight);
        if (customColorRect.Contains(mousePos))
        {
            string currentHex = _appearance.FillColor ?? "#3C3C46";
            if (mouseJustPressed && _editingFieldIndex != FieldCustomColor)
            {
                StartEditing(FieldCustomColor, currentHex);
            }
            else if (rightMouseJustPressed)
            {
                StartEditing(FieldCustomColor, currentHex);
                RequestContextMenu(mousePos, screenWidth, screenHeight);
            }
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

        var headerText = LocalizationManager.Get("designer.properties");
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

        int y = rect.Y + DesignerLayout.HeaderHeight + DesignerLayout.Padding;
        int labelWidth = rect.Width / 2 - DesignerLayout.Padding;
        int fieldHeight = 26;
        int spacing = 4;

        // Width (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.width"), new Vector2(rect.X + DesignerLayout.Padding, y + 4), DesignerColors.TextColor);
        var widthRect = new Rectangle(rect.X + labelWidth + DesignerLayout.Padding, y, rect.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        string widthText = _editingFieldIndex == FieldWidth ? _editingFieldText : (_appearance.Width / DesignerLayout.GridSize).ToString();
        DesignerDrawing.DrawTextField(spriteBatch, pixel, font, widthRect, widthText, _editingFieldIndex == FieldWidth);
        y += fieldHeight + spacing;

        // Height (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.height"), new Vector2(rect.X + DesignerLayout.Padding, y + 4), DesignerColors.TextColor);
        var heightRect = new Rectangle(rect.X + labelWidth + DesignerLayout.Padding, y, rect.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        string heightText = _editingFieldIndex == FieldHeight ? _editingFieldText : (_appearance.Height / DesignerLayout.GridSize).ToString();
        DesignerDrawing.DrawTextField(spriteBatch, pixel, font, heightRect, heightText, _editingFieldIndex == FieldHeight);
        y += fieldHeight + spacing;

        // Title
        font.DrawText(spriteBatch, "Title", new Vector2(rect.X + DesignerLayout.Padding, y + 4), DesignerColors.TextColor);
        var titleRect = new Rectangle(rect.X + labelWidth + DesignerLayout.Padding, y, rect.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        string titleText = _editingFieldIndex == FieldTitle ? _editingFieldText : _appearance.Title;
        DesignerDrawing.DrawTextField(spriteBatch, pixel, font, titleRect, titleText, _editingFieldIndex == FieldTitle);
        y += fieldHeight + spacing;

        // Font Scale
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.font_scale"), new Vector2(rect.X + DesignerLayout.Padding, y + 4), DesignerColors.TextColor);
        var fontScaleRect = new Rectangle(rect.X + labelWidth + DesignerLayout.Padding, y, rect.Width - labelWidth - DesignerLayout.Padding * 2, fieldHeight);
        string fontScaleText = _editingFieldIndex == FieldFontScale
            ? _editingFieldText
            : _appearance.TitleFontScale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        DesignerDrawing.DrawTextField(spriteBatch, pixel, font, fontScaleRect, fontScaleText, _editingFieldIndex == FieldFontScale);
        y += fieldHeight + spacing;

        // Color label
        font.DrawText(spriteBatch, "Color", new Vector2(rect.X + DesignerLayout.Padding, y + 4), DesignerColors.TextColor);
        y += fieldHeight + spacing;

        // Color swatches
        int swatchSize = 20;
        int swatchSpacing = 4;
        int swatchesPerRow = (rect.Width - DesignerLayout.Padding * 2) / (swatchSize + swatchSpacing);
        int swatchX = rect.X + DesignerLayout.Padding;

        for (int i = 0; i < DesignerColors.PresetColors.Length; i++)
        {
            var swatchRect = new Rectangle(swatchX, y, swatchSize, swatchSize);

            // Draw swatch
            spriteBatch.Draw(pixel, swatchRect, DesignerColors.PresetColors[i].color);

            // Draw selection border if this is the current color
            bool isSelected = (i == 0 && _appearance.FillColor == null) ||
                              (i > 0 && _appearance.FillColor == DesignerColors.ColorToHex(DesignerColors.PresetColors[i].color));
            if (isSelected)
            {
                DesignerDrawing.DrawBorder(spriteBatch, pixel, swatchRect, DesignerColors.TextColor, 2);
            }
            else
            {
                DesignerDrawing.DrawBorder(spriteBatch, pixel, swatchRect, DesignerColors.BorderColor, 1);
            }

            swatchX += swatchSize + swatchSpacing;
            if ((i + 1) % swatchesPerRow == 0)
            {
                swatchX = rect.X + DesignerLayout.Padding;
                y += swatchSize + swatchSpacing;
            }
        }

        // Custom color row (after swatches)
        if (swatchX != rect.X + DesignerLayout.Padding)
        {
            y += swatchSize + swatchSpacing;
        }
        y += DesignerLayout.Padding;

        // Custom color input with preview
        int previewSize = fieldHeight - 4;
        var previewRect = new Rectangle(rect.X + DesignerLayout.Padding, y + 2, previewSize, previewSize);
        var currentFillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, previewRect, currentFillColor);
        DesignerDrawing.DrawBorder(spriteBatch, pixel, previewRect, DesignerColors.BorderColor, 1);

        // Custom color text field
        var customColorRect = new Rectangle(rect.X + DesignerLayout.Padding + previewSize + 4, y, rect.Width - DesignerLayout.Padding * 2 - previewSize - 4, fieldHeight);
        string customColorText = _editingFieldIndex == FieldCustomColor
            ? _editingFieldText
            : (_appearance.FillColor ?? "#3C3C46");
        DesignerDrawing.DrawTextField(spriteBatch, pixel, font, customColorRect, customColorText, _editingFieldIndex == FieldCustomColor);
    }

    public void HandleTextInput(char character)
    {
        if (_editingFieldIndex < 0)
        {
            return;
        }

        if (_editingFieldIndex == FieldTitle)
        {
            // Title accepts any printable character
            if (!char.IsControl(character))
            {
                _editingFieldText += character;
            }
        }
        else if (_editingFieldIndex == FieldFontScale)
        {
            // Font scale accepts digits and decimal point
            if (char.IsDigit(character) || (character == '.' && !_editingFieldText.Contains('.')))
            {
                _editingFieldText += character;
            }
        }
        else if (_editingFieldIndex == FieldCustomColor)
        {
            // Custom color accepts hex characters and #
            char upper = char.ToUpper(character);
            if (upper == '#' || char.IsDigit(upper) || (upper >= 'A' && upper <= 'F'))
            {
                // Limit to 7 characters (#RRGGBB)
                if (_editingFieldText.Length < 7)
                {
                    _editingFieldText += upper;
                }
            }
        }
        else
        {
            // Width/Height accept only digits
            if (char.IsDigit(character))
            {
                _editingFieldText += character;
            }
        }
    }

    public void HandleKeyPress(bool backspace, bool enter, bool escape)
    {
        if (_editingFieldIndex < 0)
        {
            return;
        }

        if (backspace && _editingFieldText.Length > 0)
        {
            _editingFieldText = _editingFieldText.Substring(0, _editingFieldText.Length - 1);
        }
        else if (enter)
        {
            ApplyEditingValue();
            _editingFieldIndex = -1;
        }
        else if (escape)
        {
            _editingFieldIndex = -1;
        }
    }

    public void HandlePaste(string text)
    {
        if (_editingFieldIndex < 0 || string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_editingFieldIndex == FieldTitle)
        {
            // Title accepts any text
            _editingFieldText += text;
        }
        else if (_editingFieldIndex == FieldFontScale)
        {
            // Font scale accepts digits and decimal point
            foreach (char c in text)
            {
                if (char.IsDigit(c) || (c == '.' && !_editingFieldText.Contains('.')))
                {
                    _editingFieldText += c;
                }
            }
        }
        else if (_editingFieldIndex == FieldCustomColor)
        {
            // Custom color only accepts hex characters
            foreach (char c in text)
            {
                char upper = char.ToUpper(c);
                if (upper == '#' || char.IsDigit(upper) || (upper >= 'A' && upper <= 'F'))
                {
                    if (_editingFieldText.Length < 7)
                    {
                        _editingFieldText += upper;
                    }
                }
            }
        }
        else
        {
            // Width/Height accept only digits
            foreach (char c in text)
            {
                if (char.IsDigit(c))
                {
                    _editingFieldText += c;
                }
            }
        }
    }

    public void ShowContextMenu(Point mousePos, int screenWidth, int screenHeight)
    {
        RequestContextMenu(mousePos, screenWidth, screenHeight);
    }

    public void Reset()
    {
        _appearance = null;
        _editingFieldIndex = -1;
        _editingFieldText = "";
    }

    private void StartEditing(int fieldIndex, string value)
    {
        _editingFieldIndex = fieldIndex;
        _editingFieldText = value;
    }

    private void RequestContextMenu(Point mousePos, int screenWidth, int screenHeight)
    {
        int menuWidth = DesignerLayout.ContextMenuItemWidth;
        int menuHeight = DesignerLayout.ContextMenuItemHeight;

        // Ensure menu stays on screen
        int x = mousePos.X;
        int y = mousePos.Y;
        if (x + menuWidth > screenWidth)
        {
            x = screenWidth - menuWidth;
        }
        if (y + menuHeight > screenHeight)
        {
            y = screenHeight - menuHeight;
        }

        OnContextMenuRequested?.Invoke(new Rectangle(x, y, menuWidth, menuHeight));
    }

    private void ApplyEditingValue()
    {
        if (_appearance == null)
        {
            return;
        }

        if (_editingFieldIndex == FieldTitle)
        {
            _appearance.Title = _editingFieldText;
            OnAppearanceChanged?.Invoke();
        }
        else if (_editingFieldIndex == FieldFontScale)
        {
            // Parse and clamp font scale (0.5 to 3.0)
            if (float.TryParse(_editingFieldText, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                _appearance.TitleFontScale = Math.Clamp(scale, 0.5f, 3.0f);
                OnAppearanceChanged?.Invoke();
            }
        }
        else if (_editingFieldIndex == FieldCustomColor)
        {
            // Validate and apply custom color
            string hex = _editingFieldText.Trim();
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }

            // Ensure it's a valid 7-character hex color
            if (hex.Length == 7 && DesignerColors.IsValidHexColor(hex))
            {
                _appearance.FillColor = hex.ToUpper();
                OnAppearanceChanged?.Invoke();
            }
        }
        else if (int.TryParse(_editingFieldText, out int gridCells))
        {
            // Convert grid cells to pixels (minimum 1 cell)
            gridCells = Math.Max(1, gridCells);
            int pixels = gridCells * DesignerLayout.GridSize;

            if (_editingFieldIndex == FieldWidth)
            {
                _appearance.Width = pixels;
                OnAppearanceChanged?.Invoke();
            }
            else if (_editingFieldIndex == FieldHeight)
            {
                _appearance.Height = pixels;
                OnAppearanceChanged?.Invoke();
            }
        }
    }

    private Color GetCurrentFillColor()
    {
        if (_appearance?.FillColor == null)
        {
            return DesignerColors.PresetColors[0].color;
        }
        return DesignerColors.HexToColor(_appearance.FillColor);
    }
}
