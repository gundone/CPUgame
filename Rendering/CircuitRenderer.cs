using System.Linq;
using CPUgame.Components;
using CPUgame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

public class CircuitRenderer
{
    private readonly PrimitiveDrawer _drawer;
    private SpriteFont? _font;

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

    public Texture2D Pixel => _drawer.Pixel;

    public CircuitRenderer(GraphicsDevice graphicsDevice)
    {
        _drawer = new PrimitiveDrawer(graphicsDevice);
    }

    public void SetFont(SpriteFont font)
    {
        _font = font;
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

    public void DrawCircuit(SpriteBatch spriteBatch, Circuit circuit, Pin? selectedWire = null)
    {
        // Draw wires first (behind components)
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

        var start = new Vector2(from.WorldX, from.WorldY);
        var end = new Vector2(to.WorldX, to.WorldY);

        // Calculate routing with offset to minimize overlapping
        var midX = CalculateWireOffset(start, end);
        var mid1 = new Vector2(midX, start.Y);
        var mid2 = new Vector2(midX, end.Y);

        // Selected wires are thicker
        int thickness = isSelected ? 4 : 2;

        _drawer.DrawLine(spriteBatch, start, mid1, color, thickness);
        _drawer.DrawLine(spriteBatch, mid1, mid2, color, thickness);
        _drawer.DrawLine(spriteBatch, mid2, end, color, thickness);
    }

    /// <summary>
    /// Calculate the X position for the vertical segment of a wire.
    /// Uses pin positions to create unique offsets that spread out parallel wires.
    /// </summary>
    private float CalculateWireOffset(Vector2 start, Vector2 end)
    {
        float baseX = (start.X + end.X) / 2;
        float distance = end.X - start.X;

        // If wire is very short horizontally, use simple center
        if (System.Math.Abs(distance) < GridSize * 2)
        {
            return baseX;
        }

        // Calculate offset based on both Y positions to create unique paths
        // Use a hash-like approach: combine Y coordinates to get variety
        float ySum = start.Y + end.Y;
        float yDiff = start.Y - end.Y;

        // Create offset factor based on Y positions (range: -0.3 to 0.3 of distance)
        // Using modulo to keep offsets distributed
        float offsetFactor = ((ySum % (GridSize * 10)) / (GridSize * 10)) - 0.5f;
        offsetFactor *= 0.4f; // Scale down to 40% of range

        // Add secondary offset based on Y difference for more separation
        float secondaryOffset = ((yDiff % (GridSize * 5)) / (GridSize * 5)) * 0.2f;

        // Calculate final offset, keeping wire within reasonable bounds
        float offset = distance * (offsetFactor + secondaryOffset);

        // Ensure the vertical segment stays between the two endpoints
        float minX = System.Math.Min(start.X, end.X) + GridSize;
        float maxX = System.Math.Max(start.X, end.X) - GridSize;

        return System.Math.Clamp(baseX + offset, minX, maxX);
    }

    public void DrawWirePreview(SpriteBatch spriteBatch, Vector2 start, Vector2 end)
    {
        var midX = CalculateWireOffset(start, end);
        var mid1 = new Vector2(midX, start.Y);
        var mid2 = new Vector2(midX, end.Y);

        var color = new Color(150, 150, 170, 150);
        _drawer.DrawLine(spriteBatch, start, mid1, color, 2);
        _drawer.DrawLine(spriteBatch, mid1, mid2, color, 2);
        _drawer.DrawLine(spriteBatch, mid2, end, color, 2);
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

    private void DrawNandGate(SpriteBatch spriteBatch, Component component, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        // Draw NAND symbol (simplified)
        var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

        // Draw label
        if (_font != null)
        {
            var text = "&";
            var textSize = _font.MeasureString(text);
            spriteBatch.DrawString(_font, text,
                center - textSize / 2,
                TextColor);
        }

        // Small circle for negation
        _drawer.DrawFilledCircle(spriteBatch, new Vector2(rect.Right - 5, center.Y), 4, TextColor);
    }

    private void DrawSwitch(SpriteBatch spriteBatch, InputSwitch sw, Rectangle rect, Color borderColor)
    {
        var color = sw.IsOn ? SwitchOnColor : SwitchOffColor;
        _drawer.DrawRectangle(spriteBatch, rect, color);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_font != null)
        {
            var text = sw.IsOn ? "1" : "0";
            var textSize = _font.MeasureString(text);
            var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            spriteBatch.DrawString(_font, text, center - textSize / 2, TextColor);
        }
    }

    private void DrawLed(SpriteBatch spriteBatch, OutputLed led, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        // Draw LED circle
        var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        var color = led.IsLit ? LedOnColor : LedOffColor;
        _drawer.DrawFilledCircle(spriteBatch, center, 12, color);
    }

    private void DrawClock(SpriteBatch spriteBatch, Clock clk, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_font != null)
        {
            var text = "CLK";
            var textSize = _font.MeasureString(text);
            var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            spriteBatch.DrawString(_font, text, center - textSize / 2, TextColor);
        }
    }

    private void DrawCustomComponent(SpriteBatch spriteBatch, CustomComponent custom, Rectangle rect, Color borderColor)
    {
        // Custom components have a distinct purple tint
        var customColor = new Color(70, 60, 90);
        _drawer.DrawRectangle(spriteBatch, rect, customColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_font != null)
        {
            var text = custom.ComponentName;
            // Truncate if too long
            if (text.Length > 8)
                text = text.Substring(0, 7) + "..";
            var textSize = _font.MeasureString(text);
            var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            spriteBatch.DrawString(_font, text, center - textSize / 2, TextColor);
        }
    }

    private void DrawBusInput(SpriteBatch spriteBatch, BusInput busIn, Rectangle rect, Color borderColor)
    {
        // Input bus has a greenish tint
        var busColor = new Color(50, 70, 60);
        _drawer.DrawRectangle(spriteBatch, rect, busColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_font != null)
        {
            var centerX = rect.X + rect.Width / 2;

            // Draw decimal value above the component
            var decText = busIn.Value.ToString();
            var decSize = _font.MeasureString(decText);
            spriteBatch.DrawString(_font, decText,
                new Vector2(centerX - decSize.X / 2, rect.Y - decSize.Y - 2), WireOnColor);

            // Draw label inside
            var label = "IN";
            var labelSize = _font.MeasureString(label);
            spriteBatch.DrawString(_font, label,
                new Vector2(centerX - labelSize.X / 2, rect.Y + 4), TextColor);

            // Draw pin values if enabled
            if (busIn.ShowPinValues)
            {
                for (int i = 0; i < busIn.Outputs.Count; i++)
                {
                    var pin = busIn.Outputs[i];
                    var bitValue = pin.Value == Signal.High ? "1" : "0";
                    var bitSize = _font.MeasureString(bitValue);
                    // Draw inside the component, left of the pin
                    spriteBatch.DrawString(_font, bitValue,
                        new Vector2(rect.Right - bitSize.X - 8, pin.WorldY - bitSize.Y / 2),
                        pin.Value == Signal.High ? WireOnColor : WireOffColor);
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

        if (_font != null)
        {
            var centerX = rect.X + rect.Width / 2;

            // Draw decimal value above the component (unconnected pins treated as 0)
            var decText = busOut.GetValue().ToString();
            var decSize = _font.MeasureString(decText);
            spriteBatch.DrawString(_font, decText,
                new Vector2(centerX - decSize.X / 2, rect.Y - decSize.Y - 2), WireOnColor);

            // Draw label inside
            var label = "OUT";
            var labelSize = _font.MeasureString(label);
            spriteBatch.DrawString(_font, label,
                new Vector2(centerX - labelSize.X / 2, rect.Y + 4), TextColor);

            // Draw pin values if enabled
            if (busOut.ShowPinValues)
            {
                for (int i = 0; i < busOut.Inputs.Count; i++)
                {
                    var pin = busOut.Inputs[i];
                    // Unconnected/undefined pins are treated as 0
                    var bitValue = pin.Value == Signal.High ? "1" : "0";
                    var bitSize = _font.MeasureString(bitValue);
                    var pinColor = pin.Value == Signal.High ? WireOnColor : WireOffColor;
                    // Draw inside the component, right of the pin
                    spriteBatch.DrawString(_font, bitValue,
                        new Vector2(rect.X + 8, pin.WorldY - bitSize.Y / 2), pinColor);
                }
            }
        }
    }

    private void DrawGenericComponent(SpriteBatch spriteBatch, Component component, Rectangle rect, Color borderColor)
    {
        _drawer.DrawRectangle(spriteBatch, rect, ComponentColor);
        _drawer.DrawRectangleOutline(spriteBatch, rect, borderColor, 2);

        if (_font != null)
        {
            var text = component.Name;
            var textSize = _font.MeasureString(text);
            var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            spriteBatch.DrawString(_font, text, center - textSize / 2, TextColor);
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
}
