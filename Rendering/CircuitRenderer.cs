using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Primitives;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

public class CircuitRenderer : ICircuitRenderer
{
    private readonly IPrimitiveDrawer _drawer;
    private IFontService? _fontService;

    // Colors
    public static readonly Color BackgroundColor = new(30, 30, 35);
    public static readonly Color GridColor = new(45, 45, 50);
    public static readonly Color ComponentColor = new(60, 60, 70);
    public static readonly Color ComponentBorderColor = new(100, 100, 120);
    public static readonly Color SelectedBorderColor = new(100, 180, 255);
    public static readonly Color WireOffColor = new(80, 80, 90);
    public static readonly Color WireOnColor = new(50, 255, 100);
    public static readonly Color WireUndefinedColor = new(255, 100, 100);
    public static readonly Color PinColor = new(180, 180, 200);
    public static readonly Color TextColor = new(220, 220, 230);
    public static readonly Color LedOnColor = new(50, 255, 100);
    public static readonly Color LedOffColor = new(40, 60, 40);
    public static readonly Color SwitchOnColor = new(100, 200, 255);
    public static readonly Color SwitchOffColor = new(60, 80, 100);

    public int GridSize { get; set; } = 20;
    public float CurrentZoom { get; set; } = 1f;

    public Texture2D Pixel => _drawer.Pixel;

    public CircuitRenderer(IPrimitiveDrawer drawer)
    {
        _drawer = drawer;
    }

    public void Initialize(GraphicsDevice graphicsDevice, IFontService fontService)
    {
        _drawer.Initialize(graphicsDevice);
        _fontService = fontService;
    }

    /// <summary>
    /// Gets a font appropriately sized for the current zoom level.
    /// </summary>
    private SpriteFontBase GetFont()
    {
        return _fontService?.GetFontForZoom(CurrentZoom) ?? throw new InvalidOperationException("FontService not initialized");
    }

    public void DrawGrid(SpriteBatch spriteBatch, float cameraX, float cameraY, int screenWidth, int screenHeight, float zoom)
    {
        // Calculate visible world area
        float worldLeft = cameraX;
        float worldTop = cameraY;
        float worldRight = cameraX + screenWidth / zoom;
        float worldBottom = cameraY + screenHeight / zoom;

        // Snap to grid boundaries
        int startX = ((int)worldLeft / GridSize) * GridSize;
        int startY = ((int)worldTop / GridSize) * GridSize;
        int endX = ((int)worldRight / GridSize + 1) * GridSize;
        int endY = ((int)worldBottom / GridSize + 1) * GridSize;

        // Draw vertical lines
        for (int x = startX; x <= endX; x += GridSize)
        {
            _drawer.DrawLine(spriteBatch, new Vector2(x, startY), new Vector2(x, endY), GridColor, 1);
        }

        // Draw horizontal lines
        for (int y = startY; y <= endY; y += GridSize)
        {
            _drawer.DrawLine(spriteBatch, new Vector2(startX, y), new Vector2(endX, y), GridColor, 1);
        }
    }

    // Cache for component rectangles during wire drawing
    private List<Rectangle> _componentRects = new();

    // Segment overlap tracking
    private Dictionary<(int, int, int, int), int> _segmentCounts = new();

    public void DrawCircuit(SpriteBatch spriteBatch, Circuit circuit, Pin? selectedWire = null)
    {
        // Cache component rectangles for wire routing
        _componentRects.Clear();
        foreach (var comp in circuit.Components)
        {
            // Add padding around components for wire clearance
            _componentRects.Add(new Rectangle(
                comp.X - GridSize / 2,
                comp.Y - GridSize / 2,
                comp.Width + GridSize,
                comp.Height + GridSize));
        }

        // First pass: count overlapping segments
        _segmentCounts.Clear();
        foreach (var component in circuit.Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ConnectedTo != null)
                {
                    CountWireSegments(input.ConnectedTo, input);
                }
            }
        }

        // Second pass: draw wires with thickness based on overlap
        foreach (var component in circuit.Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ConnectedTo != null)
                {
                    bool isSelected = input == selectedWire;
                    DrawWire(spriteBatch, input.ConnectedTo, input, isSelected);
                }
            }
        }

        // Draw components
        foreach (var component in circuit.Components)
        {
            DrawComponent(spriteBatch, component);
        }
    }

    private void CountWireSegments(Pin from, Pin to)
    {
        List<Vector2> path;
        if (to.ManualWirePath != null && to.ManualWirePath.Count >= 2)
        {
            path = new List<Vector2>();
            foreach (var point in to.ManualWirePath)
            {
                path.Add(new Vector2(point.X, point.Y));
            }
        }
        else
        {
            var start = new Vector2(from.WorldX, from.WorldY);
            var end = new Vector2(to.WorldX, to.WorldY);
            path = CalculateWirePath(start, end);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            var key = NormalizeSegment(path[i], path[i + 1]);
            if (_segmentCounts.ContainsKey(key))
            {
                _segmentCounts[key]++;
            }
            else
            {
                _segmentCounts[key] = 1;
            }
        }
    }

    private (int, int, int, int) NormalizeSegment(Vector2 a, Vector2 b)
    {
        // Snap to grid and normalize order for consistent keys
        int x1 = (int)Math.Round(a.X / GridSize) * GridSize;
        int y1 = (int)Math.Round(a.Y / GridSize) * GridSize;
        int x2 = (int)Math.Round(b.X / GridSize) * GridSize;
        int y2 = (int)Math.Round(b.Y / GridSize) * GridSize;

        // Normalize order: smaller coordinates first
        if (x1 > x2 || (x1 == x2 && y1 > y2))
        {
            return (x2, y2, x1, y1);
        }
        return (x1, y1, x2, y2);
    }

    public void DrawWire(SpriteBatch spriteBatch, Pin from, Pin to, bool isSelected = false)
    {
        var color = from.Value switch
        {
            Signal.High => WireOnColor,
            Signal.Low => WireOffColor,
            _ => WireUndefinedColor
        };

        // Selected wires use highlight color
        if (isSelected)
        {
            color = SelectedBorderColor;
        }

        // Check if this connection has a manual wire path
        List<Vector2> path;
        if (to.ManualWirePath != null && to.ManualWirePath.Count >= 2)
        {
            // Use manual path
            path = new List<Vector2>();
            foreach (var point in to.ManualWirePath)
            {
                path.Add(new Vector2(point.X, point.Y));
            }
        }
        else
        {
            // Use auto-routing
            var start = new Vector2(from.WorldX, from.WorldY);
            var end = new Vector2(to.WorldX, to.WorldY);
            path = CalculateWirePath(start, end);
        }

        // Draw all segments of the path
        for (int i = 0; i < path.Count - 1; i++)
        {
            var key = NormalizeSegment(path[i], path[i + 1]);
            int overlapCount = _segmentCounts.GetValueOrDefault(key, 1);

            // Base thickness + extra for overlaps, selected wires are thicker
            int thickness = isSelected ? 4 : 2 + (overlapCount - 1);

            _drawer.DrawLine(spriteBatch, path[i], path[i + 1], color, thickness);
        }
    }

    /// <summary>
    /// Calculate wire path that avoids components.
    /// Wire always starts going RIGHT from output pin (start) and ends going LEFT into input pin (end).
    /// </summary>
    private List<Vector2> CalculateWirePath(Vector2 start, Vector2 end)
    {
        var path = new List<Vector2> { start };

        // Wire must go RIGHT from output (start.X increases first)
        // Wire must approach input from LEFT (end.X approached from smaller X)

        float margin = GridSize;

        if (start.X + margin < end.X - margin)
        {
            // Normal case: enough space between start and end
            // Path: start -> right -> vertical -> left into end
            float midX = FindClearVerticalX(start, end, start.X + margin, end.X - margin);
            path.Add(new Vector2(midX, start.Y));
            path.Add(new Vector2(midX, end.Y));
        }
        else
        {
            // Reverse/tight case: need to wrap around
            // Path: start -> right -> vertical (outside) -> left -> vertical -> into end from left
            float rightX = FindClearVerticalX(start, end, start.X + margin, float.MaxValue);
            float leftX = FindClearVerticalX(start, end, float.MinValue, end.X - margin);

            // Determine wrap direction (go above or below)
            float wrapY;
            float minY = Math.Min(start.Y, end.Y);
            float maxY = Math.Max(start.Y, end.Y);

            // Find clear Y for horizontal segment
            wrapY = FindClearHorizontalY(leftX, rightX, minY, maxY);

            path.Add(new Vector2(rightX, start.Y));
            path.Add(new Vector2(rightX, wrapY));
            path.Add(new Vector2(leftX, wrapY));
            path.Add(new Vector2(leftX, end.Y));
        }

        path.Add(end);
        return path;
    }

    /// <summary>
    /// Find a clear Y position for horizontal segment that avoids components.
    /// </summary>
    private float FindClearHorizontalY(float leftX, float rightX, float minY, float maxY)
    {
        // Collect components in the X range
        var blockingRects = new List<Rectangle>();
        foreach (var rect in _componentRects)
        {
            if (rightX >= rect.Left && leftX <= rect.Right)
            {
                blockingRects.Add(rect);
            }
        }

        // Try going above
        float topMost = minY;
        foreach (var rect in blockingRects)
        {
            if (rect.Top < topMost)
            {
                topMost = rect.Top;
            }
        }
        float aboveY = topMost - GridSize;

        // Try going below
        float bottomMost = maxY;
        foreach (var rect in blockingRects)
        {
            if (rect.Bottom > bottomMost)
            {
                bottomMost = rect.Bottom;
            }
        }
        float belowY = bottomMost + GridSize;

        // Choose the closer option
        float distAbove = Math.Abs((minY + maxY) / 2 - aboveY);
        float distBelow = Math.Abs((minY + maxY) / 2 - belowY);

        return distAbove <= distBelow ? SnapToGrid(aboveY) : SnapToGrid(belowY);
    }

    /// <summary>
    /// Find a clear X position for vertical segment that avoids all components.
    /// The position must be within [minAllowedX, maxAllowedX].
    /// </summary>
    private float FindClearVerticalX(Vector2 start, Vector2 end, float minAllowedX, float maxAllowedX)
    {
        float minY = Math.Min(start.Y, end.Y);
        float maxY = Math.Max(start.Y, end.Y);

        // Collect all components that could block a vertical line between start.Y and end.Y
        var blockingRects = new List<Rectangle>();
        foreach (var rect in _componentRects)
        {
            // Check if component overlaps the Y range
            if (maxY >= rect.Top && minY <= rect.Bottom)
            {
                blockingRects.Add(rect);
            }
        }

        // Calculate preferred X based on constraints
        float preferredX;
        bool goingLeft = maxAllowedX < float.MaxValue && minAllowedX <= float.MinValue + 1000;
        bool goingRight = minAllowedX > float.MinValue + 1000 && maxAllowedX >= float.MaxValue - 1000;

        if (goingLeft)
        {
            // Prefer leftmost position when going left
            preferredX = end.X - GridSize;
        }
        else if (goingRight)
        {
            // Prefer rightmost position when going right
            preferredX = start.X + GridSize;
        }
        else
        {
            // Normal case: prefer middle
            preferredX = (start.X + end.X) / 2;
        }

        // Clamp to allowed range
        if (minAllowedX > float.MinValue + 1000)
        {
            preferredX = Math.Max(preferredX, minAllowedX);
        }
        if (maxAllowedX < float.MaxValue - 1000)
        {
            preferredX = Math.Min(preferredX, maxAllowedX);
        }

        // If no blocking components, use preferred position
        if (blockingRects.Count == 0)
        {
            return SnapToGrid(preferredX);
        }

        // Determine search range
        float rangeStart = minAllowedX > float.MinValue + 1000 ? minAllowedX : Math.Min(start.X, end.X) - GridSize * 4;
        float rangeEnd = maxAllowedX < float.MaxValue - 1000 ? maxAllowedX : Math.Max(start.X, end.X) + GridSize * 4;

        // Find gaps between components where we can route
        var gaps = FindVerticalGaps(blockingRects, rangeStart, rangeEnd);

        // Try to find a gap that satisfies constraints
        foreach (var gap in gaps)
        {
            float gapCenter = (gap.Left + gap.Right) / 2;
            bool inRange = (minAllowedX <= float.MinValue + 1000 || gapCenter >= minAllowedX) &&
                          (maxAllowedX >= float.MaxValue - 1000 || gapCenter <= maxAllowedX);
            if (inRange)
            {
                return SnapToGrid(gapCenter);
            }
        }

        // Route outside all components
        if (goingRight || !goingLeft)
        {
            // Try right side first
            float rightMost = float.MinValue;
            foreach (var rect in blockingRects)
            {
                rightMost = Math.Max(rightMost, rect.Right);
            }
            float rightRoute = rightMost + GridSize;
            if (minAllowedX <= float.MinValue + 1000 || rightRoute >= minAllowedX)
            {
                return SnapToGrid(rightRoute);
            }
        }

        if (goingLeft || !goingRight)
        {
            // Try left side
            float leftMost = float.MaxValue;
            foreach (var rect in blockingRects)
            {
                leftMost = Math.Min(leftMost, rect.Left);
            }
            float leftRoute = leftMost - GridSize;
            if (maxAllowedX >= float.MaxValue - 1000 || leftRoute <= maxAllowedX)
            {
                return SnapToGrid(leftRoute);
            }
        }

        // Last resort: use preferred position even if blocked
        return SnapToGrid(preferredX);
    }

    /// <summary>
    /// Find vertical gaps between blocking rectangles.
    /// </summary>
    private List<(float Left, float Right)> FindVerticalGaps(List<Rectangle> rects, float rangeStart, float rangeEnd)
    {
        var gaps = new List<(float Left, float Right)>();

        if (rects.Count == 0)
        {
            return gaps;
        }

        // Sort rectangles by their left edge
        var sorted = rects.OrderBy(r => r.Left).ToList();

        // Check gap before first rectangle
        float minRange = rangeStart - GridSize * 2;
        if (sorted[0].Left > minRange)
        {
            gaps.Add((minRange, sorted[0].Left - GridSize));
        }

        // Check gaps between rectangles
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            float gapStart = sorted[i].Right + GridSize;
            float gapEnd = sorted[i + 1].Left - GridSize;
            if (gapEnd > gapStart)
            {
                gaps.Add((gapStart, gapEnd));
            }
        }

        // Check gap after last rectangle
        float maxRange = rangeEnd + GridSize * 2;
        if (sorted[sorted.Count - 1].Right < maxRange)
        {
            gaps.Add((sorted[sorted.Count - 1].Right + GridSize, maxRange));
        }

        return gaps;
    }

    /// <summary>
    /// Snap a coordinate to the grid.
    /// </summary>
    private float SnapToGrid(float value)
    {
        return (float)(Math.Round(value / GridSize) * GridSize);
    }

    public void DrawWirePreview(SpriteBatch spriteBatch, Vector2 start, Vector2 end)
    {
        var path = CalculateWirePath(start, end);

        var color = new Color(150, 150, 170, 150);
        for (int i = 0; i < path.Count - 1; i++)
        {
            _drawer.DrawLine(spriteBatch, path[i], path[i + 1], color);
        }
    }

    public void DrawManualWirePreview(SpriteBatch spriteBatch, IReadOnlyList<Point2> pathPoints, Vector2 currentMousePos)
    {
        if (pathPoints.Count == 0)
        {
            return;
        }

        var wireColor = new Color(100, 180, 255, 200);
        var nodeColor = new Color(255, 200, 100);

        // Draw existing path segments
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            var p1 = new Vector2(pathPoints[i].X, pathPoints[i].Y);
            var p2 = new Vector2(pathPoints[i + 1].X, pathPoints[i + 1].Y);
            _drawer.DrawLine(spriteBatch, p1, p2, wireColor);
        }

        // Draw preview line to current mouse position (snapped to grid)
        var lastPoint = new Vector2(pathPoints[^1].X, pathPoints[^1].Y);
        var snappedMouse = new Vector2(
            (float)(Math.Round(currentMousePos.X / GridSize) * GridSize),
            (float)(Math.Round(currentMousePos.Y / GridSize) * GridSize));

        var previewColor = new Color(100, 180, 255, 100);
        _drawer.DrawLine(spriteBatch, lastPoint, snappedMouse, previewColor);

        // Draw nodes at each path point
        foreach (var point in pathPoints)
        {
            _drawer.DrawFilledCircle(spriteBatch, new Vector2(point.X, point.Y), 4, nodeColor);
        }

        // Draw preview node at snapped mouse position
        _drawer.DrawFilledCircle(spriteBatch, snappedMouse, 4, new Color(255, 200, 100, 100));
    }

    public void DrawComponent(SpriteBatch spriteBatch, Component component)
    {
        var rect = new Rectangle(component.X, component.Y, component.Width, component.Height);
        var borderColor = component.IsSelected ? SelectedBorderColor : ComponentBorderColor;

        // Special rendering for different component types
        switch (component)
        {
            case InputSwitch sw:
                DrawSwitch(spriteBatch, sw, rect, borderColor);
                break;
            case OutputLed led:
                DrawLed(spriteBatch, led, rect, borderColor);
                break;
            case NandGate:
                DrawNandGate(spriteBatch, component, rect, borderColor);
                break;
            case Clock clk:
                DrawClock(spriteBatch, clk, rect, borderColor);
                break;
            case CustomComponent custom:
                DrawCustomComponent(spriteBatch, custom, rect, borderColor);
                break;
            case BusInput busIn:
                DrawBusInput(spriteBatch, busIn, rect, borderColor);
                break;
            case BusOutput busOut:
                DrawBusOutput(spriteBatch, busOut, rect, borderColor);
                break;
            default:
                DrawGenericComponent(spriteBatch, component, rect, borderColor);
                break;
        }

        // Draw pins
        foreach (var pin in component.Inputs.Concat(component.Outputs))
        {
            DrawPin(spriteBatch, pin);
        }
    }

    private void DrawNandGate(SpriteBatch spriteBatch, Component _, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        // Draw NAND symbol (simplified)
        var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);

        // Draw label
        if (_fontService != null)
        {
            var font = GetFont();
            var text = "&";
            var textSize = font.MeasureString(text);
            font.DrawText(spriteBatch, text,
                center - textSize / 2 / CurrentZoom,
                TextColor,
                scale: new Vector2(1f / CurrentZoom));
        }

        // Small circle for negation
        _drawer.DrawFilledCircle(spriteBatch, new Vector2(rect.Right - 5, center.Y), 4, TextColor);
    }

    private void DrawSwitch(SpriteBatch spriteBatch, InputSwitch sw, Rectangle rect, Color borderColor)
    {
        var color = sw.IsOn ? SwitchOnColor : SwitchOffColor;
        _drawer.DrawRectangle(spriteBatch, rect, color);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var text = sw.IsOn ? "1" : "0";
            var textSize = font.MeasureString(text);
            var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);
            font.DrawText(spriteBatch, text, center - textSize / 2 / CurrentZoom, TextColor,
                scale: new Vector2(1f / CurrentZoom));
        }
    }

    private void DrawLed(SpriteBatch spriteBatch, OutputLed led, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        // Draw LED circle
        var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);
        var color = led.IsLit ? LedOnColor : LedOffColor;
        _drawer.DrawFilledCircle(spriteBatch, center, 12, color);
    }

    private void DrawClock(SpriteBatch spriteBatch, Clock _, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var text = "CLK";
            var textSize = font.MeasureString(text);
            var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);
            font.DrawText(spriteBatch, text, center - textSize / 2 / CurrentZoom, TextColor,
                scale: new Vector2(1f / CurrentZoom));
        }
    }

    private void DrawCustomComponent(SpriteBatch spriteBatch, CustomComponent custom, Rectangle rect, Color borderColor)
    {
        // Custom components have a distinct purple tint
        var customColor = new Color(70, 60, 90);
        _drawer.DrawRectangle(spriteBatch, rect, customColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var text = custom.ComponentName;
            // Truncate if too long
            if (text.Length > 8)
            {
                text = text.Substring(0, 7) + "..";
            }
            var textSize = font.MeasureString(text);
            var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);
            font.DrawText(spriteBatch, text, center - textSize / 2 / CurrentZoom, TextColor,
                scale: new Vector2(1f / CurrentZoom));
        }
    }

    private void DrawBusInput(SpriteBatch spriteBatch, BusInput busIn, Rectangle rect, Color borderColor)
    {
        // Input bus has a greenish tint
        var busColor = new Color(50, 70, 60);
        _drawer.DrawRectangle(spriteBatch, rect, busColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var scale = new Vector2(1f / CurrentZoom);
            var centerX = rect.X + rect.Width / 2f;

            // Draw decimal value above the component
            var decText = busIn.Value.ToString();
            var decSize = font.MeasureString(decText) / CurrentZoom;
            font.DrawText(spriteBatch, decText,
                new Vector2(centerX - decSize.X / 2, rect.Y - decSize.Y - 2), WireOnColor, scale: scale);

            // Draw label inside
            var label = "IN";
            var labelSize = font.MeasureString(label) / CurrentZoom;
            font.DrawText(spriteBatch, label,
                new Vector2(centerX - labelSize.X / 2, rect.Y + 4), TextColor, scale: scale);

            // Draw pin values if enabled
            if (busIn.ShowPinValues)
            {
                for (int i = 0; i < busIn.Outputs.Count; i++)
                {
                    var pin = busIn.Outputs[i];
                    var bitValue = pin.Value == Signal.High ? "1" : "0";
                    var bitSize = font.MeasureString(bitValue) / CurrentZoom;
                    // Draw inside the component, left of the pin
                    font.DrawText(spriteBatch, bitValue,
                        new Vector2(rect.Right - bitSize.X - 8, pin.WorldY - bitSize.Y / 2),
                        pin.Value == Signal.High ? WireOnColor : WireOffColor, scale: scale);
                }
            }
        }
    }

    private void DrawBusOutput(SpriteBatch spriteBatch, BusOutput busOut, Rectangle rect, Color borderColor)
    {
        // Output bus has a reddish tint
        var busColor = new Color(70, 50, 60);
        _drawer.DrawRectangle(spriteBatch, rect, busColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var scale = new Vector2(1f / CurrentZoom);
            var centerX = rect.X + rect.Width / 2f;

            // Draw decimal value above the component (unconnected pins treated as 0)
            var decText = busOut.GetValue().ToString();
            var decSize = font.MeasureString(decText) / CurrentZoom;
            font.DrawText(spriteBatch, decText,
                new Vector2(centerX - decSize.X / 2, rect.Y - decSize.Y - 2), WireOnColor, scale: scale);

            // Draw label inside
            var label = "OUT";
            var labelSize = font.MeasureString(label) / CurrentZoom;
            font.DrawText(spriteBatch, label,
                new Vector2(centerX - labelSize.X / 2, rect.Y + 4), TextColor, scale: scale);

            // Draw pin values if enabled
            if (busOut.ShowPinValues)
            {
                for (int i = 0; i < busOut.Inputs.Count; i++)
                {
                    var pin = busOut.Inputs[i];
                    // Unconnected/undefined pins are treated as 0
                    var bitValue = pin.Value == Signal.High ? "1" : "0";
                    var bitSize = font.MeasureString(bitValue) / CurrentZoom;
                    var pinColor = pin.Value == Signal.High ? WireOnColor : WireOffColor;
                    // Draw inside the component, right of the pin
                    font.DrawText(spriteBatch, bitValue,
                        new Vector2(rect.X + 8, pin.WorldY - bitSize.Y / 2), pinColor, scale: scale);
                }
            }
        }
    }

    private void DrawGenericComponent(SpriteBatch spriteBatch, Component component, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_fontService != null)
        {
            var font = GetFont();
            var text = component.Name;
            var textSize = font.MeasureString(text);
            var center = new Vector2(rect.X + (float)rect.Width / 2, rect.Y + (float)rect.Height / 2);
            font.DrawText(spriteBatch, text, center - textSize / 2 / CurrentZoom, TextColor,
                scale: new Vector2(1f / CurrentZoom));
        }
    }

    private void DrawPin(SpriteBatch spriteBatch, Pin pin)
    {
        var color = pin.Value switch
        {
            Signal.High => WireOnColor,
            Signal.Low => PinColor,
            _ => WireUndefinedColor
        };

        _drawer.DrawFilledCircle(spriteBatch, new Vector2(pin.WorldX, pin.WorldY), 5, color);
    }

    public void DrawPinHighlight(SpriteBatch spriteBatch, Pin pin)
    {
        _drawer.DrawFilledCircle(spriteBatch, new Vector2(pin.WorldX, pin.WorldY), 8, SelectedBorderColor);
    }

    public void DrawManualWireNodes(SpriteBatch spriteBatch, Pin inputPin, int draggingNodeIndex = -1)
    {
        if (inputPin.ManualWirePath == null || inputPin.ManualWirePath.Count < 2)
        {
            return;
        }

        var path = inputPin.ManualWirePath;
        var nodeColor = new Color(255, 200, 100);
        var draggingNodeColor = new Color(255, 255, 150);
        var endpointColor = new Color(180, 180, 200);

        // Draw all nodes
        for (int i = 0; i < path.Count; i++)
        {
            var point = path[i];
            var pos = new Vector2(point.X, point.Y);

            // First and last points are endpoints (pins) - they can't be moved
            if (i == 0 || i == path.Count - 1)
            {
                // Draw endpoint indicator (smaller, different color)
                _drawer.DrawFilledCircle(spriteBatch, pos, 4, endpointColor);
                _drawer.DrawCircle(spriteBatch, pos, 6, nodeColor);
            }
            else
            {
                // Internal node - can be moved
                var color = (i == draggingNodeIndex) ? draggingNodeColor : nodeColor;
                _drawer.DrawFilledCircle(spriteBatch, pos, 6, color);
                _drawer.DrawCircle(spriteBatch, pos, 8, SelectedBorderColor);
            }
        }
    }
}
