using CPUgame.Core.Circuit;
using CPUgame.Core.Input;

namespace CPUgame.Core.Components;

/// <summary>
/// Multi-bit input component with configurable number of output pins.
/// User can set binary value which is output on the pins.
/// </summary>
public class BusInput : Component
{
    private static readonly int[] AllowedBitCounts = { 1, 2, 4, 8, 16 };

    public int BitCount { get; private set; }
    public int Value { get; set; }
    public bool ShowPinValues { get; set; } = false;
    private readonly int _gridSize;

    public BusInput(int x, int y, int bitCount = 4, int gridSize = 20) : base(x, y)
    {
        _gridSize = gridSize;
        BitCount = Math.Max(1, Math.Min(bitCount, 16)); // Limit to 1-16 bits
        SetupPins();
        Value = 0;
        ShowPinTitles = true;
    }

    private void SetupPins()
    {
        Name = $"In{BitCount}";
        Width = 3 * _gridSize; // 3 cells wide
        Height = (BitCount + 1) * _gridSize; // +1 cell for padding

        // Clear existing pins
        Outputs.Clear();

        // Create output pins (pin 0 at top), aligned to grid
        for (int i = 0; i < BitCount; i++)
        {
            int pinY = _gridSize + i * _gridSize;
            AddOutput($"O{i}", Width, pinY);
        }
    }

    /// <summary>
    /// Resize the number of bits. Call with connectedInputPins to preserve wire connections.
    /// </summary>
    public void ResizeBits(bool increase, List<Pin>? connectedInputPins = null)
    {
        int currentIndex = Array.IndexOf(AllowedBitCounts, BitCount);
        if (currentIndex < 0)
        {
            currentIndex = 2; // Default to 4
        }

        int newBitCount;
        if (increase && currentIndex < AllowedBitCounts.Length - 1)
        {
            newBitCount = AllowedBitCounts[currentIndex + 1];
        }
        else if (!increase && currentIndex > 0)
        {
            newBitCount = AllowedBitCounts[currentIndex - 1];
        }
        else
        {
            return; // No change
        }

        // Store connections: map from pin index to list of connected input pins
        // Pin index 0 is at top
        var connectionsByPinIndex = new Dictionary<int, List<Pin>>();

        if (connectedInputPins != null)
        {
            foreach (var inputPin in connectedInputPins)
            {
                if (inputPin.ConnectedTo != null && inputPin.ConnectedTo.Owner == this)
                {
                    // Find which output pin this is connected to
                    int outputIndex = Outputs.IndexOf(inputPin.ConnectedTo);
                    if (outputIndex >= 0)
                    {
                        if (!connectionsByPinIndex.ContainsKey(outputIndex))
                        {
                            connectionsByPinIndex[outputIndex] = new List<Pin>();
                        }
                        connectionsByPinIndex[outputIndex].Add(inputPin);

                        // Disconnect
                        inputPin.Disconnect();
                    }
                }
            }
        }

        // Update bit count and recreate pins
        BitCount = newBitCount;
        Value &= (1 << BitCount) - 1; // Mask value to new bit count
        SetupPins();

        // Restore connections for pins that still exist
        foreach (var kvp in connectionsByPinIndex)
        {
            int pinIndex = kvp.Key;
            if (pinIndex < Outputs.Count)
            {
                foreach (var inputPin in kvp.Value)
                {
                    inputPin.ConnectedTo = Outputs[pinIndex];
                }
            }
            // If pinIndex >= Outputs.Count, the pin was removed and connections are lost
        }
    }

    public void SetBit(int bitIndex, bool value)
    {
        if (bitIndex < 0 || bitIndex >= BitCount)
        {
            return;
        }

        if (value)
        {
            Value |= (1 << bitIndex);
        }
        else
        {
            Value &= ~(1 << bitIndex);
        }
    }

    public void ToggleBit(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= BitCount)
        {
            return;
        }
        SetBit(bitIndex, !GetBit(bitIndex));
    }

    public bool GetBit(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= BitCount)
        {
            return false;
        }
        return (Value & (1 << bitIndex)) != 0;
    }

    public string GetBinaryString()
    {
        var chars = new char[BitCount];
        for (int i = 0; i < BitCount; i++)
        {
            chars[BitCount - 1 - i] = GetBit(i) ? '1' : '0';
        }
        return new string(chars);
    }

    public void SetFromBinaryString(string binary)
    {
        Value = 0;
        for (int i = 0; i < Math.Min(binary.Length, BitCount); i++)
        {
            int bitIndex = BitCount - 1 - i;
            if (binary[i] == '1')
            {
                Value |= (1 << bitIndex);
            }
        }
    }

    public override void Evaluate()
    {
        // Set output pin values based on current value
        // Pin index equals bit index (pin 0 at top = bit 0)
        for (int i = 0; i < Outputs.Count; i++)
        {
            Outputs[i].Value = GetBit(i) ? Signal.High : Signal.Low;
        }
    }

    /// <summary>
    /// Handle commands from InputState. Call with allInputPins to preserve wire connections on resize.
    /// </summary>
    public void HandleCommands(InputState input, List<Pin>? allInputPins = null)
    {
        // Resize with space / shift+space or shift+scroll (not ctrl+scroll, that's zoom)
        if (input.ResizeIncreaseCommand || (input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta > 0))
        {
            ResizeBits(true, allInputPins);
        }
        else if (input.ResizeDecreaseCommand || (input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta < 0))
        {
            ResizeBits(false, allInputPins);
        }
        else
        {
            HandleValueCommands(input);
        }
    }

    /// <summary>
    /// Handle value-only commands (no resizing). Used for level components.
    /// </summary>
    public void HandleValueCommands(InputState input)
    {
        // Increment/decrement value with +/- or scroll (without shift/ctrl, ctrl+scroll is zoom)
        if (input.IncreaseCommand || (!input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta > 0))
        {
            Value = (Value + 1) % (1 << BitCount);
        }
        else if (input.DecreaseCommand || (!input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta < 0))
        {
            Value = (Value - 1 + (1 << BitCount)) % (1 << BitCount);
        }

        // Toggle bits with number keys 0-9
        if (input.NumberInput.HasValue)
        {
            int bitIndex = input.NumberInput.Value - '0';
            if (bitIndex >= 0 && bitIndex < BitCount)
            {
                ToggleBit(bitIndex);
            }
        }
    }
}
