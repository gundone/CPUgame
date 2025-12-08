using CPUgame.Core.Primitives;

namespace CPUgame.Core.Theme;

/// <summary>
/// Centralized color definitions for the game.
/// Platform-agnostic - uses ColorRgba.
/// </summary>
public static class GameColors
{
    // Background
    public static readonly ColorRgba Background = new(30, 30, 35);
    public static readonly ColorRgba Grid = new(45, 45, 50);

    // Components
    public static readonly ColorRgba ComponentFill = new(60, 60, 70);
    public static readonly ColorRgba ComponentBorder = new(100, 100, 120);
    public static readonly ColorRgba ComponentSelected = new(100, 180, 255);
    public static readonly ColorRgba ComponentHovered = new(120, 120, 140);

    // Wires
    public static readonly ColorRgba WireOff = new(80, 80, 90);
    public static readonly ColorRgba WireOn = new(50, 255, 100);
    public static readonly ColorRgba WireUndefined = new(255, 100, 100);
    public static readonly ColorRgba WireSelected = new(100, 180, 255);
    public static readonly ColorRgba WirePreview = new(100, 150, 200, 180);

    // Pins
    public static readonly ColorRgba Pin = new(180, 180, 200);
    public static readonly ColorRgba PinHighlight = new(255, 200, 100);

    // Wire nodes
    public static readonly ColorRgba WireNode = new(255, 200, 100);
    public static readonly ColorRgba WireNodeDragging = new(100, 200, 255);
    public static readonly ColorRgba WireNodeEndpoint = new(100, 180, 255);

    // UI
    public static readonly ColorRgba MenuBar = new(40, 40, 50);
    public static readonly ColorRgba MenuItem = new(50, 50, 60);
    public static readonly ColorRgba MenuItemHover = new(70, 70, 85);
    public static readonly ColorRgba Submenu = new(45, 45, 55, 250);
    public static readonly ColorRgba Separator = new(70, 70, 80);

    // Text
    public static readonly ColorRgba Text = new(220, 220, 230);
    public static readonly ColorRgba TextDimmed = new(150, 150, 170);
    public static readonly ColorRgba TextHeader = new(150, 180, 220);
    public static readonly ColorRgba TextKey = new(255, 200, 100);

    // Buttons
    public static readonly ColorRgba ButtonNormal = new(70, 70, 80);
    public static readonly ColorRgba ButtonHover = new(90, 90, 100);
    public static readonly ColorRgba ButtonPressed = new(50, 50, 60);
    public static readonly ColorRgba ButtonBorder = new(100, 100, 120);

    // Dialogs
    public static readonly ColorRgba DialogOverlay = new(0, 0, 0, 180);
    public static readonly ColorRgba DialogBackground = new(45, 45, 55);
    public static readonly ColorRgba DialogTitle = new(55, 55, 65);
    public static readonly ColorRgba DialogBorder = new(80, 80, 100);

    // Special components
    public static readonly ColorRgba LedOn = new(50, 255, 100);
    public static readonly ColorRgba LedOff = new(40, 60, 40);
    public static readonly ColorRgba SwitchOn = new(100, 200, 255);
    public static readonly ColorRgba SwitchOff = new(60, 80, 100);

    // Selection
    public static readonly ColorRgba SelectionRect = new(100, 180, 255, 50);
    public static readonly ColorRgba SelectionRectBorder = new(100, 180, 255);

    // Status
    public static readonly ColorRgba StatusBar = new(35, 35, 45);

    // Close button
    public static readonly ColorRgba CloseButton = new(180, 60, 60);
    public static readonly ColorRgba CloseButtonHover = new(220, 80, 80);

    // Toolbox
    public static readonly ColorRgba ToolboxBackground = new(35, 35, 45, 240);
    public static readonly ColorRgba ToolboxBorder = new(60, 60, 75);
    public static readonly ColorRgba ToolboxItemHover = new(55, 55, 70);
    public static readonly ColorRgba ToolboxItemDrag = new(80, 80, 100);
}
