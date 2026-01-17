using Microsoft.Xna.Framework;

namespace CPUgame.UI.Designer;

/// <summary>
/// Static class containing layout constants and rectangle calculations for the designer UI.
/// </summary>
public static class DesignerLayout
{
    public const int SelectorWidth = 180;
    public const int PropertiesWidth = 200;
    public const int PinEditorHeight = 120;
    public const int Padding = 8;
    public const int ItemHeight = 28;
    public const int HeaderHeight = 30;
    public const int PreviewScale = 3;
    public const int GridSize = 20;
    public const int ContextMenuItemWidth = 100;
    public const int ContextMenuItemHeight = 26;

    public static Rectangle GetSelectorRect(int screenHeight)
    {
        return new Rectangle(Padding, HeaderHeight, SelectorWidth, screenHeight - HeaderHeight - Padding * 2 - 40);
    }

    public static Rectangle GetPropertiesRect(int screenWidth, int screenHeight)
    {
        return new Rectangle(
            screenWidth - PropertiesWidth - Padding,
            HeaderHeight,
            PropertiesWidth,
            screenHeight - HeaderHeight - PinEditorHeight - Padding * 3 - 40);
    }

    public static Rectangle GetPreviewRect(int screenWidth, int screenHeight)
    {
        int left = SelectorWidth + Padding * 2;
        int right = screenWidth - PropertiesWidth - Padding * 2;
        return new Rectangle(left, HeaderHeight, right - left, screenHeight - HeaderHeight - PinEditorHeight - Padding * 3 - 40);
    }

    public static Rectangle GetPinEditorRect(int screenWidth, int screenHeight)
    {
        int left = SelectorWidth + Padding * 2;
        int right = screenWidth - PropertiesWidth - Padding * 2;
        return new Rectangle(left, screenHeight - PinEditorHeight - Padding - 40, right - left, PinEditorHeight);
    }
}
