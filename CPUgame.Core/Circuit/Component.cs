using CPUgame.Core.Designer;

namespace CPUgame.Core.Circuit;

/// <summary>
/// Base class for all circuit components
/// </summary>
public abstract class Component
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 40;
    public int Height { get; set; } = 60;
    public string Name { get; protected set; } = "Component";
    public string Title { get; set; } = "";

    public List<Pin> Inputs { get; } = new();
    public List<Pin> Outputs { get; } = new();

    public bool IsSelected { get; set; }

    /// <summary>
    /// When true, pin titles are displayed near the wires connected to pins.
    /// </summary>
    public bool ShowPinTitles { get; set; }

    /// <summary>
    /// Position of the component title relative to the component body.
    /// </summary>
    public TitlePosition TitlePosition { get; set; } = TitlePosition.Center;

    /// <summary>
    /// Custom X offset for the title when TitlePosition is Custom.
    /// </summary>
    public int TitleOffsetX { get; set; }

    /// <summary>
    /// Custom Y offset for the title when TitlePosition is Custom.
    /// </summary>
    public int TitleOffsetY { get; set; }

    protected Component(int x, int y)
    {
        X = x;
        Y = y;
    }

    protected Pin AddInput(string name, int localX, int localY)
    {
        // Extract number from pin name (e.g., "I2" -> "2") or use count as fallback
        string numPart = new string(name.Where(char.IsDigit).ToArray());
        string defaultTitle = string.IsNullOrEmpty(numPart) ? $"in{Inputs.Count}" : $"in{numPart}";
        var pin = new Pin(name, PinType.Input, this, defaultTitle) { LocalX = localX, LocalY = localY };
        Inputs.Add(pin);
        return pin;
    }

    protected Pin AddOutput(string name, int localX, int localY)
    {
        // Extract number from pin name (e.g., "O2" -> "2") or use count as fallback
        string numPart = new string(name.Where(char.IsDigit).ToArray());
        string defaultTitle = string.IsNullOrEmpty(numPart) ? $"out{Outputs.Count}" : $"out{numPart}";
        var pin = new Pin(name, PinType.Output, this, defaultTitle) { LocalX = localX, LocalY = localY };
        Outputs.Add(pin);
        return pin;
    }

    /// <summary>
    /// Evaluate the component and update output values based on inputs
    /// </summary>
    public abstract void Evaluate();

    /// <summary>
    /// Read input values from connected pins.
    /// Unconnected inputs default to Low (0).
    /// </summary>
    public void ReadInputs()
    {
        foreach (var input in Inputs)
        {
            if (input.ConnectedTo != null)
            {
                input.Value = input.ConnectedTo.Value;
            }
            else
            {
                input.Value = Signal.Low;
            }
        }
    }

    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px < X + Width && py >= Y && py < Y + Height;
    }

    public Pin? GetPinAt(int px, int py, int tolerance = 10)
    {
        foreach (var pin in Inputs.Concat(Outputs))
        {
            var dx = px - pin.WorldX;
            var dy = py - pin.WorldY;
            if (dx * dx + dy * dy <= tolerance * tolerance)
            {
                return pin;
            }
        }
        return null;
    }

    public int GridSize { get; set; } = 20;
}
