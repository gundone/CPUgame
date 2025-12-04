using System;
using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

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
    private int _gridSize;

    public BusInput(int x, int y, int bitCount = 4, int gridSize = 20) : base(x, y)
    {
        _gridSize = gridSize;
        BitCount = Math.Max(1, Math.Min(bitCount, 16)); // Limit to 1-16 bits
        SetupPins();
        Value = 0;
    }

    private void SetupPins()
    {
        Name = $"In{BitCount}";
        Width = 3 * _gridSize; // 3 cells wide
        Height = (BitCount + 1) * _gridSize; // +1 cell for padding

        // Clear existing pins
        Outputs.Clear();

        // Create output pins (MSB at top), aligned to grid
        for (int i = 0; i < BitCount; i++)
        {
            int pinY = _gridSize + i * _gridSize;
            AddOutput($"O{BitCount - 1 - i}", Width, pinY);
        }
    }

    public void ResizeBits(bool increase)
    {
        int currentIndex = Array.IndexOf(AllowedBitCounts, BitCount);
        if (currentIndex < 0) currentIndex = 2; // Default to 4

        if (increase && currentIndex < AllowedBitCounts.Length - 1)
        {
            BitCount = AllowedBitCounts[currentIndex + 1];
            Value &= (1 << BitCount) - 1; // Mask value to new bit count
            SetupPins();
        }
        else if (!increase && currentIndex > 0)
        {
            BitCount = AllowedBitCounts[currentIndex - 1];
            Value &= (1 << BitCount) - 1; // Mask value to new bit count
            SetupPins();
        }
    }

    public void SetBit(int bitIndex, bool value)
    {
        if (bitIndex < 0 || bitIndex >= BitCount) return;

        if (value)
            Value |= (1 << bitIndex);
        else
            Value &= ~(1 << bitIndex);
    }

    public bool GetBit(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= BitCount) return false;
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
                Value |= (1 << bitIndex);
        }
    }

    public override void Evaluate()
    {
        // Set output pin values based on current value
        for (int i = 0; i < Outputs.Count; i++)
        {
            int bitIndex = BitCount - 1 - i; // MSB at top (index 0)
            Outputs[i].Value = GetBit(bitIndex) ? Signal.High : Signal.Low;
        }
    }

    public override void ApplyCommand(KeyboardState current, KeyboardState previous, double deltaTime)
    {
        base.ApplyCommand(current, previous, deltaTime);

        bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);

        // Toggle pin values display with V key
        if (IsKeyJustPressed(Keys.V, current, previous))
        {
            ShowPinValues = !ShowPinValues;
            return;
        }

        // Shift+/- to resize pin count
        if (shift)
        {
            if (IsKeyJustPressed(Keys.OemPlus, current, previous) || IsKeyJustPressed(Keys.Add, current, previous))
            {
                ResizeBits(true);
                return;
            }
            if (IsKeyJustPressed(Keys.OemMinus, current, previous) || IsKeyJustPressed(Keys.Subtract, current, previous))
            {
                ResizeBits(false);
                return;
            }
        }

        // Toggle bits with number keys 0-9
        for (int i = 0; i < Math.Min(BitCount, 10); i++)
        {
            Keys key = Keys.D0 + i;
            if (IsKeyJustPressed(key, current, previous))
            {
                // Toggle bit (0 = LSB when BitCount <= 10)
                int bitIndex = i;
                if (bitIndex < BitCount)
                {
                    SetBit(bitIndex, !GetBit(bitIndex));
                }
            }
        }

        // Increment/decrement with +/-
        if (IsKeyJustPressed(Keys.OemPlus, current, previous) || IsKeyJustPressed(Keys.Add, current, previous))
        {
            Value = (Value + 1) % (1 << BitCount);
        }
        if (IsKeyJustPressed(Keys.OemMinus, current, previous) || IsKeyJustPressed(Keys.Subtract, current, previous))
        {
            Value = (Value - 1 + (1 << BitCount)) % (1 << BitCount);
        }
    }
}
