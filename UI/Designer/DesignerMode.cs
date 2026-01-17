using System;
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
/// Orchestrates the various panels for editing component visual appearances.
/// </summary>
public class DesignerMode : IDesignerMode
{
    private readonly IAppearanceService _appearanceService;
    private readonly IComponentBuilder _componentBuilder;
    private readonly IFontService _fontService;
    private readonly IComponentSelectorPanel _selectorPanel;
    private readonly IPropertiesPanel _propertiesPanel;
    private readonly IPreviewPanel _previewPanel;
    private readonly IPinEditorPanel _pinEditorPanel;

    private string? _selectedComponentType;
    private ComponentAppearance? _editingAppearance;

    // Buttons
    private Rectangle _saveButtonRect;
    private Rectangle _resetButtonRect;
    private Rectangle _closeButtonRect;
    private bool _saveButtonHovered;
    private bool _resetButtonHovered;
    private bool _closeButtonHovered;

    // Save message feedback
    private double _saveMessageTimer;
    private const double SaveMessageDuration = 2.0;

    // Context menu state
    private bool _contextMenuVisible;
    private Rectangle _contextMenuRect;
    private int _contextMenuHoveredItem = -1;
    private Func<string?>? _getClipboardText;

    public bool IsActive { get; private set; }

    public event Action? OnAppearanceSaved;
    public event Action? OnCloseRequested;

    public DesignerMode(
        IAppearanceService appearanceService,
        IComponentBuilder componentBuilder,
        IFontService fontService,
        IComponentSelectorPanel selectorPanel,
        IPropertiesPanel propertiesPanel,
        IPreviewPanel previewPanel,
        IPinEditorPanel pinEditorPanel)
    {
        _appearanceService = appearanceService;
        _componentBuilder = componentBuilder;
        _fontService = fontService;
        _selectorPanel = selectorPanel;
        _propertiesPanel = propertiesPanel;
        _previewPanel = previewPanel;
        _pinEditorPanel = pinEditorPanel;

        // Subscribe to panel events
        _selectorPanel.OnComponentSelected += SelectComponent;
        _previewPanel.OnPinSelected += OnPinSelected;
        _propertiesPanel.OnContextMenuRequested += OnContextMenuRequested;
    }

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
        _selectorPanel.Reset();
        _propertiesPanel.Reset();
        _previewPanel.Reset();
        _pinEditorPanel.Reset();
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
        _contextMenuVisible = false;
        _saveMessageTimer = 0;
        _selectorPanel.Reset();
        _propertiesPanel.Reset();
        _previewPanel.Reset();
        _pinEditorPanel.Reset();
    }

    private void RefreshComponentList()
    {
        // Convert IReadOnlyDictionary<string, CircuitData> to IReadOnlyDictionary<string, object>
        var customComponents = _componentBuilder.CustomComponents
            .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        _selectorPanel.RefreshComponentList(customComponents);
    }

    private void SelectComponent(string componentType)
    {
        SaveCurrentAppearance();

        _selectedComponentType = componentType;
        _pinEditorPanel.SetSelectedPinIndex(-1);

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

        // Update panels
        _propertiesPanel.SetAppearance(_editingAppearance);
        _previewPanel.SetAppearance(_editingAppearance, _selectedComponentType);
        _pinEditorPanel.SetAppearance(_editingAppearance);
    }

    private void OnPinSelected(int pinIndex)
    {
        _pinEditorPanel.SetSelectedPinIndex(pinIndex);
    }

    private void OnContextMenuRequested(Rectangle menuRect)
    {
        _contextMenuRect = menuRect;
        _contextMenuVisible = true;
        _contextMenuHoveredItem = -1;
    }

    private void SaveCurrentAppearance()
    {
        if (_editingAppearance == null || _selectedComponentType == null)
        {
            return;
        }

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

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased,
        bool rightMouseJustPressed, int scrollDelta, int screenWidth, int screenHeight, double deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        // Calculate panel bounds
        var selectorRect = DesignerLayout.GetSelectorRect(screenHeight);
        var propertiesRect = DesignerLayout.GetPropertiesRect(screenWidth, screenHeight);
        var previewRect = DesignerLayout.GetPreviewRect(screenWidth, screenHeight);
        var pinEditorRect = DesignerLayout.GetPinEditorRect(screenWidth, screenHeight);

        // Update button rectangles (3 buttons: Save, Reset, Close)
        int buttonWidth = 80;
        int buttonHeight = 28;
        int buttonsY = screenHeight - buttonHeight - DesignerLayout.Padding;
        int totalButtonsWidth = buttonWidth * 3 + DesignerLayout.Padding * 2;
        int buttonsStartX = (screenWidth - totalButtonsWidth) / 2;
        _saveButtonRect = new Rectangle(buttonsStartX, buttonsY, buttonWidth, buttonHeight);
        _resetButtonRect = new Rectangle(buttonsStartX + buttonWidth + DesignerLayout.Padding, buttonsY, buttonWidth, buttonHeight);
        _closeButtonRect = new Rectangle(buttonsStartX + (buttonWidth + DesignerLayout.Padding) * 2, buttonsY, buttonWidth, buttonHeight);

        _saveButtonHovered = _saveButtonRect.Contains(mousePos);
        _resetButtonHovered = _resetButtonRect.Contains(mousePos);
        _closeButtonHovered = _closeButtonRect.Contains(mousePos);

        // Handle context menu
        if (_contextMenuVisible)
        {
            UpdateContextMenu(mousePos, mouseJustPressed, rightMouseJustPressed);
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
                _propertiesPanel.SetAppearance(_editingAppearance);
                _previewPanel.SetAppearance(_editingAppearance, _selectedComponentType);
                _pinEditorPanel.SetAppearance(_editingAppearance);
            }
            else if (_closeButtonHovered)
            {
                OnCloseRequested?.Invoke();
            }
        }

        // Show context menu on right-click when editing a field
        if (rightMouseJustPressed && (_propertiesPanel.IsEditing || _pinEditorPanel.IsEditingPinName))
        {
            ShowContextMenu(mousePos, screenWidth, screenHeight);
            return;
        }

        // Update save message timer
        if (_saveMessageTimer > 0)
        {
            _saveMessageTimer -= deltaTime;
        }

        // Update panels
        _selectorPanel.Update(mousePos, mouseJustPressed, scrollDelta, selectorRect);

        if (_editingAppearance != null)
        {
            _propertiesPanel.Update(mousePos, mouseJustPressed, rightMouseJustPressed, propertiesRect, screenWidth, screenHeight);
            _previewPanel.Update(mousePos, mousePressed, mouseJustPressed, mouseJustReleased, previewRect);
            _pinEditorPanel.Update(mousePos, mouseJustPressed, pinEditorRect, _fontService);
        }
    }

    private void UpdateContextMenu(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed)
    {
        _contextMenuHoveredItem = -1;
        if (_contextMenuRect.Contains(mousePos))
        {
            int itemY = mousePos.Y - _contextMenuRect.Y;
            _contextMenuHoveredItem = itemY / DesignerLayout.ContextMenuItemHeight;
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
    }

    private void ShowContextMenu(Point mousePos, int screenWidth, int screenHeight)
    {
        int menuWidth = DesignerLayout.ContextMenuItemWidth;
        int menuHeight = DesignerLayout.ContextMenuItemHeight; // Just "Paste" for now

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

    public void HandleTextInput(char character)
    {
        if (_pinEditorPanel.IsEditingPinName)
        {
            _pinEditorPanel.HandleTextInput(character);
        }
        else if (_propertiesPanel.IsEditing)
        {
            _propertiesPanel.HandleTextInput(character);
        }
    }

    public void HandleKeyPress(bool backspace, bool enter, bool escape)
    {
        if (_pinEditorPanel.IsEditingPinName)
        {
            _pinEditorPanel.HandleKeyPress(backspace, enter, escape);
        }
        else if (_propertiesPanel.IsEditing)
        {
            _propertiesPanel.HandleKeyPress(backspace, enter, escape);
        }
    }

    public void HandlePaste(string text)
    {
        if (_pinEditorPanel.IsEditingPinName)
        {
            _pinEditorPanel.HandlePaste(text);
        }
        else if (_propertiesPanel.IsEditing)
        {
            _propertiesPanel.HandlePaste(text);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight, Point mousePos)
    {
        if (!IsActive)
        {
            return;
        }

        // Background
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), DesignerColors.BackgroundColor);

        // Draw panels
        var selectorRect = DesignerLayout.GetSelectorRect(screenHeight);
        var propertiesRect = DesignerLayout.GetPropertiesRect(screenWidth, screenHeight);
        var previewRect = DesignerLayout.GetPreviewRect(screenWidth, screenHeight);
        var pinEditorRect = DesignerLayout.GetPinEditorRect(screenWidth, screenHeight);

        _selectorPanel.Draw(spriteBatch, pixel, font, selectorRect);
        _propertiesPanel.Draw(spriteBatch, pixel, font, propertiesRect);
        _previewPanel.Draw(spriteBatch, pixel, font, previewRect, _fontService);
        _pinEditorPanel.Draw(spriteBatch, pixel, font, pinEditorRect);

        // Draw buttons
        DesignerDrawing.DrawButton(spriteBatch, pixel, font, _saveButtonRect, LocalizationManager.Get("designer.save"), _saveButtonHovered);
        DesignerDrawing.DrawButton(spriteBatch, pixel, font, _resetButtonRect, LocalizationManager.Get("designer.reset_default"), _resetButtonHovered);
        DesignerDrawing.DrawButton(spriteBatch, pixel, font, _closeButtonRect, LocalizationManager.Get("designer.close"), _closeButtonHovered);

        // Draw save confirmation message (fading over 2 seconds) in bottom right corner
        if (_saveMessageTimer > 0)
        {
            float alpha = (float)(_saveMessageTimer / SaveMessageDuration);
            var messageColor = new Color(DesignerColors.TextColor.R, DesignerColors.TextColor.G, DesignerColors.TextColor.B, (byte)(alpha * 255));
            var message = "Ok, saved";
            var messageSize = font.MeasureString(message);
            float messageX = screenWidth - messageSize.X - DesignerLayout.Padding;
            float messageY = screenHeight - messageSize.Y - DesignerLayout.Padding;
            font.DrawText(spriteBatch, message, new Vector2(messageX, messageY), messageColor);
        }

        // Title
        var title = LocalizationManager.Get("designer.title");
        var titleSize = font.MeasureString(title);
        font.DrawText(spriteBatch, title, new Vector2((screenWidth - titleSize.X) / 2, 4), DesignerColors.TextColor);

        // Draw context menu (on top of everything)
        if (_contextMenuVisible)
        {
            DrawContextMenu(spriteBatch, pixel, font);
        }
    }

    private void DrawContextMenu(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font)
    {
        // Background with border
        spriteBatch.Draw(pixel, _contextMenuRect, DesignerColors.PanelColor);
        DesignerDrawing.DrawBorder(spriteBatch, pixel, _contextMenuRect, DesignerColors.BorderColor, 1);

        // Paste item
        var itemRect = new Rectangle(_contextMenuRect.X, _contextMenuRect.Y, _contextMenuRect.Width, DesignerLayout.ContextMenuItemHeight);
        if (_contextMenuHoveredItem == 0)
        {
            spriteBatch.Draw(pixel, itemRect, DesignerColors.HoverColor);
        }

        string pasteText = LocalizationManager.Get("designer.paste");
        var pasteSize = font.MeasureString(pasteText);
        float textX = itemRect.X + DesignerLayout.Padding;
        float textY = itemRect.Y + (itemRect.Height - pasteSize.Y) / 2;
        font.DrawText(spriteBatch, pasteText, new Vector2(textX, textY), DesignerColors.TextColor);
    }
}
