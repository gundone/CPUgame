using System.Text.Json.Serialization;

namespace CPUgame.Core.Designer;

/// <summary>
/// Container for all component appearances, used for JSON serialization
/// </summary>
public class ComponentAppearanceData
{
    [JsonPropertyName("appearances")]
    public Dictionary<string, ComponentAppearance> Appearances { get; set; } = new();
}