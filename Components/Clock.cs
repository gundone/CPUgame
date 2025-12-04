using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

/// <summary>
/// A clock signal generator that oscillates between High and Low
/// </summary>
public class Clock : Component
{
    public Pin Output { get; }

    public double Frequency { get; set; } = 1.0; // Hz
    public bool IsRunning { get; set; } = true;

    private double _elapsed;
    private bool _state;

    public Clock(int x, int y) : base(x, y)
    {
        Name = "Clock";
        Width = 50;
        Height = 40;

        Output = AddOutput("Out", 50, 20);
    }

    public void Update(double deltaTime)
    {
        if (!IsRunning) return;

        _elapsed += deltaTime;
        var period = 1.0 / Frequency;

        if (_elapsed >= period / 2)
        {
            _elapsed = 0;
            _state = !_state;
        }
    }

    public override void Evaluate()
    {
        Output.Value = _state ? Signal.High : Signal.Low;
    }
}
