namespace CPUgame.Core.TruthTable;

/// <summary>
/// Stores information about a bus for header display
/// </summary>
public class BusHeaderInfo
{
    public string Title { get; }
    public int BitCount { get; }

    public BusHeaderInfo(string title, int bitCount)
    {
        Title = title;
        BitCount = bitCount;
    }
}