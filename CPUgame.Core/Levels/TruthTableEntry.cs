using System.Text.Json.Serialization;

namespace CPUgame.Core.Levels;

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