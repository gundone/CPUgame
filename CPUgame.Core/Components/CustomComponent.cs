using CPUgame.Core.Circuit;
using CPUgame.Core.Designer;

namespace CPUgame.Core.Components;

/// <summary>
/// A custom component built from other components (composite circuit)
/// </summary>
public class CustomComponent : Component
{
    public Circuit.Circuit InternalCircuit { get; }
    public string ComponentName { get; }

    private readonly List<BusInput> _busInputs = new();
    private readonly List<BusOutput> _busOutputs = new();

    // Maps external pin index to (BusInput index, bit index within that BusInput)
    private readonly List<(int busIndex, int bitIndex)> _inputPinMap = new();
    // Maps external pin index to (BusOutput index, bit index within that BusOutput)
    private readonly List<(int busIndex, int bitIndex)> _outputPinMap = new();

    public CustomComponent(int x, int y, string name, Circuit.Circuit internalCircuit, ComponentAppearance? appearance = null) : base(x, y)
    {
        ComponentName = name;
        Name = name;
        InternalCircuit = internalCircuit;

        // Find BusInputs and BusOutputs to create external pins
        // Sort them by position (X then Y) to match the order used in appearance generation
        foreach (var component in internalCircuit.Components)
        {
            if (component is BusInput busIn)
            {
                _busInputs.Add(busIn);
            }
            else if (component is BusOutput busOut)
            {
                _busOutputs.Add(busOut);
            }
        }

        // Sort by X, then Y to match appearance generation order
        _busInputs.Sort((a, b) =>
        {
            int xCompare = a.X.CompareTo(b.X);
            return xCompare != 0 ? xCompare : a.Y.CompareTo(b.Y);
        });
        _busOutputs.Sort((a, b) =>
        {
            int xCompare = a.X.CompareTo(b.X);
            return xCompare != 0 ? xCompare : a.Y.CompareTo(b.Y);
        });

        // Count total input pins (from all BusInputs)
        int totalInputPins = _busInputs.Sum(b => b.BitCount);
        // Count total output pins (from all BusOutputs)
        int totalOutputPins = _busOutputs.Sum(b => b.BitCount);

        // Use appearance data if available, otherwise calculate defaults
        if (appearance != null)
        {
            Width = appearance.Width;
            Height = appearance.Height;
        }
        else
        {
            // Calculate dimensions based on pin count
            int maxPins = Math.Max(totalInputPins, totalOutputPins);
            Height = Math.Max(40, maxPins * 20 + 20);
            Width = 60; // Default 3 cells
        }

        // Create external input pins (one per bit across all BusInputs)
        int inputPinIndex = 0;
        for (int busIdx = 0; busIdx < _busInputs.Count; busIdx++)
        {
            var busInput = _busInputs[busIdx];
            for (int bitIdx = 0; bitIdx < busInput.BitCount; bitIdx++)
            {
                // Get pin name and position from appearance if available
                string pinName;
                int pinX, pinY;

                if (appearance != null && inputPinIndex < appearance.InputPins.Count)
                {
                    var pinAppearance = appearance.InputPins[inputPinIndex];
                    pinName = pinAppearance.Name;
                    pinX = pinAppearance.LocalX;
                    pinY = pinAppearance.LocalY;
                }
                else
                {
                    pinName = $"In{inputPinIndex}";
                    pinX = 0;
                    pinY = 20 + inputPinIndex * 20;
                }

                AddInput(pinName, pinX, pinY);
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
                // Get pin name and position from appearance if available
                string pinName;
                int pinX, pinY;

                if (appearance != null && outputPinIndex < appearance.OutputPins.Count)
                {
                    var pinAppearance = appearance.OutputPins[outputPinIndex];
                    pinName = pinAppearance.Name;
                    pinX = pinAppearance.LocalX;
                    pinY = pinAppearance.LocalY;
                }
                else
                {
                    pinName = $"Out{outputPinIndex}";
                    pinX = Width;
                    pinY = 20 + outputPinIndex * 20;
                }

                AddOutput(pinName, pinX, pinY);
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
        // Pin index equals bit index (pin 0 at top = bit 0)
        for (int i = 0; i < Outputs.Count && i < _outputPinMap.Count; i++)
        {
            var (busIndex, bitIndex) = _outputPinMap[i];
            if (busIndex < _busOutputs.Count && bitIndex < _busOutputs[busIndex].Inputs.Count)
            {
                bool bitValue = _busOutputs[busIndex].Inputs[bitIndex].Value == Signal.High;
                Outputs[i].Value = bitValue ? Signal.High : Signal.Low;
            }
        }
    }

    public override Component? Clone(int gridSize)
    {
        // CustomComponent cloning requires access to ComponentBuilder
        // This will be handled by ComponentBuilder.CloneComponent
        return null;
    }
}
