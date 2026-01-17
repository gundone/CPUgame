using System;
using System.Collections.Generic;
using CPUgame.Core.Levels;
using CPUgame.Core.TruthTable;
using FontStashSharp;
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
    void Show(Circuit.Circuit circuit, SpriteFontBase font);
    void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, Circuit.Circuit circuit, double deltaTime);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Point mousePos);
    bool ContainsPoint(Point p);
    void SetCurrentLevel(GameLevel? level);
    void SetPosition(int x, int y);
}