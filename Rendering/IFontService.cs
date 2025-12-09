using System;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Interface for dynamically-sized font rendering
/// </summary>
public interface IFontService : IDisposable
{
    void Initialize(GraphicsDevice graphicsDevice);
    SpriteFontBase GetFont();
    SpriteFontBase GetFont(float scale);
    SpriteFontBase GetFontAtSize(int size);
    SpriteFontBase GetFontForZoom(float zoom);
}
