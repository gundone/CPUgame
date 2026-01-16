using CPUgame.Core.Circuit;

namespace CPUgame.Core.Components;

/// <summary>
/// An LED that shows the signal state visually
/// </summary>
public class OutputLed : Component
{
    public Pin Input { get; }

    public bool IsLit => Input.Value == Signal.High;

    public OutputLed(int x, int y) : base(x, y)
    {
        Name = "LED";
        Width = 40;
        Height = 40;

        Input = AddInput("In", 0, 20);
    }

    public override void Evaluate()
    {
        // LED just displays the input value, no processing needed
    }

    public override Component Clone(int gridSize)
    {
        var clone = new OutputLed(X, Y);
        CopyTitles(this, clone);
        return clone;
    }
}
