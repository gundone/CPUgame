namespace CPUgame.Core.Serialization;

public class ComponentData
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string? Title { get; set; }
    public bool? State { get; set; }
    public double? Frequency { get; set; }
    public string? CustomName { get; set; }
    public int? BitCount { get; set; }
    public int? Value { get; set; }
    public List<string>? InputTitles { get; set; }
    public List<string>? OutputTitles { get; set; }
}