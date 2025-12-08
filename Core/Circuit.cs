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

    // Component counters per type for auto-naming
    private readonly Dictionary<string, int> _componentCounters = new();

    public void AddComponent(Component component)
    {
        // Generate title if not set
        if (string.IsNullOrEmpty(component.Title))
        {
            component.Title = GenerateTitle(component);
        }

        Components.Add(component);
    }

    private string GenerateTitle(Component component)
    {
        string typeName = component.Name;

        if (!_componentCounters.ContainsKey(typeName))
        {
            _componentCounters[typeName] = 0;
        }

        _componentCounters[typeName]++;
        return $"{typeName}-{_componentCounters[typeName]}";
    }

    /// <summary>
    /// Update counters when loading a circuit to avoid duplicate titles
    /// </summary>
    public void UpdateCountersFromComponents()
    {
        _componentCounters.Clear();

        foreach (var component in Components)
        {
            string typeName = component.Name;

            // Try to extract number from title
            if (!string.IsNullOrEmpty(component.Title) && component.Title.StartsWith(typeName + "-"))
            {
                string numberPart = component.Title.Substring(typeName.Length + 1);
                if (int.TryParse(numberPart, out int number))
                {
                    if (!_componentCounters.ContainsKey(typeName) || _componentCounters[typeName] < number)
                    {
                        _componentCounters[typeName] = number;
                    }
                }
            }
        }
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

    /// <summary>
    /// Get all manual wires connected to the given components (either as input or output).
    /// Returns a list of input pins that have ManualWirePath set.
    /// </summary>
    public List<Pin> GetManualWiresForComponents(IEnumerable<Component> components)
    {
        var result = new List<Pin>();
        var componentSet = new HashSet<Component>(components);

        foreach (var component in Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ManualWirePath != null && input.ManualWirePath.Count >= 2 && input.ConnectedTo != null)
                {
                    // Check if either the input's owner or the connected output's owner is in the set
                    if (componentSet.Contains(input.Owner) || componentSet.Contains(input.ConnectedTo.Owner))
                    {
                        result.Add(input);
                    }
                }
            }
        }

        return result;
    }

    private bool IsPointNearWire(int px, int py, Pin from, Pin to, int tolerance)
    {
        // Check if this wire has a manual path
        if (to.ManualWirePath != null && to.ManualWirePath.Count >= 2)
        {
            // Use manual path
            var manualPath = to.ManualWirePath;
            for (int i = 0; i < manualPath.Count - 1; i++)
            {
                if (IsPointNearSegment(px, py, manualPath[i].X, manualPath[i].Y, manualPath[i + 1].X, manualPath[i + 1].Y, tolerance))
                {
                    return true;
                }
            }
            return false;
        }

        // Calculate wire path (same logic as CircuitRenderer)
        var path = CalculateWirePath(from.WorldX, from.WorldY, to.WorldX, to.WorldY);

        // Check each segment
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (IsPointNearSegment(px, py, path[i].X, path[i].Y, path[i + 1].X, path[i + 1].Y, tolerance))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculate wire path (same logic as CircuitRenderer).
    /// Wire goes RIGHT from output (start) and LEFT into input (end).
    /// </summary>
    private List<(float X, float Y)> CalculateWirePath(float startX, float startY, float endX, float endY)
    {
        const int gridSize = 20;
        var path = new List<(float X, float Y)> { (startX, startY) };

        float margin = gridSize;

        if (startX + margin < endX - margin)
        {
            // Normal case: enough space between start and end
            float midX = CalculateWireMidX(startX, startY, endX, endY, startX + margin, endX - margin);
            path.Add((midX, startY));
            path.Add((midX, endY));
        }
        else
        {
            // Reverse/tight case: need to wrap around
            float rightX = CalculateWireMidX(startX, startY, endX, endY, startX + margin, float.MaxValue);
            float leftX = CalculateWireMidX(startX, startY, endX, endY, float.MinValue, endX - margin);

            // Find wrap Y (above or below)
            float minY = System.Math.Min(startY, endY);
            float maxY = System.Math.Max(startY, endY);
            float wrapY = CalculateWrapY(leftX, rightX, minY, maxY, gridSize);

            path.Add((rightX, startY));
            path.Add((rightX, wrapY));
            path.Add((leftX, wrapY));
            path.Add((leftX, endY));
        }

        path.Add((endX, endY));
        return path;
    }

    /// <summary>
    /// Calculate Y position for horizontal wrap segment.
    /// </summary>
    private float CalculateWrapY(float leftX, float rightX, float minY, float maxY, int gridSize)
    {
        float topMost = minY;
        float bottomMost = maxY;

        foreach (var comp in Components)
        {
            int rectLeft = comp.X - gridSize / 2;
            int rectRight = comp.X + comp.Width + gridSize / 2;

            if (rightX >= rectLeft && leftX <= rectRight)
            {
                if (comp.Y - gridSize / 2 < topMost)
                {
                    topMost = comp.Y - gridSize / 2;
                }
                if (comp.Y + comp.Height + gridSize / 2 > bottomMost)
                {
                    bottomMost = comp.Y + comp.Height + gridSize / 2;
                }
            }
        }

        float aboveY = topMost - gridSize;
        float belowY = bottomMost + gridSize;
        float centerY = (minY + maxY) / 2;

        return System.Math.Abs(centerY - aboveY) <= System.Math.Abs(centerY - belowY)
            ? SnapToGrid(aboveY, gridSize)
            : SnapToGrid(belowY, gridSize);
    }

    /// <summary>
    /// Calculate the X position for the vertical wire segment (same logic as CircuitRenderer).
    /// </summary>
    private float CalculateWireMidX(float startX, float startY, float endX, float endY, float minAllowedX, float maxAllowedX)
    {
        const int gridSize = 20;

        float minY = System.Math.Min(startY, endY);
        float maxY = System.Math.Max(startY, endY);

        // Collect components that could block a vertical line
        var blockingRects = new List<(int Left, int Right)>();
        foreach (var comp in Components)
        {
            int rectTop = comp.Y - gridSize / 2;
            int rectBottom = comp.Y + comp.Height + gridSize / 2;

            if (maxY >= rectTop && minY <= rectBottom)
            {
                blockingRects.Add((comp.X - gridSize / 2, comp.X + comp.Width + gridSize / 2));
            }
        }

        // Calculate preferred X based on direction
        bool goingLeft = maxAllowedX < float.MaxValue - 1000 && minAllowedX <= float.MinValue + 1000;
        bool goingRight = minAllowedX > float.MinValue + 1000 && maxAllowedX >= float.MaxValue - 1000;

        float preferredX;
        if (goingLeft)
        {
            preferredX = endX - gridSize;
        }
        else if (goingRight)
        {
            preferredX = startX + gridSize;
        }
        else
        {
            preferredX = (startX + endX) / 2;
        }

        // Clamp to allowed range
        if (minAllowedX > float.MinValue + 1000)
        {
            preferredX = System.Math.Max(preferredX, minAllowedX);
        }
        if (maxAllowedX < float.MaxValue - 1000)
        {
            preferredX = System.Math.Min(preferredX, maxAllowedX);
        }

        if (blockingRects.Count == 0)
        {
            return SnapToGrid(preferredX, gridSize);
        }

        // Sort by left edge
        blockingRects.Sort((a, b) => a.Left.CompareTo(b.Left));

        // Find gaps and pick one that satisfies constraints
        float rangeStart = minAllowedX > float.MinValue + 1000 ? minAllowedX : System.Math.Min(startX, endX) - gridSize * 4;
        float rangeEnd = maxAllowedX < float.MaxValue - 1000 ? maxAllowedX : System.Math.Max(startX, endX) + gridSize * 4;

        // Check gap before first rect
        if (blockingRects[0].Left > rangeStart)
        {
            float gapCenter = (rangeStart + blockingRects[0].Left - gridSize) / 2;
            bool inRange = (minAllowedX <= float.MinValue + 1000 || gapCenter >= minAllowedX) &&
                          (maxAllowedX >= float.MaxValue - 1000 || gapCenter <= maxAllowedX);
            if (inRange)
            {
                return SnapToGrid(gapCenter, gridSize);
            }
        }

        // Check gaps between rects
        for (int i = 0; i < blockingRects.Count - 1; i++)
        {
            float gapStart = blockingRects[i].Right + gridSize;
            float gapEnd = blockingRects[i + 1].Left - gridSize;
            if (gapEnd > gapStart)
            {
                float gapCenter = (gapStart + gapEnd) / 2;
                bool inRange = (minAllowedX <= float.MinValue + 1000 || gapCenter >= minAllowedX) &&
                              (maxAllowedX >= float.MaxValue - 1000 || gapCenter <= maxAllowedX);
                if (inRange)
                {
                    return SnapToGrid(gapCenter, gridSize);
                }
            }
        }

        // Check gap after last rect
        if (blockingRects[^1].Right < rangeEnd)
        {
            float gapCenter = (blockingRects[^1].Right + gridSize + rangeEnd) / 2;
            bool inRange = (minAllowedX <= float.MinValue + 1000 || gapCenter >= minAllowedX) &&
                          (maxAllowedX >= float.MaxValue - 1000 || gapCenter <= maxAllowedX);
            if (inRange)
            {
                return SnapToGrid(gapCenter, gridSize);
            }
        }

        // Route outside all components
        if (goingRight || !goingLeft)
        {
            // Try right side first
            float rightMost = blockingRects.Max(r => r.Right);
            float rightRoute = rightMost + gridSize;
            if (minAllowedX <= float.MinValue + 1000 || rightRoute >= minAllowedX)
            {
                return SnapToGrid(rightRoute, gridSize);
            }
        }

        if (goingLeft || !goingRight)
        {
            // Try left side
            float leftMost = blockingRects.Min(r => r.Left);
            float leftRoute = leftMost - gridSize;
            if (maxAllowedX >= float.MaxValue - 1000 || leftRoute <= maxAllowedX)
            {
                return SnapToGrid(leftRoute, gridSize);
            }
        }

        // Last resort: use preferred position
        return SnapToGrid(preferredX, gridSize);
    }

    private float SnapToGrid(float value, int gridSize)
    {
        return (float)(System.Math.Round(value / gridSize) * gridSize);
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
