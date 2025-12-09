using System;
using System.Collections.Generic;
using CPUgame.Core.Levels;
using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Core;

public interface ITruthTableService
{
    bool IsVisible { get; set; }
    bool IsInteracting { get; }
    bool IsSimulating { get; }
    bool IsLevelPassed { get; }
    List<TruthTableRow> TruthTableRows { get; }
    event Action? OnLevelPassed;
    void Initialize(int screenWidth);
    void Show(Circuit.Circuit circuit, SpriteFont font);
    void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, Circuit.Circuit circuit, double deltaTime);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos);
    bool ContainsPoint(Point p);
    void SetCurrentLevel(GameLevel? level);
}

public class TruthTableService : ITruthTableService
{
    private TruthTableWindow? _window;
    private bool _isContinuousSimulation;
    private double _simulationTimer;
    private bool _wasLevelPassed;
    private const double SimulationInterval = 0.5; // Recalculate every 0.5 seconds

    public event Action? OnLevelPassed;

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
    public bool IsSimulating => _isContinuousSimulation;
    public bool IsLevelPassed => _window?.IsLevelPassed ?? false;
    public List<TruthTableRow> TruthTableRows => _window?.TruthTableRows ?? new List<TruthTableRow>();

    public void Initialize(int screenWidth)
    {
        // Position the window on the left side of the screen
        _window = new TruthTableWindow(50, 50);
        _window.IsVisible = false; // Hidden by default
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, Circuit.Circuit circuit, double deltaTime)
    {
        if (_window == null)
        {
            return;
        }

        _window.Update(mousePos, mousePressed, mouseJustPressed, mouseJustReleased, scrollDelta, deltaTime);

        // Handle button clicks
        var action = _window.HandleButtonClick(mousePos, mouseJustPressed);
        switch (action)
        {
            case TruthTableAction.Play:
                _isContinuousSimulation = true;
                _window.IsSimulating = true;
                _simulationTimer = 0; // Trigger immediate simulation
                break;
            case TruthTableAction.Stop:
                _isContinuousSimulation = false;
                _window.IsSimulating = false;
                break;
            case TruthTableAction.Clear:
                _isContinuousSimulation = false;
                _window.IsSimulating = false;
                _window.ClearTable();
                break;
        }

        // Continuous simulation with timer
        if (_isContinuousSimulation)
        {
            _simulationTimer += deltaTime;
            if (_simulationTimer >= SimulationInterval)
            {
                _simulationTimer = 0;
                _window.SimulateTruthTable(circuit);
                _window.UpdateRowMatchStatus();
                _window.IsSimulating = true; // Keep it active

                // Notify if level passed
                if (_window.IsLevelPassed && !_wasLevelPassed)
                {
                    _wasLevelPassed = true;
                    OnLevelPassed?.Invoke();
                }
                else if (!_window.IsLevelPassed)
                {
                    _wasLevelPassed = false;
                }
            }
        }
    }

    public void SetCurrentLevel(GameLevel? level)
    {
        _window?.SetCurrentLevel(level);
        _wasLevelPassed = false;
    }

    public void Show(Circuit.Circuit circuit, SpriteFont font)
    {
        if (_window != null)
        {
            _window.RecalculateSize(circuit, font);
            _window.IsVisible = true;
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
