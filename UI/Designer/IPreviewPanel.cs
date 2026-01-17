using System;
using CPUgame.Core.Designer;
using CPUgame.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Interface for the preview panel in the designer.
/// </summary>
public interface IPreviewPanel
{
    int SelectedPinIndex { get; }

    void SetAppearance(ComponentAppearance? appearance, string? componentType);
    void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, Rectangle bounds);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle bounds, IFontService fontService);
    void Reset();

    event Action<int>? OnPinSelected;
    event Action? OnAppearanceChanged;
}
