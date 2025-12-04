using System.Collections.Generic;
using System.Linq;

namespace CPUgame.Core;

/// <summary>
/// Represents a circuit containing multiple components
/// </summary>
public class Circuit
{
    public List<Component> Components { get; } = new();
    public string Name { get; set; } = "Untitled Circuit";

    private const int MaxIterations = 1000; // Prevent infinite loops

    public void AddComponent(Component component)
    {
        Components.Add(component);
    }

    public void RemoveComponent(Component component)
    {
        // Disconnect all pins first
        foreach (var input in component.Inputs)
        {
            input.Disconnect();
        }

        // Find and disconnect any pins connected to this component's outputs
        foreach (var output in component.Outputs)
        {
            foreach (var other in Components)
            {
                foreach (var input in other.Inputs)
                {
                    if (input.ConnectedTo == output)
                        input.Disconnect();
                }
            }
        }

        Components.Remove(component);
    }

    /// <summary>
    /// Simulate the circuit until stable or max iterations reached
    /// </summary>
    public void Simulate()
    {
        for (int i = 0; i < MaxIterations; i++)
        {
            bool changed = false;

            foreach (var component in Components)
            {
                component.ReadInputs();

                // Store old output values
                var oldValues = component.Outputs.Select(o => o.Value).ToList();

                component.Evaluate();

                // Check if any output changed
                for (int j = 0; j < component.Outputs.Count; j++)
                {
                    if (component.Outputs[j].Value != oldValues[j])
                    {
                        changed = true;
                    }
                }
            }

            // Circuit is stable
            if (!changed) break;
        }
    }

    public Component? GetComponentAt(int x, int y)
    {
        // Return topmost (last added) component at position
        for (int i = Components.Count - 1; i >= 0; i--)
        {
            if (Components[i].ContainsPoint(x, y))
                return Components[i];
        }
        return null;
    }

    public Pin? GetPinAt(int x, int y, int tolerance = 10)
    {
        foreach (var component in Components)
        {
            var pin = component.GetPinAt(x, y, tolerance);
            if (pin != null) return pin;
        }
        return null;
    }

    public void ClearSelection()
    {
        foreach (var component in Components)
        {
            component.IsSelected = false;
        }
    }

    /// <summary>
    /// Find a wire (connection) near the given point.
    /// Returns the input pin of the connection if found.
    /// </summary>
    public Pin? GetWireAt(int x, int y, int tolerance = 8)
    {
        foreach (var component in Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ConnectedTo != null)
                {
                    if (IsPointNearWire(x, y, input.ConnectedTo, input, tolerance))
                    {
                        return input;
                    }
                }
            }
        }
        return null;
    }

    private bool IsPointNearWire(int px, int py, Pin from, Pin to, int tolerance)
    {
        // Wire is drawn as 3 segments: horizontal, vertical, horizontal
        float startX = from.WorldX;
        float startY = from.WorldY;
        float endX = to.WorldX;
        float endY = to.WorldY;
        float midX = (startX + endX) / 2;

        // Segment 1: from start to mid1 (horizontal)
        if (IsPointNearSegment(px, py, startX, startY, midX, startY, tolerance))
            return true;

        // Segment 2: from mid1 to mid2 (vertical)
        if (IsPointNearSegment(px, py, midX, startY, midX, endY, tolerance))
            return true;

        // Segment 3: from mid2 to end (horizontal)
        if (IsPointNearSegment(px, py, midX, endY, endX, endY, tolerance))
            return true;

        return false;
    }

    private bool IsPointNearSegment(int px, int py, float x1, float y1, float x2, float y2, int tolerance)
    {
        // Calculate distance from point to line segment
        float dx = x2 - x1;
        float dy = y2 - y1;
        float lengthSq = dx * dx + dy * dy;

        if (lengthSq == 0)
        {
            // Segment is a point
            float distSq = (px - x1) * (px - x1) + (py - y1) * (py - y1);
            return distSq <= tolerance * tolerance;
        }

        // Project point onto line
        float t = System.Math.Max(0, System.Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSq));
        float projX = x1 + t * dx;
        float projY = y1 + t * dy;

        float distSq2 = (px - projX) * (px - projX) + (py - projY) * (py - projY);
        return distSq2 <= tolerance * tolerance;
    }
}
