using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Provides dynamically-sized fonts using FontStashSharp.
/// Renders crisp text at any zoom level.
/// </summary>
public class FontService : IDisposable
{
    private readonly FontSystem _fontSystem;
    private const int BaseFontSize = 14;

    public FontService(GraphicsDevice graphicsDevice)
    {
        var settings = new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 2,
            KernelHeight = 2
        };

        _fontSystem = new FontSystem(settings);

        // Try to load Arial from Windows fonts
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (File.Exists(arialPath))
        {
            _fontSystem.AddFont(File.ReadAllBytes(arialPath));
        }
        else
        {
            // Fallback: try segoeui or other common fonts
            var segoePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
            if (File.Exists(segoePath))
            {
                _fontSystem.AddFont(File.ReadAllBytes(segoePath));
            }
            else
            {
                throw new FileNotFoundException("Could not find a suitable TTF font file.");
            }
        }
    }

    /// <summary>
    /// Gets a font at the base size (14pt).
    /// </summary>
    public SpriteFontBase GetFont()
    {
        return _fontSystem.GetFont(BaseFontSize);
    }

    /// <summary>
    /// Gets a font scaled by the given factor.
    /// </summary>
    public SpriteFontBase GetFont(float scale)
    {
        int size = Math.Max(8, (int)(BaseFontSize * scale));
        return _fontSystem.GetFont(size);
    }

    /// <summary>
    /// Gets a font at a specific size in pixels.
    /// </summary>
    public SpriteFontBase GetFontAtSize(int size)
    {
        return _fontSystem.GetFont(Math.Max(8, size));
    }

    /// <summary>
    /// Gets a font sized appropriately for the given zoom level.
    /// Higher zoom = larger font size to maintain crispness.
    /// </summary>
    public SpriteFontBase GetFontForZoom(float zoom)
    {
        // At zoom 1.0, use base size
        // At zoom 2.0, use 2x size, etc.
        int size = Math.Max(8, (int)(BaseFontSize * zoom));
        return _fontSystem.GetFont(size);
    }

    public void Dispose()
    {
        _fontSystem.Dispose();
    }
}
