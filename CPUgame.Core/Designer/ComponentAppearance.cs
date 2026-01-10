using System.Text.Json.Serialization;

namespace CPUgame.Core.Designer;

/// <summary>
/// Defines the complete visual appearance customization for a component type.
/// </summary>
public class ComponentAppearance
{
    /// <summary>
    /// The component type this appearance applies to (e.g., "NAND", "Switch", "Custom:MyGate")
    /// </summary>
    [JsonPropertyName("componentType")]
    public string ComponentType { get; set; } = "";

    /// <summary>
    /// The width of the component in pixels
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// The height of the component in pixels
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Where the title is positioned relative to the component
    /// </summary>
    [JsonPropertyName("titlePosition")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TitlePosition TitlePosition { get; set; } = TitlePosition.Center;

    /// <summary>
    /// Custom X offset for the title when TitlePosition is Custom
    /// </summary>
    [JsonPropertyName("titleOffsetX")]
    public int TitleOffsetX { get; set; }

    /// <summary>
    /// Custom Y offset for the title when TitlePosition is Custom
    /// </summary>
    [JsonPropertyName("titleOffsetY")]
    public int TitleOffsetY { get; set; }

    /// <summary>
    /// Font size scale for the title (1.0 = default size)
    /// </summary>
    [JsonPropertyName("titleFontScale")]
    public float TitleFontScale { get; set; } = 1.0f;

    /// <summary>
    /// Custom title text (if empty, uses component type name)
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    /// <summary>
    /// Fill color for the component body (RGBA hex string, e.g., "#FF5050FF")
    /// </summary>
    [JsonPropertyName("fillColor")]
    public string? FillColor { get; set; }

    /// <summary>
    /// Appearance definitions for input pins
    /// </summary>
    [JsonPropertyName("inputPins")]
    public List<PinAppearance> InputPins { get; set; } = new();

    /// <summary>
    /// Appearance definitions for output pins
    /// </summary>
    [JsonPropertyName("outputPins")]
    public List<PinAppearance> OutputPins { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this appearance
    /// </summary>
    public ComponentAppearance Clone()
    {
        return new ComponentAppearance
        {
            ComponentType = ComponentType,
            Width = Width,
            Height = Height,
            TitlePosition = TitlePosition,
            TitleOffsetX = TitleOffsetX,
            TitleOffsetY = TitleOffsetY,
            TitleFontScale = TitleFontScale,
            Title = Title,
            FillColor = FillColor,
            InputPins = InputPins.Select(p => new PinAppearance(p.Name, p.LocalX, p.LocalY)).ToList(),
            OutputPins = OutputPins.Select(p => new PinAppearance(p.Name, p.LocalX, p.LocalY)).ToList()
        };
    }
}

/// <summary>
/// Container for all component appearances, used for JSON serialization
/// </summary>
public class ComponentAppearanceData
{
    [JsonPropertyName("appearances")]
    public Dictionary<string, ComponentAppearance> Appearances { get; set; } = new();
}
