using CPUgame.Core;

namespace CPUgame.Components;

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
}
