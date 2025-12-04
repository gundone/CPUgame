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

    private readonly List<BusInput> _busInputs = new();
    private readonly List<BusOutput> _busOutputs = new();

    // Maps external pin index to (BusInput index, bit index within that BusInput)
    private readonly List<(int busIndex, int bitIndex)> _inputPinMap = new();
    // Maps external pin index to (BusOutput index, bit index within that BusOutput)
    private readonly List<(int busIndex, int bitIndex)> _outputPinMap = new();

    public CustomComponent(int x, int y, string name, Circuit internalCircuit) : base(x, y)
    {
        ComponentName = name;
        Name = name;
        InternalCircuit = internalCircuit;

        // Find BusInputs and BusOutputs to create external pins
        foreach (var component in internalCircuit.Components)
        {
            if (component is BusInput busIn)
                _busInputs.Add(busIn);
            else if (component is BusOutput busOut)
                _busOutputs.Add(busOut);
        }

        // Count total input pins (from all BusInputs)
        int totalInputPins = _busInputs.Sum(b => b.BitCount);
        // Count total output pins (from all BusOutputs)
        int totalOutputPins = _busOutputs.Sum(b => b.BitCount);

        // Calculate dimensions based on pin count
        int maxPins = System.Math.Max(totalInputPins, totalOutputPins);
        Height = System.Math.Max(40, maxPins * 20 + 20);
        Width = 80;

        // Create external input pins (one per bit across all BusInputs)
        int inputPinIndex = 0;
        for (int busIdx = 0; busIdx < _busInputs.Count; busIdx++)
        {
            var busInput = _busInputs[busIdx];
            for (int bitIdx = 0; bitIdx < busInput.BitCount; bitIdx++)
            {
                int pinY = 20 + inputPinIndex * 20;
                AddInput($"In{inputPinIndex}", 0, pinY);
                _inputPinMap.Add((busIdx, bitIdx));
                inputPinIndex++;
            }
        }

        // Create external output pins (one per bit across all BusOutputs)
        int outputPinIndex = 0;
        for (int busIdx = 0; busIdx < _busOutputs.Count; busIdx++)
        {
            var busOutput = _busOutputs[busIdx];
            for (int bitIdx = 0; bitIdx < busOutput.BitCount; bitIdx++)
            {
                int pinY = 20 + outputPinIndex * 20;
                AddOutput($"Out{outputPinIndex}", Width, pinY);
                _outputPinMap.Add((busIdx, bitIdx));
                outputPinIndex++;
            }
        }
    }

    public override void Evaluate()
    {
        // Transfer external inputs to internal BusInputs
        for (int i = 0; i < Inputs.Count && i < _inputPinMap.Count; i++)
        {
            var (busIndex, bitIndex) = _inputPinMap[i];
            if (busIndex < _busInputs.Count)
            {
                _busInputs[busIndex].SetBit(bitIndex, Inputs[i].Value == Signal.High);
            }
        }

        // Simulate internal circuit
        InternalCircuit.Simulate();

        // Transfer internal BusOutput values to external outputs
        for (int i = 0; i < Outputs.Count && i < _outputPinMap.Count; i++)
        {
            var (busIndex, bitIndex) = _outputPinMap[i];
            if (busIndex < _busOutputs.Count)
            {
                bool bitValue = _busOutputs[busIndex].Inputs[_busOutputs[busIndex].BitCount - 1 - bitIndex].Value == Signal.High;
                Outputs[i].Value = bitValue ? Signal.High : Signal.Low;
            }
        }
    }
}
