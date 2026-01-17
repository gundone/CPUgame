using System;
using Microsoft.Xna.Framework;

namespace CPUgame.UI.Designer;

/// <summary>
/// Static class containing all color definitions used by the designer UI.
/// </summary>
public static class DesignerColors
{
    public static readonly Color BackgroundColor = new(35, 35, 45);
    public static readonly Color PanelColor = new(45, 45, 55);
    public static readonly Color HeaderColor = new(55, 55, 65);
    public static readonly Color BorderColor = new(70, 70, 85);
    public static readonly Color TextColor = new(220, 220, 230);
    public static readonly Color DimTextColor = new(140, 140, 160);
    public static readonly Color SelectedColor = new(70, 100, 140);
    public static readonly Color HoverColor = new(60, 60, 75);
    public static readonly Color InputFieldColor = new(35, 35, 45);
    public static readonly Color ButtonColor = new(60, 100, 140);
    public static readonly Color ButtonHoverColor = new(80, 120, 160);
    public static readonly Color PinColor = new(100, 180, 100);
    public static readonly Color PinSelectedColor = new(255, 200, 100);
    public static readonly Color GridColor = new(50, 50, 60);
    public static readonly Color ComponentBodyColor = new(60, 60, 70);

    /// <summary>
    /// Preset colors for component fill color selection.
    /// </summary>
    public static readonly (string name, Color color)[] PresetColors =
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

    /// <summary>
    /// Converts a Color to a hex string (e.g., "#RRGGBB").
    /// </summary>
    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Converts a hex string to a Color.
    /// </summary>
    public static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length < 7)
        {
            return PresetColors[0].color; // Default
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
            return PresetColors[0].color; // Default on parse error
        }
    }

    /// <summary>
    /// Validates a hex color string.
    /// </summary>
    public static bool IsValidHexColor(string hex)
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
}
