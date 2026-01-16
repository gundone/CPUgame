using System;
using CPUgame.Converters;
using CPUgame.Core;
using CPUgame.Core.Circuit;
using CPUgame.Core.Localization;
using CPUgame.Core.Primitives;
using CPUgame.Core.Selection;
using CPUgame.Core.Services;
using CPUgame.UI;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

public interface IGameRenderer
{
    int GridSize { get; }
    Texture2D Pixel { get; }
    float TitleFontScale { get; set; }
    void Initialize(GraphicsDevice graphicsDevice, IFontService fontService);
    void DrawWorld(SpriteBatch spriteBatch, Circuit circuit, ICameraController camera, ISelectionManager selection, IWireManager wireManager, IManualWireService manualWireService, Pin? hoveredPin, Point2 mousePos, int screenWidth, int screenHeight, bool isDraggingItem);
    void DrawUI(SpriteBatch spriteBatch, IToolboxManager toolboxManager, MainMenu mainMenu, IStatusService statusService, IDialogService dialogService, ITruthTableService truthTableService, ICameraController camera, Point2 mousePos, int screenWidth, int screenHeight, SpriteFontBase font, bool isMenuVisible = true);
}

public class GameRenderer : IGameRenderer
{
    private readonly ICircuitRenderer _circuitRenderer;
    private IFontService _fontService = null!;

    public int GridSize => _circuitRenderer.GridSize;
    public Texture2D Pixel => _circuitRenderer.Pixel;
    public float TitleFontScale { get; set; } = 0.8f;

    public GameRenderer(ICircuitRenderer circuitRenderer)
    {
        _circuitRenderer = circuitRenderer;
    }

    public void Initialize(GraphicsDevice graphicsDevice, IFontService fontService)
    {
        _circuitRenderer.Initialize(graphicsDevice, fontService);
        _fontService = fontService;
    }

    public void DrawWorld(SpriteBatch spriteBatch, Circuit circuit, ICameraController camera, ISelectionManager selection, IWireManager wireManager, IManualWireService manualWireService, Pin? hoveredPin, Point2 mousePos, int screenWidth, int screenHeight, bool isDraggingItem)
    {
        // Set zoom for circuit renderer so fonts scale appropriately
        _circuitRenderer.CurrentZoom = camera.Zoom;

        _circuitRenderer.DrawGrid(spriteBatch, camera.Offset.X, camera.Offset.Y, screenWidth, screenHeight, camera.Zoom);
        _circuitRenderer.DrawCircuit(spriteBatch, circuit, selection.SelectedWire);

        // Draw component titles
        DrawComponentTitles(spriteBatch, circuit, camera.Zoom);

        if (hoveredPin != null && !isDraggingItem)
        {
            _circuitRenderer.DrawPinHighlight(spriteBatch, hoveredPin);
        }

        // Draw auto wire preview
        if (wireManager.IsDraggingWire && wireManager.WireStartPin != null)
        {
            var worldMousePos = camera.ScreenToWorld(mousePos);
            _circuitRenderer.DrawWirePreview(spriteBatch,
                new Vector2(wireManager.WireStartPin.WorldX, wireManager.WireStartPin.WorldY),
                worldMousePos);
        }

        // Draw manual wire preview
        if (manualWireService.IsActive)
        {
            var worldMousePos = camera.ScreenToWorld(mousePos);
            _circuitRenderer.DrawManualWirePreview(spriteBatch, manualWireService.PathPoints, worldMousePos);
        }

        // Draw editable nodes for selected manual wire
        if (manualWireService.IsEditingWire && manualWireService.EditingWire != null)
        {
            _circuitRenderer.DrawManualWireNodes(spriteBatch, manualWireService.EditingWire, manualWireService.DraggingNodeIndex);
        }
        else if (selection.IsSelectedWireManual && selection.SelectedWire != null)
        {
            // Show nodes for selected manual wire (but not in editing mode yet)
            _circuitRenderer.DrawManualWireNodes(spriteBatch, selection.SelectedWire, -1);
        }

        if (selection.IsSelecting)
        {
            DrawSelectionRectangle(spriteBatch, selection, camera.Zoom);
        }

        // Draw selected nodes
        foreach (var node in selection.GetSelectedNodes())
        {
            _circuitRenderer.DrawSelectedWireNode(spriteBatch, node.X, node.Y);
        }
    }

    private void DrawComponentTitles(SpriteBatch spriteBatch, Circuit circuit, float zoom)
    {
        var titleColor = new Color(180, 180, 200);
        var font = _fontService.GetFontForZoom(zoom * TitleFontScale);
        var scale = new Vector2(1f / zoom);

        foreach (var component in circuit.Components)
        {
            if (string.IsNullOrEmpty(component.Title))
            {
                continue;
            }

            var textSize = font.MeasureString(component.Title) / zoom;
            float x = component.X + (component.Width - textSize.X) / 2;
            float y = component.Y + component.Height + 2;

            font.DrawText(spriteBatch, component.Title,
                new Vector2(x, y),
                titleColor,
                scale: scale);
        }
    }

    public void DrawUI(SpriteBatch spriteBatch, IToolboxManager toolboxManager, MainMenu mainMenu, IStatusService statusService, IDialogService dialogService, ITruthTableService truthTableService, ICameraController camera, Point2 mousePos, int screenWidth, int screenHeight, SpriteFontBase font, bool isMenuVisible = true)
    {
        var mousePosMonoGame = mousePos.ToMonoGame();
        int menuHeight = isMenuVisible ? mainMenu.Height : 0;
        DrawZoomIndicator(spriteBatch, camera.Zoom, menuHeight);
        toolboxManager.MainToolbox.Draw(spriteBatch, Pixel, font, mousePosMonoGame);
        toolboxManager.UserToolbox.Draw(spriteBatch, Pixel, font, mousePosMonoGame);
        truthTableService.Draw(spriteBatch, Pixel, font, mousePosMonoGame);
        DrawStatusBar(spriteBatch, statusService.Message, screenWidth, screenHeight);

        if (isMenuVisible)
        {
            mainMenu.Draw(spriteBatch, Pixel, font, screenWidth, mousePosMonoGame);
        }

        if (dialogService.IsActive)
        {
            DrawInputDialog(spriteBatch, dialogService.DialogTitle, dialogService.InputText, screenWidth, screenHeight);
        }
    }

    private void DrawSelectionRectangle(SpriteBatch spriteBatch, ISelectionManager selection, float zoom)
    {
        int minX = Math.Min(selection.SelectionStart.X, selection.SelectionEnd.X);
        int maxX = Math.Max(selection.SelectionStart.X, selection.SelectionEnd.X);
        int minY = Math.Min(selection.SelectionStart.Y, selection.SelectionEnd.Y);
        int maxY = Math.Max(selection.SelectionStart.Y, selection.SelectionEnd.Y);

        var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        var fillColor = new Color(70, 130, 180, 50);
        var borderColor = new Color(70, 130, 180, 200);

        spriteBatch.Draw(Pixel, rect, fillColor);

        int thickness = Math.Max(1, (int)(1 / zoom));
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), borderColor);
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), borderColor);
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), borderColor);
        spriteBatch.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), borderColor);
    }

    private void DrawStatusBar(SpriteBatch spriteBatch, string statusMessage, int screenWidth, int screenHeight)
    {
        var font = _fontService.GetFont();
        var barHeight = 44;
        var barY = screenHeight - barHeight;
        spriteBatch.Draw(Pixel, new Rectangle(0, barY, screenWidth, barHeight), new Color(40, 40, 50));

        var helpText = LocalizationManager.Get("help.shortcuts");
        font.DrawText(spriteBatch, helpText, new Vector2(8, barY + 4), new Color(120, 120, 140));
        font.DrawText(spriteBatch, statusMessage, new Vector2(8, barY + 24), new Color(200, 200, 210));
    }

    private void DrawZoomIndicator(SpriteBatch spriteBatch, float zoom, int menuHeight)
    {
        var font = _fontService.GetFont();
        var zoomText = $"Zoom: {zoom:P0}";
        // Position below both menu (24px) and tab bar (32px)
        font.DrawText(spriteBatch, zoomText, new Vector2(8, menuHeight + 40), new Color(150, 150, 170));
    }

    private void DrawInputDialog(SpriteBatch spriteBatch, string title, string inputText, int screenWidth, int screenHeight)
    {
        var font = _fontService.GetFont();
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(0, 0, 0, 150));

        var hintText = LocalizationManager.Get("dialog.name_hint");
        var titleSize = font.MeasureString(title);
        var hintSize = font.MeasureString(hintText);

        int minWidth = 300;
        int contentWidth = Math.Max((int)titleSize.X, (int)hintSize.X) + 60;
        int dialogWidth = Math.Max(minWidth, contentWidth);
        int dialogHeight = 120;
        int dialogX = (screenWidth - dialogWidth) / 2;
        int dialogY = (screenHeight - dialogHeight) / 2;
        var dialogRect = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);

        spriteBatch.Draw(Pixel, dialogRect, new Color(45, 45, 55));
        DrawBorder(spriteBatch, dialogRect, new Color(80, 80, 100), 2);

        font.DrawText(spriteBatch, title,
            new Vector2(dialogX + (dialogWidth - titleSize.X) / 2, dialogY + 10),
            new Color(220, 220, 230));

        var inputRect = new Rectangle(dialogX + 20, dialogY + 40, dialogWidth - 40, 30);
        spriteBatch.Draw(Pixel, inputRect, new Color(30, 30, 40));
        DrawBorder(spriteBatch, inputRect, new Color(100, 100, 120), 1);

        var displayText = inputText + "_";
        font.DrawText(spriteBatch, displayText,
            new Vector2(inputRect.X + 5, inputRect.Y + 5),
            new Color(220, 220, 230));

        font.DrawText(spriteBatch, hintText,
            new Vector2(dialogX + (dialogWidth - hintSize.X) / 2, dialogY + dialogHeight - 30),
            new Color(150, 150, 170));
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
