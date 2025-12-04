using System;
using System.Collections.Generic;
using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

/// <summary>
/// Multi-bit output component with configurable number of input pins.
/// Displays the binary value received on input pins.
/// </summary>
public class BusOutput : Component
{
    private static readonly int[] AllowedBitCounts = { 1, 2, 4, 8, 16 };

    public int BitCount { get; private set; }
    public bool ShowPinValues { get; set; } = false;
    private int _gridSize;

    public BusOutput(int x, int y, int bitCount = 4, int gridSize = 20) : base(x, y)
    {
        _gridSize = gridSize;
        BitCount = Math.Max(1, Math.Min(bitCount, 16)); // Limit to 1-16 bits
        SetupPins();
    }

    private void SetupPins()
    {
        Name = $"Out{BitCount}";
        Width = 4 * _gridSize; // 4 cells wide
        Height = (BitCount + 1) * _gridSize; // +1 cell for padding

        // Clear existing pins
        Inputs.Clear();

        // Create input pins (MSB at top), aligned to grid
        for (int i = 0; i < BitCount; i++)
        {
            int pinY = _gridSize + i * _gridSize;
            AddInput($"I{BitCount - 1 - i}", 0, pinY);
        }
    }

    /// <summary>
    /// Resize the number of bits. Preserves wire connections for pins that still exist.
    /// </summary>
    public void ResizeBits(bool increase)
    {
        int currentIndex = Array.IndexOf(AllowedBitCounts, BitCount);
        if (currentIndex < 0) currentIndex = 2; // Default to 4

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

        int oldBitCount = BitCount;

        // Store connections: map from bit index to connected output pin
        // Bit index is from LSB (0) to MSB (BitCount-1)
        var connectionsByBit = new Dictionary<int, Pin>();

        for (int i = 0; i < Inputs.Count; i++)
        {
            if (Inputs[i].ConnectedTo != null)
            {
                // Convert input index to bit index (MSB at top, so index 0 = MSB)
                int bitIndex = oldBitCount - 1 - i;
                connectionsByBit[bitIndex] = Inputs[i].ConnectedTo;
            }
        }

        // Update bit count and recreate pins
        BitCount = newBitCount;
        SetupPins();

        // Restore connections for pins that still exist
        foreach (var kvp in connectionsByBit)
        {
            int bitIndex = kvp.Key;
            if (bitIndex < BitCount)
            {
                // Convert bit index back to input index
                int inputIndex = BitCount - 1 - bitIndex;
                if (inputIndex >= 0 && inputIndex < Inputs.Count)
                {
                    Inputs[inputIndex].ConnectedTo = kvp.Value;
                }
            }
            // If bitIndex >= BitCount, the pin was removed and connections are lost
        }
    }

    public int GetValue()
    {
        int value = 0;
        for (int i = 0; i < Inputs.Count; i++)
        {
            int bitIndex = BitCount - 1 - i; // MSB at top (index 0)
            if (Inputs[i].Value == Signal.High)
            {
                value |= (1 << bitIndex);
            }
        }
        return value;
    }

    public string GetBinaryString()
    {
        var chars = new char[BitCount];
        for (int i = 0; i < BitCount; i++)
        {
            int bitIndex = BitCount - 1 - i;
            bool isHigh = false;
            if (i < Inputs.Count)
            {
                isHigh = Inputs[i].Value == Signal.High;
            }
            chars[i] = isHigh ? '1' : '0';
        }
        return new string(chars);
    }


    public override void Evaluate()
    {
        // Output just displays values, no computation needed
    }

    public override void ApplyCommand(KeyboardState current, KeyboardState previous, double deltaTime)
    {
        base.ApplyCommand(current, previous, deltaTime);

        bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);

        // Shift+/- to resize pin count
        if (shift)
        {
            if (IsKeyJustPressed(Keys.OemPlus, current, previous) || IsKeyJustPressed(Keys.Add, current, previous))
            {
                ResizeBits(true);
            }
            else if (IsKeyJustPressed(Keys.OemMinus, current, previous) || IsKeyJustPressed(Keys.Subtract, current, previous))
            {
                ResizeBits(false);
            }
        }
    }
}
