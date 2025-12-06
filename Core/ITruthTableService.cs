using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Core;

public interface ITruthTableService
{
    bool IsVisible { get; set; }
    bool IsInteracting { get; }
    void Initialize(int screenWidth);
    void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, Circuit circuit);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos);
    bool ContainsPoint(Point p);
}

public class TruthTableService : ITruthTableService
{
    private TruthTableWindow? _window;

    public bool IsVisible
    {
        get => _window?.IsVisible ?? false;
        set
        {
            if (_window != null)
            {
                _window.IsVisible = value;
            }
        }
    }

    public bool IsInteracting => _window?.IsDraggingWindow ?? false;

    public void Initialize(int screenWidth)
    {
        // Position the window on the left side of the screen
        _window = new TruthTableWindow(50, 50);
        _window.IsVisible = false; // Hidden by default
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, Circuit circuit)
    {
        if (_window == null)
        {
            return;
        }

        _window.Update(mousePos, mousePressed, mouseJustPressed, mouseJustReleased, scrollDelta);

        // Handle simulate button click
        if (_window.HandleSimulateClick(mousePos, mouseJustPressed))
        {
            _window.SimulateTruthTable(circuit);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos)
    {
        _window?.Draw(spriteBatch, pixel, font, mousePos);
    }

    public bool ContainsPoint(Point p)
    {
        return _window?.ContainsPoint(p) ?? false;
    }
}
