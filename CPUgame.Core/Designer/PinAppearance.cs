using System.Text.Json.Serialization;

namespace CPUgame.Core.Designer;

/// <summary>
/// Defines the visual position of a pin on a component.
/// </summary>
public class PinAppearance
{
    /// <summary>
    /// The name of the pin (e.g., "A", "B", "Out", "I0", "O1")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// The X position of the pin relative to the component's top-left corner
    /// </summary>
    [JsonPropertyName("localX")]
    public int LocalX { get; set; }

    /// <summary>
    /// The Y position of the pin relative to the component's top-left corner
    /// </summary>
    [JsonPropertyName("localY")]
    public int LocalY { get; set; }

    public PinAppearance()
    {
    }

    public PinAppearance(string name, int localX, int localY)
    {
        Name = name;
        LocalX = localX;
        LocalY = localY;
    }
}
