namespace CPUgame.Core;

/// <summary>
/// Represents an input or output pin on a component
/// </summary>
public class Pin
{
    public string Name { get; }
    public PinType Type { get; }
    public Component Owner { get; }
    public Signal Value { get; set; } = Signal.Undefined;

    /// <summary>
    /// User-editable title for the pin. Defaults to "in0", "in1", etc. for inputs
    /// and "out0", "out1", etc. for outputs.
    /// </summary>
    public string Title { get; set; }

    // Connection to another pin (for input pins, this is the source)
    public Pin? ConnectedTo { get; set; }

    // Visual position relative to component
    public int LocalX { get; set; }
    public int LocalY { get; set; }

    public int WorldX => Owner.X + LocalX;
    public int WorldY => Owner.Y + LocalY;

    public Pin(string name, PinType type, Component owner, string? title = null)
    {
        Name = name;
        Type = type;
        Owner = owner;
        Title = title ?? name;
    }

    public void Connect(Pin other)
    {
        if (Type == PinType.Input && other.Type == PinType.Output)
        {
            ConnectedTo = other;
        }
        else if (Type == PinType.Output && other.Type == PinType.Input)
        {
            other.ConnectedTo = this;
        }
    }

    public void Disconnect()
    {
        ConnectedTo = null;
    }
}

public enum PinType
{
    Input,
    Output
}
