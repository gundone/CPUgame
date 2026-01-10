using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Core.Designer;
using CPUgame.Core.Localization;
using CPUgame.Core.Services;
using CPUgame.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Main controller for the Component Designer mode.
/// Provides a full-screen UI for editing component visual appearances.
/// </summary>
public class DesignerMode
{
    private readonly IAppearanceService _appearanceService;
    private readonly IComponentBuilder _componentBuilder;
    private readonly IFontService _fontService;

    // UI Layout
    private const int _selectorWidth = 180;
    private const int _propertiesWidth = 200;
    private const int _pinEditorHeight = 120;
    private const int _padding = 8;
    private const int _itemHeight = 28;
    private const int _headerHeight = 30;

    // Colors
    private static readonly Color _backgroundColor = new(35, 35, 45);
    private static readonly Color _panelColor = new(45, 45, 55);
    private static readonly Color _headerColor = new(55, 55, 65);
    private static readonly Color _borderColor = new(70, 70, 85);
    private static readonly Color _textColor = new(220, 220, 230);
    private static readonly Color _dimTextColor = new(140, 140, 160);
    private static readonly Color _selectedColor = new(70, 100, 140);
    private static readonly Color _hoverColor = new(60, 60, 75);
    private static readonly Color _inputFieldColor = new(35, 35, 45);
    private static readonly Color _buttonColor = new(60, 100, 140);
    private static readonly Color _buttonHoverColor = new(80, 120, 160);
    private static readonly Color _pinColor = new(100, 180, 100);
    private static readonly Color _pinSelectedColor = new(255, 200, 100);
    private static readonly Color _gridColor = new(50, 50, 60);
    private static readonly Color _componentBodyColor = new(60, 60, 70);

    // State
    private string? _selectedComponentType;
    private ComponentAppearance? _editingAppearance;
    private int _selectorScrollOffset;
    private int _selectedPinIndex = -1;
    private bool _isDraggingPin;
    private int _hoveredComponentIndex = -1;
    private int _editingFieldIndex = -1;
    private string _editingFieldText = "";

    // Field indices for editing
    private const int _fieldWidth = 0;
    private const int _fieldHeight = 1;
    private const int _fieldTitle = 2;
    private const int _fieldFontScale = 3;
    private const int _fieldCustomColor = 4;

    // Title dragging state
    private bool _isDraggingTitle;
    private Rectangle _titleRect;

    // Preset colors for component fill
    private static readonly (string name, Color color)[] _presetColors =
    {
        ("Default", new Color(60, 60, 70)),
        ("Red", new Color(120, 50, 50)),
        ("Green", new Color(50, 100, 50)),
        ("Blue", new Color(50, 60, 120)),
        ("Yellow", new Color(120, 110, 40)),
        ("Purple", new Color(90, 50, 110)),
        ("Cyan", new Color(40, 100, 110)),
        ("Orange", new Color(130, 70, 30)),
        ("Gray", new Color(80, 80, 80))
    };

    // Cached component list
    private List<string> _componentTypes = new();

    // Preview state
    private const int _previewScale = 3;
    private const int _gridSize = 20;

    // Buttons
    private Rectangle _saveButtonRect;
    private Rectangle _resetButtonRect;
    private bool _saveButtonHovered;
    private bool _resetButtonHovered;

    // Save message feedback
    private double _saveMessageTimer;
    private const double _saveMessageDuration = 2.0;

    // Context menu state
    private bool _contextMenuVisible;
    private Rectangle _contextMenuRect;
    private int _contextMenuHoveredItem = -1;
    private const int _contextMenuItemWidth = 100;
    private const int _contextMenuItemHeight = 26;
    private Func<string?>? _getClipboardText;

    public bool IsActive { get; private set; }

    /// <summary>
    /// Event fired when appearance is saved, so circuit can be updated.
    /// </summary>
    public event Action? OnAppearanceSaved;

    public DesignerMode(IAppearanceService appearanceService, IComponentBuilder componentBuilder, IFontService fontService)
    {
        _appearanceService = appearanceService;
        _componentBuilder = componentBuilder;
        _fontService = fontService;
    }

    /// <summary>
    /// Sets the function to retrieve clipboard text.
    /// </summary>
    public void SetClipboardGetter(Func<string?> getter)
    {
        _getClipboardText = getter;
    }

    public void Activate()
    {
        IsActive = true;
        RefreshComponentList();
        _selectedComponentType = null;
        _editingAppearance = null;
        _selectedPinIndex = -1;
        _editingFieldIndex = -1;
    }

    public void Deactivate()
    {
        IsActive = false;

        // Save current work - wrapped in try-catch to ensure state cleanup happens
        try
        {
            SaveCurrentAppearance();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save appearance during deactivation: {ex.Message}");
        }

        // Clear all state to prevent issues when reactivating
        _selectedComponentType = null;
        _editingAppearance = null;
        _selectedPinIndex = -1;
        _editingFieldIndex = -1;
        _isDraggingPin = false;
        _isDraggingTitle = false;
        _contextMenuVisible = false;
        _selectorScrollOffset = 0;
        _hoveredComponentIndex = -1;
        _saveMessageTimer = 0;
    }

    private void RefreshComponentList()
    {
        _componentTypes.Clear();

        // Built-in components (exclude BusInput/BusOutput - they have dynamic pins)
        _componentTypes.Add("NAND");
        _componentTypes.Add("Switch");
        _componentTypes.Add("LED");
        _componentTypes.Add("Clock");

        // Custom components
        foreach (var name in _componentBuilder.CustomComponents.Keys)
        {
            _componentTypes.Add($"Custom:{name}");
        }
    }

    private void SelectComponent(string componentType)
    {
        SaveCurrentAppearance();

        _selectedComponentType = componentType;
        _selectedPinIndex = -1;
        _editingFieldIndex = -1;

        // Load or create appearance
        var existing = _appearanceService.GetAppearance(componentType);
        if (existing != null)
        {
            _editingAppearance = existing.Clone();
        }
        else
        {
            _editingAppearance = _appearanceService.GetDefaultAppearance(componentType);
        }
    }

    private void SaveCurrentAppearance()
    {
        if (_editingAppearance != null && _selectedComponentType != null)
        {
            _appearanceService.SetAppearance(_editingAppearance);
            _appearanceService.SaveAll();
            OnAppearanceSaved?.Invoke();
        }
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased,
        bool rightMouseJustPressed, int scrollDelta, int screenWidth, int screenHeight, double deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        // Calculate panel bounds
        var selectorRect = GetSelectorRect(screenHeight);
        var propertiesRect = GetPropertiesRect(screenWidth, screenHeight);
        var previewRect = GetPreviewRect(screenWidth, screenHeight);
        var pinEditorRect = GetPinEditorRect(screenWidth, screenHeight);

        // Update button rectangles
        int buttonWidth = 80;
        int buttonHeight = 28;
        int buttonsY = screenHeight - buttonHeight - _padding;
        _saveButtonRect = new Rectangle(screenWidth / 2 - buttonWidth - _padding, buttonsY, buttonWidth, buttonHeight);
        _resetButtonRect = new Rectangle(screenWidth / 2 + _padding, buttonsY, buttonWidth, buttonHeight);

        _saveButtonHovered = _saveButtonRect.Contains(mousePos);
        _resetButtonHovered = _resetButtonRect.Contains(mousePos);

        // Handle context menu
        if (_contextMenuVisible)
        {
            _contextMenuHoveredItem = -1;
            if (_contextMenuRect.Contains(mousePos))
            {
                int itemY = mousePos.Y - _contextMenuRect.Y;
                _contextMenuHoveredItem = itemY / _contextMenuItemHeight;
            }

            if (mouseJustPressed)
            {
                if (_contextMenuHoveredItem == 0) // Paste
                {
                    ExecutePaste();
                }
                _contextMenuVisible = false;
                return;
            }

            if (rightMouseJustPressed)
            {
                _contextMenuVisible = false;
            }
            return;
        }

        // Handle button clicks
        if (mouseJustPressed)
        {
            if (_saveButtonHovered)
            {
                SaveCurrentAppearance();
                _saveMessageTimer = _saveMessageDuration;
            }
            else if (_resetButtonHovered && _selectedComponentType != null)
            {
                _appearanceService.ResetAppearance(_selectedComponentType);
                _editingAppearance = _appearanceService.GetDefaultAppearance(_selectedComponentType);
            }
        }

        // Show context menu on right-click when editing a field
        if (rightMouseJustPressed && _editingFieldIndex >= 0)
        {
            ShowContextMenu(mousePos, screenWidth, screenHeight);
            return;
        }

        // Update save message timer
        if (_saveMessageTimer > 0)
        {
            _saveMessageTimer -= deltaTime;
        }

        // Handle selector panel
        UpdateSelector(mousePos, mouseJustPressed, scrollDelta, selectorRect);

        // Handle properties panel
        if (_editingAppearance != null)
        {
            UpdateProperties(mousePos, mouseJustPressed, rightMouseJustPressed, propertiesRect, screenWidth, screenHeight);
        }

        // Handle preview (pin dragging)
        if (_editingAppearance != null)
        {
            UpdatePreview(mousePos, mousePressed, mouseJustPressed, mouseJustReleased, previewRect);
        }

        // Handle pin editor
        if (_editingAppearance != null)
        {
            UpdatePinEditor(mousePos, mouseJustPressed, pinEditorRect);
        }
    }

    private void ShowContextMenu(Point mousePos, int screenWidth, int screenHeight)
    {
        int menuWidth = _contextMenuItemWidth;
        int menuHeight = _contextMenuItemHeight; // Just "Paste" for now

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

        _contextMenuRect = new Rectangle(x, y, menuWidth, menuHeight);
        _contextMenuVisible = true;
        _contextMenuHoveredItem = -1;
    }

    private void ExecutePaste()
    {
        if (_getClipboardText == null)
        {
            return;
        }

        var text = _getClipboardText();
        if (!string.IsNullOrEmpty(text))
        {
            HandlePaste(text);
        }
    }

    private void UpdateSelector(Point mousePos, bool mouseJustPressed, int scrollDelta, Rectangle selectorRect)
    {
        _hoveredComponentIndex = -1;

        if (selectorRect.Contains(mousePos))
        {
            // Handle scroll
            if (scrollDelta != 0)
            {
                _selectorScrollOffset -= scrollDelta / 40;
                _selectorScrollOffset = Math.Max(0, _selectorScrollOffset);
            }

            // Find hovered item - must match DrawSelector layout exactly
            int y = selectorRect.Y + _headerHeight + _padding - _selectorScrollOffset * _itemHeight;

            // Skip "Built-in" header
            y += _itemHeight;

            for (int i = 0; i < _componentTypes.Count; i++)
            {
                var componentType = _componentTypes[i];

                // Skip "Custom" header when transitioning from built-in to custom
                if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
                {
                    y += _itemHeight;
                }

                var itemRect = new Rectangle(selectorRect.X + _padding, y, selectorRect.Width - _padding * 2, _itemHeight);
                if (itemRect.Contains(mousePos))
                {
                    _hoveredComponentIndex = i;

                    if (mouseJustPressed)
                    {
                        SelectComponent(_componentTypes[i]);
                    }
                    break;
                }
                y += _itemHeight;
            }
        }
    }

    private void UpdateProperties(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed,
        Rectangle propertiesRect, int screenWidth, int screenHeight)
    {
        if (propertiesRect.Contains(mousePos) && _editingAppearance != null)
        {
            // Calculate property item positions
            int y = propertiesRect.Y + _headerHeight + _padding;
            int fieldHeight = 26;
            int spacing = 4;
            int labelWidth = propertiesRect.Width / 2 - _padding;

            // Width field (display in grid cells)
            var widthRect = new Rectangle(propertiesRect.X + labelWidth + _padding, y, propertiesRect.Width - labelWidth - _padding * 2, fieldHeight);
            if (widthRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != _fieldWidth)
                {
                    StartEditing(_fieldWidth, (_editingAppearance.Width / _gridSize).ToString());
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(_fieldWidth, (_editingAppearance.Width / _gridSize).ToString());
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Height field (display in grid cells)
            var heightRect = new Rectangle(propertiesRect.X + labelWidth + _padding, y, propertiesRect.Width - labelWidth - _padding * 2, fieldHeight);
            if (heightRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != _fieldHeight)
                {
                    StartEditing(_fieldHeight, (_editingAppearance.Height / _gridSize).ToString());
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(_fieldHeight, (_editingAppearance.Height / _gridSize).ToString());
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Title field
            var titleRect = new Rectangle(propertiesRect.X + labelWidth + _padding, y, propertiesRect.Width - labelWidth - _padding * 2, fieldHeight);
            if (titleRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != _fieldTitle)
                {
                    StartEditing(_fieldTitle, _editingAppearance.Title);
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(_fieldTitle, _editingAppearance.Title);
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Font scale field
            var fontScaleRect = new Rectangle(propertiesRect.X + labelWidth + _padding, y, propertiesRect.Width - labelWidth - _padding * 2, fieldHeight);
            if (fontScaleRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != _fieldFontScale)
                {
                    StartEditing(_fieldFontScale, _editingAppearance.TitleFontScale.ToString("0.0"));
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(_fieldFontScale, _editingAppearance.TitleFontScale.ToString("0.0"));
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Color label row
            y += fieldHeight + spacing;

            // Color swatches
            int swatchSize = 20;
            int swatchSpacing = 4;
            int swatchesPerRow = (propertiesRect.Width - _padding * 2) / (swatchSize + swatchSpacing);
            int swatchX = propertiesRect.X + _padding;

            for (int i = 0; i < _presetColors.Length; i++)
            {
                var swatchRect = new Rectangle(swatchX, y, swatchSize, swatchSize);
                if (swatchRect.Contains(mousePos) && mouseJustPressed)
                {
                    if (i == 0)
                    {
                        _editingAppearance.FillColor = null; // Default
                    }
                    else
                    {
                        _editingAppearance.FillColor = ColorToHex(_presetColors[i].color);
                    }
                    _editingFieldIndex = -1; // Stop editing custom color field
                }

                swatchX += swatchSize + swatchSpacing;
                if ((i + 1) % swatchesPerRow == 0)
                {
                    swatchX = propertiesRect.X + _padding;
                    y += swatchSize + swatchSpacing;
                }
            }

            // Custom color row (after swatches)
            // Ensure we're on a new row
            if (swatchX != propertiesRect.X + _padding)
            {
                y += swatchSize + swatchSpacing;
            }
            y += _padding;

            // Custom color field
            var customColorRect = new Rectangle(propertiesRect.X + _padding, y, propertiesRect.Width - _padding * 2, fieldHeight);
            if (customColorRect.Contains(mousePos))
            {
                string currentHex = _editingAppearance.FillColor ?? "#3C3C46";
                if (mouseJustPressed && _editingFieldIndex != _fieldCustomColor)
                {
                    StartEditing(_fieldCustomColor, currentHex);
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(_fieldCustomColor, currentHex);
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
        }
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length < 7)
        {
            return new Color(60, 60, 70); // Default
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
            return new Color(60, 60, 70); // Default on parse error
        }
    }

    private Color GetCurrentFillColor()
    {
        if (_editingAppearance?.FillColor == null)
        {
            return _presetColors[0].color; // Default
        }
        return HexToColor(_editingAppearance.FillColor);
    }

    private void StartEditing(int fieldIndex, string value)
    {
        _editingFieldIndex = fieldIndex;
        _editingFieldText = value;
    }

    private void UpdatePreview(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, Rectangle previewRect)
    {
        if (_editingAppearance == null)
        {
            return;
        }

        // Calculate component position in preview
        int compWidth = _editingAppearance.Width * _previewScale;
        int compHeight = _editingAppearance.Height * _previewScale;
        int compX = previewRect.X + (previewRect.Width - compWidth) / 2;
        int compY = previewRect.Y + (previewRect.Height - compHeight) / 2;

        // Check for click on title or pins
        if (mouseJustPressed && previewRect.Contains(mousePos))
        {
            // Check title first (stored in _titleRect from last draw)
            if (_titleRect.Contains(mousePos))
            {
                _isDraggingTitle = true;
                _selectedPinIndex = -1;
            }
            else
            {
                // Check all pins
                int pinIndex = 0;
                foreach (var pin in _editingAppearance.InputPins.Concat(_editingAppearance.OutputPins))
                {
                    int pinX = compX + pin.LocalX * _previewScale;
                    int pinY = compY + pin.LocalY * _previewScale;
                    int pinRadius = 8 * _previewScale;

                    if (Math.Abs(mousePos.X - pinX) < pinRadius && Math.Abs(mousePos.Y - pinY) < pinRadius)
                    {
                        _selectedPinIndex = pinIndex;
                        _isDraggingPin = true;
                        _isDraggingTitle = false;
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
            _editingAppearance.TitleOffsetX = (mousePos.X - centerX) / _previewScale;
            _editingAppearance.TitleOffsetY = (mousePos.Y - centerY) / _previewScale;
        }

        // Handle pin dragging
        if (_isDraggingPin && mousePressed && _selectedPinIndex >= 0)
        {
            // Update pin position
            int localX = (mousePos.X - compX) / _previewScale;
            int localY = (mousePos.Y - compY) / _previewScale;

            // Snap to grid (use game's grid size)
            localX = (int)Math.Round((double)localX / _gridSize) * _gridSize;
            localY = (int)Math.Round((double)localY / _gridSize) * _gridSize;

            // Clamp to component bounds
            localX = Math.Clamp(localX, 0, _editingAppearance.Width);
            localY = Math.Clamp(localY, 0, _editingAppearance.Height);

            // Update the pin
            int inputCount = _editingAppearance.InputPins.Count;
            if (_selectedPinIndex < inputCount)
            {
                _editingAppearance.InputPins[_selectedPinIndex].LocalX = localX;
                _editingAppearance.InputPins[_selectedPinIndex].LocalY = localY;
            }
            else
            {
                int outputIndex = _selectedPinIndex - inputCount;
                _editingAppearance.OutputPins[outputIndex].LocalX = localX;
                _editingAppearance.OutputPins[outputIndex].LocalY = localY;
            }
        }

        if (mouseJustReleased)
        {
            _isDraggingPin = false;
            _isDraggingTitle = false;
        }
    }

    private void UpdatePinEditor(Point mousePos, bool mouseJustPressed, Rectangle pinEditorRect)
    {
        // Pin list selection handled by clicking on pin names
    }

    public void HandlePaste(string text)
    {
        if (_editingFieldIndex < 0 || string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_editingFieldIndex == _fieldTitle)
        {
            // Title accepts any text
            _editingFieldText += text;
        }
        else if (_editingFieldIndex == _fieldFontScale)
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
        else if (_editingFieldIndex == _fieldCustomColor)
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

    public void HandleTextInput(char character)
    {
        if (_editingFieldIndex >= 0)
        {
            if (_editingFieldIndex == _fieldTitle)
            {
                // Title accepts any printable character
                if (!char.IsControl(character))
                {
                    _editingFieldText += character;
                }
            }
            else if (_editingFieldIndex == _fieldFontScale)
            {
                // Font scale accepts digits and decimal point
                if (char.IsDigit(character) || (character == '.' && !_editingFieldText.Contains('.')))
                {
                    _editingFieldText += character;
                }
            }
            else if (_editingFieldIndex == _fieldCustomColor)
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
    }

    public void HandleKeyPress(bool backspace, bool enter, bool escape)
    {
        if (_editingFieldIndex >= 0)
        {
            if (backspace && _editingFieldText.Length > 0)
            {
                _editingFieldText = _editingFieldText.Substring(0, _editingFieldText.Length - 1);
            }
            else if (enter)
            {
                // Apply value on Enter
                ApplyEditingValue();
                _editingFieldIndex = -1;
            }
            else if (escape)
            {
                // Cancel editing on Escape
                _editingFieldIndex = -1;
            }
        }
    }

    private void ApplyEditingValue()
    {
        if (_editingAppearance == null)
        {
            return;
        }

        if (_editingFieldIndex == _fieldTitle)
        {
            _editingAppearance.Title = _editingFieldText;
        }
        else if (_editingFieldIndex == _fieldFontScale)
        {
            // Parse and clamp font scale (0.5 to 3.0)
            if (float.TryParse(_editingFieldText, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                _editingAppearance.TitleFontScale = Math.Clamp(scale, 0.5f, 3.0f);
            }
        }
        else if (_editingFieldIndex == _fieldCustomColor)
        {
            // Validate and apply custom color
            string hex = _editingFieldText.Trim();
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }

            // Ensure it's a valid 7-character hex color
            if (hex.Length == 7 && IsValidHexColor(hex))
            {
                _editingAppearance.FillColor = hex.ToUpper();
            }
        }
        else if (int.TryParse(_editingFieldText, out int gridCells))
        {
            // Convert grid cells to pixels (minimum 1 cell)
            gridCells = Math.Max(1, gridCells);
            int pixels = gridCells * _gridSize;

            if (_editingFieldIndex == _fieldWidth)
            {
                _editingAppearance.Width = pixels;
            }
            else if (_editingFieldIndex == _fieldHeight)
            {
                _editingAppearance.Height = pixels;
            }
        }
    }

    private static bool IsValidHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#')
        {
            return false;
        }

        for (int i = 1; i < 7; i++)
        {
            char c = char.ToUpper(hex[i]);
            if (!char.IsDigit(c) && (c < 'A' || c > 'F'))
            {
                return false;
            }
        }
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight, Point mousePos)
    {
        if (!IsActive)
        {
            return;
        }

        // Background
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), _backgroundColor);

        // Draw panels
        var selectorRect = GetSelectorRect(screenHeight);
        var propertiesRect = GetPropertiesRect(screenWidth, screenHeight);
        var previewRect = GetPreviewRect(screenWidth, screenHeight);
        var pinEditorRect = GetPinEditorRect(screenWidth, screenHeight);

        DrawSelector(spriteBatch, pixel, font, selectorRect);
        DrawProperties(spriteBatch, pixel, font, propertiesRect);
        DrawPreview(spriteBatch, pixel, font, previewRect);
        DrawPinEditor(spriteBatch, pixel, font, pinEditorRect);

        // Draw buttons
        DrawButton(spriteBatch, pixel, font, _saveButtonRect, LocalizationManager.Get("designer.save"), _saveButtonHovered);
        DrawButton(spriteBatch, pixel, font, _resetButtonRect, LocalizationManager.Get("designer.reset_default"), _resetButtonHovered);

        // Draw save confirmation message (fading over 2 seconds) in bottom right corner
        if (_saveMessageTimer > 0)
        {
            float alpha = (float)(_saveMessageTimer / _saveMessageDuration);
            var messageColor = new Color(_textColor.R, _textColor.G, _textColor.B, (byte)(alpha * 255));
            var message = "Ok, saved";
            var messageSize = font.MeasureString(message);
            float messageX = screenWidth - messageSize.X - _padding;
            float messageY = screenHeight - messageSize.Y - _padding;
            font.DrawText(spriteBatch, message, new Vector2(messageX, messageY), messageColor);
        }

        // Title
        var title = LocalizationManager.Get("designer.title");
        var titleSize = font.MeasureString(title);
        font.DrawText(spriteBatch, title, new Vector2((screenWidth - titleSize.X) / 2, 4), _textColor);

        // Draw context menu (on top of everything)
        if (_contextMenuVisible)
        {
            DrawContextMenu(spriteBatch, pixel, font);
        }
    }

    private void DrawContextMenu(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        // Background with border
        spriteBatch.Draw(pixel, _contextMenuRect, _panelColor);
        DrawBorder(spriteBatch, pixel, _contextMenuRect, _borderColor, 1);

        // Paste item
        var itemRect = new Rectangle(_contextMenuRect.X, _contextMenuRect.Y, _contextMenuRect.Width, _contextMenuItemHeight);
        if (_contextMenuHoveredItem == 0)
        {
            spriteBatch.Draw(pixel, itemRect, _hoverColor);
        }

        string pasteText = LocalizationManager.Get("designer.paste");
        var pasteSize = font.MeasureString(pasteText);
        float textX = itemRect.X + _padding;
        float textY = itemRect.Y + (itemRect.Height - pasteSize.Y) / 2;
        font.DrawText(spriteBatch, pasteText, new Vector2(textX, textY), _textColor);
    }

    private Rectangle GetSelectorRect(int screenHeight)
    {
        return new Rectangle(_padding, _headerHeight, _selectorWidth, screenHeight - _headerHeight - _padding * 2 - 40);
    }

    private Rectangle GetPropertiesRect(int screenWidth, int screenHeight)
    {
        return new Rectangle(screenWidth - _propertiesWidth - _padding, _headerHeight, _propertiesWidth, screenHeight - _headerHeight - _pinEditorHeight - _padding * 3 - 40);
    }

    private Rectangle GetPreviewRect(int screenWidth, int screenHeight)
    {
        int left = _selectorWidth + _padding * 2;
        int right = screenWidth - _propertiesWidth - _padding * 2;
        return new Rectangle(left, _headerHeight, right - left, screenHeight - _headerHeight - _pinEditorHeight - _padding * 3 - 40);
    }

    private Rectangle GetPinEditorRect(int screenWidth, int screenHeight)
    {
        int left = _selectorWidth + _padding * 2;
        int right = screenWidth - _propertiesWidth - _padding * 2;
        return new Rectangle(left, screenHeight - _pinEditorHeight - _padding - 40, right - left, _pinEditorHeight);
    }

    private void DrawSelector(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, _panelColor);
        DrawBorder(spriteBatch, pixel, rect, _borderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, _headerHeight);
        spriteBatch.Draw(pixel, headerRect, _headerColor);

        var headerText = LocalizationManager.Get("designer.select_component");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (_headerHeight - headerSize.Y) / 2), _textColor);

        // Component list
        int y = rect.Y + _headerHeight + _padding - _selectorScrollOffset * _itemHeight;

        // Built-in header
        if (y > rect.Y + _headerHeight)
        {
            font.DrawText(spriteBatch, LocalizationManager.Get("designer.builtin"), new Vector2(rect.X + _padding, y), _dimTextColor);
        }
        y += _itemHeight;

        for (int i = 0; i < _componentTypes.Count; i++)
        {
            var componentType = _componentTypes[i];
            var itemRect = new Rectangle(rect.X + _padding, y, rect.Width - _padding * 2, _itemHeight);

            // Skip if outside visible area
            if (y + _itemHeight < rect.Y + _headerHeight || y > rect.Bottom)
            {
                y += _itemHeight;
                continue;
            }

            // Custom components header
            if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
            {
                font.DrawText(spriteBatch, LocalizationManager.Get("designer.custom"), new Vector2(rect.X + _padding, y), _dimTextColor);
                y += _itemHeight;
                itemRect = new Rectangle(rect.X + _padding, y, rect.Width - _padding * 2, _itemHeight);
            }

            // Draw item
            bool isSelected = componentType == _selectedComponentType;
            bool isHovered = i == _hoveredComponentIndex;

            if (isSelected)
            {
                spriteBatch.Draw(pixel, itemRect, _selectedColor);
            }
            else if (isHovered)
            {
                spriteBatch.Draw(pixel, itemRect, _hoverColor);
            }

            string displayName = componentType.StartsWith("Custom:") ? componentType.Substring(7) : componentType;
            font.DrawText(spriteBatch, displayName, new Vector2(itemRect.X + 4, itemRect.Y + (itemRect.Height - font.MeasureString(displayName).Y) / 2), _textColor);

            y += _itemHeight;
        }
    }

    private void DrawProperties(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, _panelColor);
        DrawBorder(spriteBatch, pixel, rect, _borderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, _headerHeight);
        spriteBatch.Draw(pixel, headerRect, _headerColor);

        var headerText = LocalizationManager.Get("designer.properties");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (_headerHeight - headerSize.Y) / 2), _textColor);

        if (_editingAppearance == null)
        {
            return;
        }

        int y = rect.Y + _headerHeight + _padding;
        int labelWidth = rect.Width / 2 - _padding;
        int fieldHeight = 26;
        int spacing = 4;

        // Width (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.width"), new Vector2(rect.X + _padding, y + 4), _textColor);
        var widthRect = new Rectangle(rect.X + labelWidth + _padding, y, rect.Width - labelWidth - _padding * 2, fieldHeight);
        string widthText = _editingFieldIndex == _fieldWidth ? _editingFieldText : (_editingAppearance.Width / _gridSize).ToString();
        DrawTextField(spriteBatch, pixel, font, widthRect, widthText, _editingFieldIndex == _fieldWidth);
        y += fieldHeight + spacing;

        // Height (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.height"), new Vector2(rect.X + _padding, y + 4), _textColor);
        var heightRect = new Rectangle(rect.X + labelWidth + _padding, y, rect.Width - labelWidth - _padding * 2, fieldHeight);
        string heightText = _editingFieldIndex == _fieldHeight ? _editingFieldText : (_editingAppearance.Height / _gridSize).ToString();
        DrawTextField(spriteBatch, pixel, font, heightRect, heightText, _editingFieldIndex == _fieldHeight);
        y += fieldHeight + spacing;

        // Title
        font.DrawText(spriteBatch, "Title", new Vector2(rect.X + _padding, y + 4), _textColor);
        var titleRect = new Rectangle(rect.X + labelWidth + _padding, y, rect.Width - labelWidth - _padding * 2, fieldHeight);
        string titleText = _editingFieldIndex == _fieldTitle ? _editingFieldText : _editingAppearance.Title;
        DrawTextField(spriteBatch, pixel, font, titleRect, titleText, _editingFieldIndex == _fieldTitle);
        y += fieldHeight + spacing;

        // Font Scale
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.font_scale"), new Vector2(rect.X + _padding, y + 4), _textColor);
        var fontScaleRect = new Rectangle(rect.X + labelWidth + _padding, y, rect.Width - labelWidth - _padding * 2, fieldHeight);
        string fontScaleText = _editingFieldIndex == _fieldFontScale
            ? _editingFieldText
            : _editingAppearance.TitleFontScale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        DrawTextField(spriteBatch, pixel, font, fontScaleRect, fontScaleText, _editingFieldIndex == _fieldFontScale);
        y += fieldHeight + spacing;

        // Color label
        font.DrawText(spriteBatch, "Color", new Vector2(rect.X + _padding, y + 4), _textColor);
        y += fieldHeight + spacing;

        // Color swatches
        int swatchSize = 20;
        int swatchSpacing = 4;
        int swatchesPerRow = (rect.Width - _padding * 2) / (swatchSize + swatchSpacing);
        int swatchX = rect.X + _padding;
        var currentColor = GetCurrentFillColor();

        for (int i = 0; i < _presetColors.Length; i++)
        {
            var swatchRect = new Rectangle(swatchX, y, swatchSize, swatchSize);

            // Draw swatch
            spriteBatch.Draw(pixel, swatchRect, _presetColors[i].color);

            // Draw selection border if this is the current color
            bool isSelected = (i == 0 && _editingAppearance.FillColor == null) ||
                              (i > 0 && _editingAppearance.FillColor == ColorToHex(_presetColors[i].color));
            if (isSelected)
            {
                DrawBorder(spriteBatch, pixel, swatchRect, _textColor, 2);
            }
            else
            {
                DrawBorder(spriteBatch, pixel, swatchRect, _borderColor, 1);
            }

            swatchX += swatchSize + swatchSpacing;
            if ((i + 1) % swatchesPerRow == 0)
            {
                swatchX = rect.X + _padding;
                y += swatchSize + swatchSpacing;
            }
        }

        // Custom color row (after swatches)
        if (swatchX != rect.X + _padding)
        {
            y += swatchSize + swatchSpacing;
        }
        y += _padding;

        // Custom color input with preview
        int previewSize = fieldHeight - 4;
        var previewRect = new Rectangle(rect.X + _padding, y + 2, previewSize, previewSize);
        var currentFillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, previewRect, currentFillColor);
        DrawBorder(spriteBatch, pixel, previewRect, _borderColor, 1);

        // Custom color text field
        var customColorRect = new Rectangle(rect.X + _padding + previewSize + 4, y, rect.Width - _padding * 2 - previewSize - 4, fieldHeight);
        string customColorText = _editingFieldIndex == _fieldCustomColor
            ? _editingFieldText
            : (_editingAppearance.FillColor ?? "#3C3C46");
        DrawTextField(spriteBatch, pixel, font, customColorRect, customColorText, _editingFieldIndex == _fieldCustomColor);
    }

    private void DrawPreview(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, _panelColor);
        DrawBorder(spriteBatch, pixel, rect, _borderColor, 1);

        if (_editingAppearance == null)
        {
            // Show hint
            var hint = LocalizationManager.Get("designer.select_component");
            var hintSize = font.MeasureString(hint);
            font.DrawText(spriteBatch, hint, new Vector2(rect.X + (rect.Width - hintSize.X) / 2, rect.Y + (rect.Height - hintSize.Y) / 2), _dimTextColor);
            return;
        }

        // Calculate component position first (needed for grid alignment)
        int compWidth = _editingAppearance.Width * _previewScale;
        int compHeight = _editingAppearance.Height * _previewScale;
        int compX = rect.X + (rect.Width - compWidth) / 2;
        int compY = rect.Y + (rect.Height - compHeight) / 2;

        // Draw grid aligned with component position
        int gridStep = _gridSize * _previewScale;

        // Draw vertical grid lines (aligned with component's left edge)
        int startX = compX - ((compX - rect.X) / gridStep + 1) * gridStep;
        for (int x = startX; x < rect.Right; x += gridStep)
        {
            if (x >= rect.X)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, rect.Y, 1, rect.Height), _gridColor);
            }
        }

        // Draw horizontal grid lines (aligned with component's top edge)
        int startY = compY - ((compY - rect.Y) / gridStep + 1) * gridStep;
        for (int y = startY; y < rect.Bottom; y += gridStep)
        {
            if (y >= rect.Y)
            {
                spriteBatch.Draw(pixel, new Rectangle(rect.X, y, rect.Width, 1), _gridColor);
            }
        }

        // Draw component body with selected fill color
        var fillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, new Rectangle(compX, compY, compWidth, compHeight), fillColor);
        DrawBorder(spriteBatch, pixel, new Rectangle(compX, compY, compWidth, compHeight), _borderColor, 2);

        // Draw title (use custom title if set, otherwise component type)
        string displayName;
        if (!string.IsNullOrEmpty(_editingAppearance.Title))
        {
            displayName = _editingAppearance.Title;
        }
        else if (_selectedComponentType?.StartsWith("Custom:") == true)
        {
            displayName = _selectedComponentType.Substring(7);
        }
        else
        {
            displayName = _selectedComponentType ?? "";
        }
        // Use font with scale based on both preview scale and title font scale
        float titleFontScale = _previewScale * _editingAppearance.TitleFontScale;
        var previewFont = _fontService.GetFont(titleFontScale);
        var nameSize = previewFont.MeasureString(displayName);

        // Calculate title position with offset (offset is in game pixels, convert to preview)
        float centerX = compX + compWidth / 2f;
        float centerY = compY + compHeight / 2f;
        float nameX = centerX - nameSize.X / 2 + _editingAppearance.TitleOffsetX * _previewScale;
        float nameY = centerY - nameSize.Y / 2 + _editingAppearance.TitleOffsetY * _previewScale;

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
            DrawBorder(spriteBatch, pixel, _titleRect, _pinSelectedColor, 1);
        }

        previewFont.DrawText(spriteBatch, displayName, new Vector2(nameX, nameY), _textColor);

        // Draw pins (use preview font without title scale)
        var pinFont = _fontService.GetFont(_previewScale);
        int pinIndex = 0;
        foreach (var pin in _editingAppearance.InputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * _previewScale, compY + pin.LocalY * _previewScale, pin.Name, pinIndex == _selectedPinIndex, true);
            pinIndex++;
        }
        foreach (var pin in _editingAppearance.OutputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * _previewScale, compY + pin.LocalY * _previewScale, pin.Name, pinIndex == _selectedPinIndex, false);
            pinIndex++;
        }

        // Draw hint
        var dragHint = LocalizationManager.Get("designer.drag_pins");
        var dragHintSize = font.MeasureString(dragHint);
        font.DrawText(spriteBatch, dragHint, new Vector2(rect.X + (rect.Width - dragHintSize.X) / 2, rect.Bottom - dragHintSize.Y - _padding), _dimTextColor);
    }

    private void DrawPin(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int x, int y, string name, bool isSelected, bool isInput)
    {
        // Scale pin size to match preview scale
        int basePinSize = isSelected ? 12 : 8;
        int pinSize = basePinSize * _previewScale / 2; // Scale but not too large
        var pinColor = isSelected ? _pinSelectedColor : _pinColor;

        spriteBatch.Draw(pixel, new Rectangle(x - pinSize / 2, y - pinSize / 2, pinSize, pinSize), pinColor);

        // Draw pin name (font is already at preview scale size)
        var nameSize = font.MeasureString(name);
        int offset = 8 * _previewScale;
        float nameX = isInput ? x - nameSize.X - offset : x + offset;
        float nameY = y - nameSize.Y / 2;
        font.DrawText(spriteBatch, name, new Vector2(nameX, nameY), _textColor);
    }

    private void DrawPinEditor(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, _panelColor);
        DrawBorder(spriteBatch, pixel, rect, _borderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, _headerHeight);
        spriteBatch.Draw(pixel, headerRect, _headerColor);

        var headerText = LocalizationManager.Get("designer.pin_positions");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (_headerHeight - headerSize.Y) / 2), _textColor);

        if (_editingAppearance == null)
        {
            return;
        }

        // Draw pin list
        int x = rect.X + _padding;
        int y = rect.Y + _headerHeight + _padding;
        int pinIndex = 0;

        foreach (var pin in _editingAppearance.InputPins.Concat(_editingAppearance.OutputPins))
        {
            bool isSelected = pinIndex == _selectedPinIndex;
            var pinText = $"{pin.Name}: X={pin.LocalX}, Y={pin.LocalY}";
            var textColor = isSelected ? _pinSelectedColor : _textColor;
            font.DrawText(spriteBatch, pinText, new Vector2(x, y), textColor);

            x += 150;
            if (x > rect.Right - 150)
            {
                x = rect.X + _padding;
                y += 20;
            }
            pinIndex++;
        }
    }

    private void DrawTextField(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect, string text, bool isEditing)
    {
        spriteBatch.Draw(pixel, rect, _inputFieldColor);
        DrawBorder(spriteBatch, pixel, rect, isEditing ? _selectedColor : _borderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + 4, rect.Y + (rect.Height - textSize.Y) / 2), _textColor);

        if (isEditing)
        {
            // Draw cursor
            int cursorX = rect.X + 4 + (int)textSize.X;
            spriteBatch.Draw(pixel, new Rectangle(cursorX, rect.Y + 4, 2, rect.Height - 8), _textColor);
        }
    }

    private void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect, string text, bool isHovered)
    {
        spriteBatch.Draw(pixel, rect, isHovered ? _buttonHoverColor : _buttonColor);
        DrawBorder(spriteBatch, pixel, rect, _borderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2), _textColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public bool ContainsPoint(Point p)
    {
        return IsActive;
    }
}
