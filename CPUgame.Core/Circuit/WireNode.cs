namespace CPUgame.Core.Circuit;

/// <summary>
/// Represents an intermediate node on a manual wire path.
/// </summary>
public class WireNode
{
    public Pin Wire { get; }
    public int NodeIndex { get; }
    public bool IsSelected { get; set; }

    public WireNode(Pin wire, int nodeIndex)
    {
        Wire = wire;
        NodeIndex = nodeIndex;
    }

    public int X => Wire.ManualWirePath?[NodeIndex].X ?? 0;
    public int Y => Wire.ManualWirePath?[NodeIndex].Y ?? 0;

    public void SetPosition(int x, int y)
    {
        if (Wire.ManualWirePath != null && NodeIndex >= 0 && NodeIndex < Wire.ManualWirePath.Count)
        {
            Wire.ManualWirePath[NodeIndex] = new Primitives.Point2(x, y);
        }
    }
}
