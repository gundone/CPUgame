using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Core;

/// <summary>
/// Base class for all circuit components
/// </summary>
public abstract class Component
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; protected set; } = 40;
    public int Height { get; protected set; } = 60;
    public string Name { get; protected set; } = "Component";
    public string Title { get; set; } = "";

    public List<Pin> Inputs { get; } = new();
    public List<Pin> Outputs { get; } = new();

    public bool IsSelected { get; set; }

    protected Component(int x, int y)
    {
        X = x;
        Y = y;
    }

    protected Pin AddInput(string name, int localX, int localY)
    {
        var pin = new Pin(name, PinType.Input, this) { LocalX = localX, LocalY = localY };
        Inputs.Add(pin);
        return pin;
    }

    protected Pin AddOutput(string name, int localX, int localY)
    {
        var pin = new Pin(name, PinType.Output, this) { LocalX = localX, LocalY = localY };
        Outputs.Add(pin);
        return pin;
    }

    /// <summary>
    /// Evaluate the component and update output values based on inputs
    /// </summary>
    public abstract void Evaluate();

    /// <summary>
    /// Read input values from connected pins
    /// </summary>
    public void ReadInputs()
    {
        foreach (var input in Inputs)
        {
            if (input.ConnectedTo != null)
            {
                input.Value = input.ConnectedTo.Value;
            }
            else
            {
                input.Value = Signal.Undefined;
            }
        }
    }

    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px < X + Width && py >= Y && py < Y + Height;
    }

    public Pin? GetPinAt(int px, int py, int tolerance = 10)
    {
        foreach (var pin in Inputs.Concat(Outputs))
        {
            var dx = px - pin.WorldX;
            var dy = py - pin.WorldY;
            if (dx * dx + dy * dy <= tolerance * tolerance)
                return pin;
        }
        return null;
    }

    public int GridSize { get; set; } = 20;
    public double MoveInterval { get; set; } = 0.1; // seconds between moves (2 cells per second)

    private double _moveTimer;

    public virtual void ApplyCommand(KeyboardState current, KeyboardState previous, double deltaTime)
    {
        bool wantsMove = current.IsKeyDown(Keys.Left) || current.IsKeyDown(Keys.Right) ||
                         current.IsKeyDown(Keys.Up) || current.IsKeyDown(Keys.Down);

        if (wantsMove)
        {
            _moveTimer += deltaTime;

            // Move immediately on first press, then wait for interval
            bool isFirstPress = !previous.IsKeyDown(Keys.Left) && !previous.IsKeyDown(Keys.Right) &&
                               !previous.IsKeyDown(Keys.Up) && !previous.IsKeyDown(Keys.Down);

            if (isFirstPress || _moveTimer >= MoveInterval)
            {
                if (current.IsKeyDown(Keys.Left))
                    X -= GridSize;
                if (current.IsKeyDown(Keys.Right))
                    X += GridSize;
                if (current.IsKeyDown(Keys.Up))
                    Y -= GridSize;
                if (current.IsKeyDown(Keys.Down))
                    Y += GridSize;

                _moveTimer = 0;
            }
        }
        else
        {
            _moveTimer = 0;
        }
    }

    protected static bool IsKeyJustPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }
}
