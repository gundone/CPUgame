using System.Text.Json.Serialization;

namespace CPUgame.Core.Levels;

/// <summary>
/// Represents a game level with a target truth table
/// </summary>
public class GameLevel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("fullDescription")]
    public string FullDescription { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public int Tier { get; set; } = 1;

    [JsonPropertyName("inputCount")]
    public int InputCount { get; set; }

    [JsonPropertyName("outputCount")]
    public int OutputCount { get; set; }

    [JsonPropertyName("truthTable")]
    public List<TruthTableEntry> TruthTable { get; set; } = new();

    [JsonPropertyName("componentName")]
    public string ComponentName { get; set; } = string.Empty;
}

/// <summary>
/// A single row in the level's truth table
/// </summary>
public class TruthTableEntry
{
    [JsonPropertyName("inputs")]
    public List<bool> Inputs { get; set; } = new();

    [JsonPropertyName("outputs")]
    public List<bool> Outputs { get; set; } = new();
}

/// <summary>
/// Game modes
/// </summary>
public enum GameMode
{
    Sandbox,
    Levels
}
