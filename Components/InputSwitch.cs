using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

/// <summary>
/// A switch that can be toggled to provide High or Low signal
/// </summary>
public class InputSwitch : Component
{
    public Pin Output { get; }
    public bool IsOn { get; set; }

    public InputSwitch(int x, int y, bool initialState = false) : base(x, y)
    {
        Name = "Switch";
        Width = 40;
        Height = 40;
        IsOn = initialState;

        Output = AddOutput("Out", 40, 20);
    }

    public void Toggle()
    {
        IsOn = !IsOn;
    }

    public override void Evaluate()
    {
        Output.Value = IsOn ? Signal.High : Signal.Low;
    }

    public override void ApplyCommand(KeyboardState current, KeyboardState previous, double deltaTime)
    {
        base.ApplyCommand(current, previous, deltaTime);

        if (IsKeyJustPressed(Keys.Space, current, previous))
        {
            Toggle();
        }
    }
}
