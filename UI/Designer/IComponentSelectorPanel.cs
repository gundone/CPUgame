using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Interface for the component selector panel in the designer.
/// </summary>
public interface IComponentSelectorPanel
{
    string? SelectedComponentType { get; }

    void RefreshComponentList(IReadOnlyDictionary<string, object> customComponents);
    void Update(Point mousePos, bool mouseJustPressed, int scrollDelta, Rectangle bounds);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle bounds);
    void Reset();

    event Action<string>? OnComponentSelected;
}
