namespace CPUgame.Core.Serialization;

public class ConnectionData
{
    public int FromComponentId { get; set; }
    public int FromPinIndex { get; set; }
    public int ToComponentId { get; set; }
    public int ToPinIndex { get; set; }
    public List<int[]>? ManualWirePath { get; set; }
}