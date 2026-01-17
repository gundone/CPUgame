using System.Text.Json.Serialization;

namespace CPUgame.Core.Services;

/// <summary>
/// User preferences containing last session information and UI settings
/// </summary>
public class UserPreferences
{
    [JsonPropertyName("lastProfile")]
    public string? LastProfile { get; set; }

    [JsonPropertyName("lastLevelIndex")]
    public int LastLevelIndex { get; set; } = -1;
}