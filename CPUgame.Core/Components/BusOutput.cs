using CPUgame.Core.Circuit;
using CPUgame.Core.Input;

namespace CPUgame.Core.Components;

/// <summary>
/// Multi-bit output component with configurable number of input pins.
/// Displays the binary value received on input pins.
/// </summary>
public class BusOutput : Component
{
    private static readonly int[] AllowedBitCounts = { 1, 2, 4, 8, 16 };

    public int BitCount { get; private set; }
    public bool ShowPinValues { get; set; } = false;
    private readonly int _gridSize;

    public BusOutput(int x, int y, int bitCount = 4, int gridSize = 20) : base(x, y)
    {
        _gridSize = gridSize;
        BitCount = Math.Max(1, Math.Min(bitCount, 16)); // Limit to 1-16 bits
        SetupPins();
        ShowPinTitles = true;
    }

    private void SetupPins()
    {
        Name = $"Out{BitCount}";
        Width = 3 * _gridSize; // 3 cells wide
        Height = (BitCount + 1) * _gridSize; // +1 cell for padding

        // Clear existing pins
        Inputs.Clear();

        // Create input pins (pin 0 at top), aligned to grid
        for (int i = 0; i < BitCount; i++)
        {
            int pinY = _gridSize + i * _gridSize;
            AddInput($"I{i}", 0, pinY);
        }
    }

    /// <summary>
    /// Resize the number of bits. Preserves wire connections for pins that still exist.
    /// </summary>
    public void ResizeBits(bool increase)
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

        // Store connections: map from pin index to connected output pin
        // Pin index 0 is at top
        var connectionsByPinIndex = new Dictionary<int, Pin>();

        for (int i = 0; i < Inputs.Count; i++)
        {
            if (Inputs[i].ConnectedTo != null)
            {
                connectionsByPinIndex[i] = Inputs[i].ConnectedTo!;
            }
        }

        // Update bit count and recreate pins
        BitCount = newBitCount;
        SetupPins();

        // Restore connections for pins that still exist
        foreach (var kvp in connectionsByPinIndex)
        {
            int pinIndex = kvp.Key;
            if (pinIndex < Inputs.Count)
            {
                Inputs[pinIndex].ConnectedTo = kvp.Value;
            }
            // If pinIndex >= Inputs.Count, the pin was removed and connections are lost
        }
    }

    public int GetValue()
    {
        int value = 0;
        // Pin index equals bit index (pin 0 at top = bit 0)
        for (int i = 0; i < Inputs.Count; i++)
        {
            if (Inputs[i].Value == Signal.High)
            {
                value |= (1 << i);
            }
        }
        return value;
    }

    public string GetBinaryString()
    {
        // Binary string is MSB first (standard notation)
        // Pin 0 is bit 0 (LSB) at top, so reverse for display
        var chars = new char[BitCount];
        for (int i = 0; i < BitCount; i++)
        {
            bool isHigh = false;
            int pinIndex = BitCount - 1 - i;
            if (pinIndex < Inputs.Count)
            {
                isHigh = Inputs[pinIndex].Value == Signal.High;
            }
            chars[i] = isHigh ? '1' : '0';
        }
        return new string(chars);
    }


    public override void Evaluate()
    {
        // Output just displays values, no computation needed
    }

    /// <summary>
    /// Handle commands from InputState.
    /// </summary>
    public void HandleCommands(InputState input)
    {
        // Resize with space / shift+space or shift+scroll (not ctrl+scroll, that's zoom)
        if (input.ResizeIncreaseCommand || (input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta > 0))
        {
            ResizeBits(true);
        }
        else if (input.ResizeDecreaseCommand || (input.ShiftHeld && !input.CtrlHeld && input.ScrollDelta < 0))
        {
            ResizeBits(false);
        }
    }

    public override Component Clone(int gridSize)
    {
        var clone = new BusOutput(X, Y, BitCount, gridSize);
        CopyTitles(this, clone);
        return clone;
    }
}
