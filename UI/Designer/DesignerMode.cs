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
    private const int SelectorWidth = 180;
    private const int PropertiesWidth = 200;
    private const int PinEditorHeight = 120;
    private const int Padding = 8;
    private const int ItemHeight = 28;
    private const int HeaderHeight = 30;

    // Colors
    private static readonly Color BackgroundColor = new(35, 35, 45);
    private static readonly Color PanelColor = new(45, 45, 55);
    private static readonly Color HeaderColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(70, 70, 85);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color DimTextColor = new(140, 140, 160);
    private static readonly Color SelectedColor = new(70, 100, 140);
    private static readonly Color HoverColor = new(60, 60, 75);
    private static readonly Color InputFieldColor = new(35, 35, 45);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color PinColor = new(100, 180, 100);
    private static readonly Color PinSelectedColor = new(255, 200, 100);
    private static readonly Color GridColor = new(50, 50, 60);
    private static readonly Color ComponentBodyColor = new(60, 60, 70);

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
    private const int FieldWidth = 0;
    private const int FieldHeight = 1;
    private const int FieldTitle = 2;
    private const int FieldFontScale = 3;
    private const int FieldCustomColor = 4;
    private const int FieldPinName = 5;

    // Pin editing state
    private int _editingPinIndex = -1;
    private bool _editingPinIsInput;

    // Title dragging state
    private bool _isDraggingTitle;
    private Rectangle _titleRect;

    // Preset colors for component fill
    private static readonly (string name, Color color)[] PresetColors =
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
    private const int PreviewScale = 3;
    private const int GridSize = 20;

    // Buttons
    private Rectangle _saveButtonRect;
    private Rectangle _resetButtonRect;
    private bool _saveButtonHovered;
    private bool _resetButtonHovered;

    // Save message feedback
    private double _saveMessageTimer;
    private const double SaveMessageDuration = 2.0;

    // Context menu state
    private bool _contextMenuVisible;
    private Rectangle _contextMenuRect;
    private int _contextMenuHoveredItem = -1;
    private const int ContextMenuItemWidth = 100;
    private const int ContextMenuItemHeight = 26;
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
        _editingPinIndex = -1;
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
        _editingPinIndex = -1;

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
            // For custom components, save appearance directly to component file
            if (_selectedComponentType.StartsWith("Custom:"))
            {
                var customName = _selectedComponentType.Substring(7);
                _appearanceService.UpdateCustomComponentAppearance(customName, _editingAppearance);
            }
            else
            {
                // For built-in components, keep in memory
                _appearanceService.SetAppearance(_editingAppearance);
            }

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
        int buttonsY = screenHeight - buttonHeight - Padding;
        _saveButtonRect = new Rectangle(screenWidth / 2 - buttonWidth - Padding, buttonsY, buttonWidth, buttonHeight);
        _resetButtonRect = new Rectangle(screenWidth / 2 + Padding, buttonsY, buttonWidth, buttonHeight);

        _saveButtonHovered = _saveButtonRect.Contains(mousePos);
        _resetButtonHovered = _resetButtonRect.Contains(mousePos);

        // Handle context menu
        if (_contextMenuVisible)
        {
            _contextMenuHoveredItem = -1;
            if (_contextMenuRect.Contains(mousePos))
            {
                int itemY = mousePos.Y - _contextMenuRect.Y;
                _contextMenuHoveredItem = itemY / ContextMenuItemHeight;
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
                _saveMessageTimer = SaveMessageDuration;
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
        int menuWidth = ContextMenuItemWidth;
        int menuHeight = ContextMenuItemHeight; // Just "Paste" for now

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
            int y = selectorRect.Y + HeaderHeight + Padding - _selectorScrollOffset * ItemHeight;

            // Skip "Built-in" header
            y += ItemHeight;

            for (int i = 0; i < _componentTypes.Count; i++)
            {
                var componentType = _componentTypes[i];

                // Skip "Custom" header when transitioning from built-in to custom
                if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
                {
                    y += ItemHeight;
                }

                var itemRect = new Rectangle(selectorRect.X + Padding, y, selectorRect.Width - Padding * 2, ItemHeight);
                if (itemRect.Contains(mousePos))
                {
                    _hoveredComponentIndex = i;

                    if (mouseJustPressed)
                    {
                        SelectComponent(_componentTypes[i]);
                    }
                    break;
                }
                y += ItemHeight;
            }
        }
    }

    private void UpdateProperties(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed,
        Rectangle propertiesRect, int screenWidth, int screenHeight)
    {
        if (propertiesRect.Contains(mousePos) && _editingAppearance != null)
        {
            // Calculate property item positions
            int y = propertiesRect.Y + HeaderHeight + Padding;
            int fieldHeight = 26;
            int spacing = 4;
            int labelWidth = propertiesRect.Width / 2 - Padding;

            // Width field (display in grid cells)
            var widthRect = new Rectangle(propertiesRect.X + labelWidth + Padding, y, propertiesRect.Width - labelWidth - Padding * 2, fieldHeight);
            if (widthRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != FieldWidth)
                {
                    StartEditing(FieldWidth, (_editingAppearance.Width / GridSize).ToString());
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(FieldWidth, (_editingAppearance.Width / GridSize).ToString());
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Height field (display in grid cells)
            var heightRect = new Rectangle(propertiesRect.X + labelWidth + Padding, y, propertiesRect.Width - labelWidth - Padding * 2, fieldHeight);
            if (heightRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != FieldHeight)
                {
                    StartEditing(FieldHeight, (_editingAppearance.Height / GridSize).ToString());
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(FieldHeight, (_editingAppearance.Height / GridSize).ToString());
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Title field
            var titleRect = new Rectangle(propertiesRect.X + labelWidth + Padding, y, propertiesRect.Width - labelWidth - Padding * 2, fieldHeight);
            if (titleRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != FieldTitle)
                {
                    StartEditing(FieldTitle, _editingAppearance.Title);
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(FieldTitle, _editingAppearance.Title);
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Font scale field
            var fontScaleRect = new Rectangle(propertiesRect.X + labelWidth + Padding, y, propertiesRect.Width - labelWidth - Padding * 2, fieldHeight);
            if (fontScaleRect.Contains(mousePos))
            {
                if (mouseJustPressed && _editingFieldIndex != FieldFontScale)
                {
                    StartEditing(FieldFontScale, _editingAppearance.TitleFontScale.ToString("0.0"));
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(FieldFontScale, _editingAppearance.TitleFontScale.ToString("0.0"));
                    ShowContextMenu(mousePos, screenWidth, screenHeight);
                }
            }
            y += fieldHeight + spacing;

            // Color label row
            y += fieldHeight + spacing;

            // Color swatches
            int swatchSize = 20;
            int swatchSpacing = 4;
            int swatchesPerRow = (propertiesRect.Width - Padding * 2) / (swatchSize + swatchSpacing);
            int swatchX = propertiesRect.X + Padding;

            for (int i = 0; i < PresetColors.Length; i++)
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
                        _editingAppearance.FillColor = ColorToHex(PresetColors[i].color);
                    }
                    _editingFieldIndex = -1; // Stop editing custom color field
                }

                swatchX += swatchSize + swatchSpacing;
                if ((i + 1) % swatchesPerRow == 0)
                {
                    swatchX = propertiesRect.X + Padding;
                    y += swatchSize + swatchSpacing;
                }
            }

            // Custom color row (after swatches)
            // Ensure we're on a new row
            if (swatchX != propertiesRect.X + Padding)
            {
                y += swatchSize + swatchSpacing;
            }
            y += Padding;

            // Custom color field
            var customColorRect = new Rectangle(propertiesRect.X + Padding, y, propertiesRect.Width - Padding * 2, fieldHeight);
            if (customColorRect.Contains(mousePos))
            {
                string currentHex = _editingAppearance.FillColor ?? "#3C3C46";
                if (mouseJustPressed && _editingFieldIndex != FieldCustomColor)
                {
                    StartEditing(FieldCustomColor, currentHex);
                }
                else if (rightMouseJustPressed)
                {
                    StartEditing(FieldCustomColor, currentHex);
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
            return PresetColors[0].color; // Default
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
        int compWidth = _editingAppearance.Width * PreviewScale;
        int compHeight = _editingAppearance.Height * PreviewScale;
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
                    int pinX = compX + pin.LocalX * PreviewScale;
                    int pinY = compY + pin.LocalY * PreviewScale;
                    int pinRadius = 8 * PreviewScale;

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
            _editingAppearance.TitleOffsetX = (mousePos.X - centerX) / PreviewScale;
            _editingAppearance.TitleOffsetY = (mousePos.Y - centerY) / PreviewScale;
        }

        // Handle pin dragging
        if (_isDraggingPin && mousePressed && _selectedPinIndex >= 0)
        {
            // Update pin position
            int localX = (mousePos.X - compX) / PreviewScale;
            int localY = (mousePos.Y - compY) / PreviewScale;

            // Snap to grid (use game's grid size)
            localX = (int)Math.Round((double)localX / GridSize) * GridSize;
            localY = (int)Math.Round((double)localY / GridSize) * GridSize;

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
        if (_editingAppearance == null || !pinEditorRect.Contains(mousePos))
        {
            return;
        }

        if (!mouseJustPressed)
        {
            return;
        }

        // Calculate pin name positions (must match DrawPinEditor layout exactly)
        int x = pinEditorRect.X + Padding;
        int y = pinEditorRect.Y + HeaderHeight + Padding;
        int pinIndex = 0;

        // Get font for measuring text
        var font = _fontService.GetFont(1.0f);

        // Check input pins
        foreach (var pin in _editingAppearance.InputPins)
        {
            // Measure just the pin name (without coordinates)
            var nameSize = font.MeasureString(pin.Name);
            var nameRect = new Rectangle(x, y, (int)nameSize.X, (int)nameSize.Y);

            if (nameRect.Contains(mousePos))
            {
                // Start editing this pin name
                _editingFieldIndex = FieldPinName;
                _editingPinIndex = pinIndex;
                _editingPinIsInput = true;
                _editingFieldText = pin.Name;
                return;
            }

            x += 150;
            if (x > pinEditorRect.Right - 150)
            {
                x = pinEditorRect.X + Padding;
                y += 20;
            }
            pinIndex++;
        }

        // Check output pins
        foreach (var pin in _editingAppearance.OutputPins)
        {
            // Measure just the pin name (without coordinates)
            var nameSize = font.MeasureString(pin.Name);
            var nameRect = new Rectangle(x, y, (int)nameSize.X, (int)nameSize.Y);

            if (nameRect.Contains(mousePos))
            {
                // Start editing this pin name
                _editingFieldIndex = FieldPinName;
                _editingPinIndex = pinIndex;
                _editingPinIsInput = false;
                _editingFieldText = pin.Name;
                return;
            }

            x += 150;
            if (x > pinEditorRect.Right - 150)
            {
                x = pinEditorRect.X + Padding;
                y += 20;
            }
            pinIndex++;
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
        else if (_editingFieldIndex == FieldPinName)
        {
            // Pin name accepts alphanumeric and underscore
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    _editingFieldText += c;
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
            else if (_editingFieldIndex == FieldPinName)
            {
                // Pin name accepts alphanumeric and underscore
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    _editingFieldText += character;
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

        if (_editingFieldIndex == FieldTitle)
        {
            _editingAppearance.Title = _editingFieldText;
        }
        else if (_editingFieldIndex == FieldFontScale)
        {
            // Parse and clamp font scale (0.5 to 3.0)
            if (float.TryParse(_editingFieldText, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                _editingAppearance.TitleFontScale = Math.Clamp(scale, 0.5f, 3.0f);
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
            if (hex.Length == 7 && IsValidHexColor(hex))
            {
                _editingAppearance.FillColor = hex.ToUpper();
            }
        }
        else if (_editingFieldIndex == FieldPinName)
        {
            // Validate and apply pin name
            string newName = _editingFieldText.Trim();
            if (!string.IsNullOrEmpty(newName) && _editingPinIndex >= 0)
            {
                // Check for duplicate names
                bool isDuplicate = false;
                foreach (var pin in _editingAppearance.InputPins.Concat(_editingAppearance.OutputPins))
                {
                    if (pin.Name == newName && pin != (_editingPinIsInput
                        ? _editingAppearance.InputPins[_editingPinIndex]
                        : _editingAppearance.OutputPins[_editingPinIndex - _editingAppearance.InputPins.Count]))
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
                        _editingAppearance.InputPins[_editingPinIndex].Name = newName;
                    }
                    else
                    {
                        int outputIndex = _editingPinIndex - _editingAppearance.InputPins.Count;
                        _editingAppearance.OutputPins[outputIndex].Name = newName;
                    }
                }
            }
        }
        else if (int.TryParse(_editingFieldText, out int gridCells))
        {
            // Convert grid cells to pixels (minimum 1 cell)
            gridCells = Math.Max(1, gridCells);
            int pixels = gridCells * GridSize;

            if (_editingFieldIndex == FieldWidth)
            {
                _editingAppearance.Width = pixels;
            }
            else if (_editingFieldIndex == FieldHeight)
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
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), BackgroundColor);

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
            float alpha = (float)(_saveMessageTimer / SaveMessageDuration);
            var messageColor = new Color(TextColor.R, TextColor.G, TextColor.B, (byte)(alpha * 255));
            var message = "Ok, saved";
            var messageSize = font.MeasureString(message);
            float messageX = screenWidth - messageSize.X - Padding;
            float messageY = screenHeight - messageSize.Y - Padding;
            font.DrawText(spriteBatch, message, new Vector2(messageX, messageY), messageColor);
        }

        // Title
        var title = LocalizationManager.Get("designer.title");
        var titleSize = font.MeasureString(title);
        font.DrawText(spriteBatch, title, new Vector2((screenWidth - titleSize.X) / 2, 4), TextColor);

        // Draw context menu (on top of everything)
        if (_contextMenuVisible)
        {
            DrawContextMenu(spriteBatch, pixel, font);
        }
    }

    private void DrawContextMenu(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        // Background with border
        spriteBatch.Draw(pixel, _contextMenuRect, PanelColor);
        DrawBorder(spriteBatch, pixel, _contextMenuRect, BorderColor, 1);

        // Paste item
        var itemRect = new Rectangle(_contextMenuRect.X, _contextMenuRect.Y, _contextMenuRect.Width, ContextMenuItemHeight);
        if (_contextMenuHoveredItem == 0)
        {
            spriteBatch.Draw(pixel, itemRect, HoverColor);
        }

        string pasteText = LocalizationManager.Get("designer.paste");
        var pasteSize = font.MeasureString(pasteText);
        float textX = itemRect.X + Padding;
        float textY = itemRect.Y + (itemRect.Height - pasteSize.Y) / 2;
        font.DrawText(spriteBatch, pasteText, new Vector2(textX, textY), TextColor);
    }

    private Rectangle GetSelectorRect(int screenHeight)
    {
        return new Rectangle(Padding, HeaderHeight, SelectorWidth, screenHeight - HeaderHeight - Padding * 2 - 40);
    }

    private Rectangle GetPropertiesRect(int screenWidth, int screenHeight)
    {
        return new Rectangle(screenWidth - PropertiesWidth - Padding, HeaderHeight, PropertiesWidth, screenHeight - HeaderHeight - PinEditorHeight - Padding * 3 - 40);
    }

    private Rectangle GetPreviewRect(int screenWidth, int screenHeight)
    {
        int left = SelectorWidth + Padding * 2;
        int right = screenWidth - PropertiesWidth - Padding * 2;
        return new Rectangle(left, HeaderHeight, right - left, screenHeight - HeaderHeight - PinEditorHeight - Padding * 3 - 40);
    }

    private Rectangle GetPinEditorRect(int screenWidth, int screenHeight)
    {
        int left = SelectorWidth + Padding * 2;
        int right = screenWidth - PropertiesWidth - Padding * 2;
        return new Rectangle(left, screenHeight - PinEditorHeight - Padding - 40, right - left, PinEditorHeight);
    }

    private void DrawSelector(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, PanelColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, HeaderHeight);
        spriteBatch.Draw(pixel, headerRect, HeaderColor);

        var headerText = LocalizationManager.Get("designer.select_component");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (HeaderHeight - headerSize.Y) / 2), TextColor);

        // Component list
        int y = rect.Y + HeaderHeight + Padding - _selectorScrollOffset * ItemHeight;

        // Built-in header
        if (y > rect.Y + HeaderHeight)
        {
            font.DrawText(spriteBatch, LocalizationManager.Get("designer.builtin"), new Vector2(rect.X + Padding, y), DimTextColor);
        }
        y += ItemHeight;

        for (int i = 0; i < _componentTypes.Count; i++)
        {
            var componentType = _componentTypes[i];
            var itemRect = new Rectangle(rect.X + Padding, y, rect.Width - Padding * 2, ItemHeight);

            // Skip if outside visible area
            if (y + ItemHeight < rect.Y + HeaderHeight || y > rect.Bottom)
            {
                y += ItemHeight;
                continue;
            }

            // Custom components header
            if (componentType.StartsWith("Custom:") && i > 0 && !_componentTypes[i - 1].StartsWith("Custom:"))
            {
                font.DrawText(spriteBatch, LocalizationManager.Get("designer.custom"), new Vector2(rect.X + Padding, y), DimTextColor);
                y += ItemHeight;
                itemRect = new Rectangle(rect.X + Padding, y, rect.Width - Padding * 2, ItemHeight);
            }

            // Draw item
            bool isSelected = componentType == _selectedComponentType;
            bool isHovered = i == _hoveredComponentIndex;

            if (isSelected)
            {
                spriteBatch.Draw(pixel, itemRect, SelectedColor);
            }
            else if (isHovered)
            {
                spriteBatch.Draw(pixel, itemRect, HoverColor);
            }

            string displayName = componentType.StartsWith("Custom:") ? componentType.Substring(7) : componentType;
            font.DrawText(spriteBatch, displayName, new Vector2(itemRect.X + 4, itemRect.Y + (itemRect.Height - font.MeasureString(displayName).Y) / 2), TextColor);

            y += ItemHeight;
        }
    }

    private void DrawProperties(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, PanelColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, HeaderHeight);
        spriteBatch.Draw(pixel, headerRect, HeaderColor);

        var headerText = LocalizationManager.Get("designer.properties");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (HeaderHeight - headerSize.Y) / 2), TextColor);

        if (_editingAppearance == null)
        {
            return;
        }

        int y = rect.Y + HeaderHeight + Padding;
        int labelWidth = rect.Width / 2 - Padding;
        int fieldHeight = 26;
        int spacing = 4;

        // Width (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.width"), new Vector2(rect.X + Padding, y + 4), TextColor);
        var widthRect = new Rectangle(rect.X + labelWidth + Padding, y, rect.Width - labelWidth - Padding * 2, fieldHeight);
        string widthText = _editingFieldIndex == FieldWidth ? _editingFieldText : (_editingAppearance.Width / GridSize).ToString();
        DrawTextField(spriteBatch, pixel, font, widthRect, widthText, _editingFieldIndex == FieldWidth);
        y += fieldHeight + spacing;

        // Height (in grid cells)
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.height"), new Vector2(rect.X + Padding, y + 4), TextColor);
        var heightRect = new Rectangle(rect.X + labelWidth + Padding, y, rect.Width - labelWidth - Padding * 2, fieldHeight);
        string heightText = _editingFieldIndex == FieldHeight ? _editingFieldText : (_editingAppearance.Height / GridSize).ToString();
        DrawTextField(spriteBatch, pixel, font, heightRect, heightText, _editingFieldIndex == FieldHeight);
        y += fieldHeight + spacing;

        // Title
        font.DrawText(spriteBatch, "Title", new Vector2(rect.X + Padding, y + 4), TextColor);
        var titleRect = new Rectangle(rect.X + labelWidth + Padding, y, rect.Width - labelWidth - Padding * 2, fieldHeight);
        string titleText = _editingFieldIndex == FieldTitle ? _editingFieldText : _editingAppearance.Title;
        DrawTextField(spriteBatch, pixel, font, titleRect, titleText, _editingFieldIndex == FieldTitle);
        y += fieldHeight + spacing;

        // Font Scale
        font.DrawText(spriteBatch, LocalizationManager.Get("designer.font_scale"), new Vector2(rect.X + Padding, y + 4), TextColor);
        var fontScaleRect = new Rectangle(rect.X + labelWidth + Padding, y, rect.Width - labelWidth - Padding * 2, fieldHeight);
        string fontScaleText = _editingFieldIndex == FieldFontScale
            ? _editingFieldText
            : _editingAppearance.TitleFontScale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        DrawTextField(spriteBatch, pixel, font, fontScaleRect, fontScaleText, _editingFieldIndex == FieldFontScale);
        y += fieldHeight + spacing;

        // Color label
        font.DrawText(spriteBatch, "Color", new Vector2(rect.X + Padding, y + 4), TextColor);
        y += fieldHeight + spacing;

        // Color swatches
        int swatchSize = 20;
        int swatchSpacing = 4;
        int swatchesPerRow = (rect.Width - Padding * 2) / (swatchSize + swatchSpacing);
        int swatchX = rect.X + Padding;
        var currentColor = GetCurrentFillColor();

        for (int i = 0; i < PresetColors.Length; i++)
        {
            var swatchRect = new Rectangle(swatchX, y, swatchSize, swatchSize);

            // Draw swatch
            spriteBatch.Draw(pixel, swatchRect, PresetColors[i].color);

            // Draw selection border if this is the current color
            bool isSelected = (i == 0 && _editingAppearance.FillColor == null) ||
                              (i > 0 && _editingAppearance.FillColor == ColorToHex(PresetColors[i].color));
            if (isSelected)
            {
                DrawBorder(spriteBatch, pixel, swatchRect, TextColor, 2);
            }
            else
            {
                DrawBorder(spriteBatch, pixel, swatchRect, BorderColor, 1);
            }

            swatchX += swatchSize + swatchSpacing;
            if ((i + 1) % swatchesPerRow == 0)
            {
                swatchX = rect.X + Padding;
                y += swatchSize + swatchSpacing;
            }
        }

        // Custom color row (after swatches)
        if (swatchX != rect.X + Padding)
        {
            y += swatchSize + swatchSpacing;
        }
        y += Padding;

        // Custom color input with preview
        int previewSize = fieldHeight - 4;
        var previewRect = new Rectangle(rect.X + Padding, y + 2, previewSize, previewSize);
        var currentFillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, previewRect, currentFillColor);
        DrawBorder(spriteBatch, pixel, previewRect, BorderColor, 1);

        // Custom color text field
        var customColorRect = new Rectangle(rect.X + Padding + previewSize + 4, y, rect.Width - Padding * 2 - previewSize - 4, fieldHeight);
        string customColorText = _editingFieldIndex == FieldCustomColor
            ? _editingFieldText
            : (_editingAppearance.FillColor ?? "#3C3C46");
        DrawTextField(spriteBatch, pixel, font, customColorRect, customColorText, _editingFieldIndex == FieldCustomColor);
    }

    private void DrawPreview(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, PanelColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        if (_editingAppearance == null)
        {
            // Show hint
            var hint = LocalizationManager.Get("designer.select_component");
            var hintSize = font.MeasureString(hint);
            font.DrawText(spriteBatch, hint, new Vector2(rect.X + (rect.Width - hintSize.X) / 2, rect.Y + (rect.Height - hintSize.Y) / 2), DimTextColor);
            return;
        }

        // Calculate component position first (needed for grid alignment)
        int compWidth = _editingAppearance.Width * PreviewScale;
        int compHeight = _editingAppearance.Height * PreviewScale;
        int compX = rect.X + (rect.Width - compWidth) / 2;
        int compY = rect.Y + (rect.Height - compHeight) / 2;

        // Draw grid aligned with component position
        int gridStep = GridSize * PreviewScale;

        // Draw vertical grid lines (aligned with component's left edge)
        int startX = compX - ((compX - rect.X) / gridStep + 1) * gridStep;
        for (int x = startX; x < rect.Right; x += gridStep)
        {
            if (x >= rect.X)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, rect.Y, 1, rect.Height), GridColor);
            }
        }

        // Draw horizontal grid lines (aligned with component's top edge)
        int startY = compY - ((compY - rect.Y) / gridStep + 1) * gridStep;
        for (int y = startY; y < rect.Bottom; y += gridStep)
        {
            if (y >= rect.Y)
            {
                spriteBatch.Draw(pixel, new Rectangle(rect.X, y, rect.Width, 1), GridColor);
            }
        }

        // Draw component body with selected fill color
        var fillColor = GetCurrentFillColor();
        spriteBatch.Draw(pixel, new Rectangle(compX, compY, compWidth, compHeight), fillColor);
        DrawBorder(spriteBatch, pixel, new Rectangle(compX, compY, compWidth, compHeight), BorderColor, 2);

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
        float titleFontScale = PreviewScale * _editingAppearance.TitleFontScale;
        var previewFont = _fontService.GetFont(titleFontScale);
        var nameSize = previewFont.MeasureString(displayName);

        // Calculate title position with offset (offset is in game pixels, convert to preview)
        float centerX = compX + compWidth / 2f;
        float centerY = compY + compHeight / 2f;
        float nameX = centerX - nameSize.X / 2 + _editingAppearance.TitleOffsetX * PreviewScale;
        float nameY = centerY - nameSize.Y / 2 + _editingAppearance.TitleOffsetY * PreviewScale;

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
            DrawBorder(spriteBatch, pixel, _titleRect, PinSelectedColor, 1);
        }

        previewFont.DrawText(spriteBatch, displayName, new Vector2(nameX, nameY), TextColor);

        // Draw pins (use preview font without title scale)
        var pinFont = _fontService.GetFont(PreviewScale);
        int pinIndex = 0;
        foreach (var pin in _editingAppearance.InputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * PreviewScale, compY + pin.LocalY * PreviewScale, pin.Name, pinIndex == _selectedPinIndex, true);
            pinIndex++;
        }
        foreach (var pin in _editingAppearance.OutputPins)
        {
            DrawPin(spriteBatch, pixel, pinFont, compX + pin.LocalX * PreviewScale, compY + pin.LocalY * PreviewScale, pin.Name, pinIndex == _selectedPinIndex, false);
            pinIndex++;
        }

        // Draw hint
        var dragHint = LocalizationManager.Get("designer.drag_pins");
        var dragHintSize = font.MeasureString(dragHint);
        font.DrawText(spriteBatch, dragHint, new Vector2(rect.X + (rect.Width - dragHintSize.X) / 2, rect.Bottom - dragHintSize.Y - Padding), DimTextColor);
    }

    private void DrawPin(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int x, int y, string name, bool isSelected, bool isInput)
    {
        // Scale pin size to match preview scale
        int basePinSize = isSelected ? 12 : 8;
        int pinSize = basePinSize * PreviewScale / 2; // Scale but not too large
        var pinColor = isSelected ? PinSelectedColor : PinColor;

        spriteBatch.Draw(pixel, new Rectangle(x - pinSize / 2, y - pinSize / 2, pinSize, pinSize), pinColor);

        // Draw pin name (font is already at preview scale size)
        var nameSize = font.MeasureString(name);
        int offset = 8 * PreviewScale;
        float nameX = isInput ? x - nameSize.X - offset : x + offset;
        float nameY = y - nameSize.Y / 2;
        font.DrawText(spriteBatch, name, new Vector2(nameX, nameY), TextColor);
    }

    private void DrawPinEditor(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect)
    {
        // Panel background
        spriteBatch.Draw(pixel, rect, PanelColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        // Header
        var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, HeaderHeight);
        spriteBatch.Draw(pixel, headerRect, HeaderColor);

        var headerText = LocalizationManager.Get("designer.pin_positions");
        var headerSize = font.MeasureString(headerText);
        font.DrawText(spriteBatch, headerText, new Vector2(rect.X + (rect.Width - headerSize.X) / 2, rect.Y + (HeaderHeight - headerSize.Y) / 2), TextColor);

        if (_editingAppearance == null)
        {
            return;
        }

        // Draw pin list
        int x = rect.X + Padding;
        int y = rect.Y + HeaderHeight + Padding;
        int pinIndex = 0;
        int inputCount = _editingAppearance.InputPins.Count;

        foreach (var pin in _editingAppearance.InputPins.Concat(_editingAppearance.OutputPins))
        {
            bool isSelected = pinIndex == _selectedPinIndex;
            bool isEditing = _editingFieldIndex == FieldPinName && _editingPinIndex == pinIndex;

            // Determine if this is an input pin
            bool isInputPin = pinIndex < inputCount;

            if (isEditing)
            {
                // Draw pin name as editable field
                var nameSize = font.MeasureString(_editingFieldText);
                var nameRect = new Rectangle(x, y - 2, (int)nameSize.X + 8, 18);
                DrawTextField(spriteBatch, pixel, font, nameRect, _editingFieldText, true);

                // Draw coordinates after the editable field
                var coordText = $": X={pin.LocalX}, Y={pin.LocalY}";
                font.DrawText(spriteBatch, coordText, new Vector2(nameRect.Right + 2, y), TextColor);
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

                font.DrawText(spriteBatch, pin.Name, new Vector2(x, y), isSelected ? PinSelectedColor : TextColor);

                // Draw coordinates
                var coordText = $": X={pin.LocalX}, Y={pin.LocalY}";
                font.DrawText(spriteBatch, coordText, new Vector2(x + nameSize.X, y), DimTextColor);
            }

            x += 150;
            if (x > rect.Right - 150)
            {
                x = rect.X + Padding;
                y += 20;
            }
            pinIndex++;
        }
    }

    private void DrawTextField(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect, string text, bool isEditing)
    {
        spriteBatch.Draw(pixel, rect, InputFieldColor);
        DrawBorder(spriteBatch, pixel, rect, isEditing ? SelectedColor : BorderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + 4, rect.Y + (rect.Height - textSize.Y) / 2), TextColor);

        if (isEditing)
        {
            // Draw cursor
            int cursorX = rect.X + 4 + (int)textSize.X;
            spriteBatch.Draw(pixel, new Rectangle(cursorX, rect.Y + 4, 2, rect.Height - 8), TextColor);
        }
    }

    private void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle rect, string text, bool isHovered)
    {
        spriteBatch.Draw(pixel, rect, isHovered ? ButtonHoverColor : ButtonColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2), TextColor);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
