namespace CPUgame.Core.TruthTable;

/// <summary>
/// Stores information about a bus for header display
/// </summary>
public class BusHeaderInfo
{
    public string Title { get; }
    public int BitCount { get; }
    public List<string> PinTitles { get; }

    public BusHeaderInfo(string title, int bitCount, List<string>? pinTitles = null)
    {
        Title = title;
        BitCount = bitCount;
        PinTitles = pinTitles ?? new List<string>();
    }

    /// <summary>
    /// Gets the title for a specific pin, falling back to pin number if not set
    /// </summary>
    public string GetPinTitle(int pinIndex)
    {
        if (pinIndex >= 0 && pinIndex < PinTitles.Count && !string.IsNullOrEmpty(PinTitles[pinIndex]))
        {
            return PinTitles[pinIndex];
        }
        return pinIndex.ToString();
    }
}