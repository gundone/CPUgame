using System.Collections.Generic;
using System.Linq;
using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

/// <summary>
/// A custom component built from other components (composite circuit)
/// </summary>
public class CustomComponent : Component
{
    public Circuit InternalCircuit { get; }
    public string ComponentName { get; }

    private readonly List<InputSwitch> _inputSwitches = new();
    private readonly List<OutputLed> _outputLeds = new();

    public CustomComponent(int x, int y, string name, Circuit internalCircuit) : base(x, y)
    {
        ComponentName = name;
        Name = name;
        InternalCircuit = internalCircuit;

        // Find input switches and output LEDs to create external pins
        foreach (var component in internalCircuit.Components)
        {
            if (component is InputSwitch sw)
                _inputSwitches.Add(sw);
            else if (component is OutputLed led)
                _outputLeds.Add(led);
        }

        // Calculate dimensions based on pin count
        int maxPins = System.Math.Max(_inputSwitches.Count, _outputLeds.Count);
        Height = System.Math.Max(40, maxPins * 20 + 20);
        Width = 80;

        // Create external pins
        for (int i = 0; i < _inputSwitches.Count; i++)
        {
            int pinY = 10 + i * 20;
            AddInput($"In{i}", 0, pinY);
        }

        for (int i = 0; i < _outputLeds.Count; i++)
        {
            int pinY = 10 + i * 20;
            AddOutput($"Out{i}", Width, pinY);
        }
    }

    public override void Evaluate()
    {
        // Transfer external inputs to internal switches
        for (int i = 0; i < Inputs.Count && i < _inputSwitches.Count; i++)
        {
            _inputSwitches[i].IsOn = Inputs[i].Value == Signal.High;
        }

        // Simulate internal circuit
        InternalCircuit.Simulate();

        // Transfer internal LED states to external outputs
        for (int i = 0; i < Outputs.Count && i < _outputLeds.Count; i++)
        {
            Outputs[i].Value = _outputLeds[i].IsLit ? Signal.High : Signal.Low;
        }
    }
}
