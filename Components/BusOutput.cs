using System;
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

    public void ResizeBits(bool increase)
    {
        int currentIndex = Array.IndexOf(AllowedBitCounts, BitCount);
        if (currentIndex < 0) currentIndex = 2; // Default to 4

        if (increase && currentIndex < AllowedBitCounts.Length - 1)
        {
            BitCount = AllowedBitCounts[currentIndex + 1];
            SetupPins();
        }
        else if (!increase && currentIndex > 0)
        {
            BitCount = AllowedBitCounts[currentIndex - 1];
            SetupPins();
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

        // Toggle pin values display with V key
        if (IsKeyJustPressed(Keys.V, current, previous))
        {
            ShowPinValues = !ShowPinValues;
            return;
        }

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
