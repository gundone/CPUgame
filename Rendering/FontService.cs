using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Provides dynamically-sized fonts using FontStashSharp.
/// Renders crisp text at any zoom level.
/// </summary>
public class FontService : IFontService
{
    private FontSystem? _fontSystem;
    private const int BaseFontSize = 16;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        var settings = new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 2,
            KernelHeight = 2
        };

        _fontSystem = new FontSystem(settings);

        var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        bool primaryFontLoaded = false;

        // Try to load primary font (Arial or Segoe UI)
        var arialPath = Path.Combine(fontsFolder, "arial.ttf");
        var segoePath = Path.Combine(fontsFolder, "segoeui.ttf");
        if (File.Exists(segoePath))
        {
            _fontSystem.AddFont(File.ReadAllBytes(segoePath));
            primaryFontLoaded = true;
        }
        else
        {
            
            if (File.Exists(arialPath))
            {
                _fontSystem.AddFont(File.ReadAllBytes(arialPath));
                primaryFontLoaded = true;
            }
        }

        if (!primaryFontLoaded)
        {
            throw new FileNotFoundException("Could not find a suitable TTF font file.");
        }

        // Add fallback fonts for extended Unicode support
        AddFallbackFont(fontsFolder, "seguisym.ttf");  // Segoe UI Symbol - various symbols
        AddFallbackFont(fontsFolder, "seguiemj.ttf");  // Segoe UI Emoji - emoji support
        AddFallbackFont(fontsFolder, "segmdl2.ttf");   // Segoe MDL2 Assets - icons
        AddFallbackFont(fontsFolder, "msyh.ttc");      // Microsoft YaHei - CJK characters
        AddFallbackFont(fontsFolder, "malgun.ttf");    // Malgun Gothic - Korean
        AddFallbackFont(fontsFolder, "yugothm.ttc");   // Yu Gothic - Japanese
    }

    private void AddFallbackFont(string fontsFolder, string fontFileName)
    {
        var fontPath = Path.Combine(fontsFolder, fontFileName);
        if (File.Exists(fontPath))
        {
            try
            {
                _fontSystem!.AddFont(File.ReadAllBytes(fontPath));
            }
            catch
            {
                // Ignore errors loading fallback fonts
            }
        }
    }

    /// <summary>
    /// Gets a font at the base size (14pt).
    /// </summary>
    public SpriteFontBase GetFont()
    {
        return _fontSystem!.GetFont(BaseFontSize);
    }

    /// <summary>
    /// Gets a font scaled by the given factor.
    /// </summary>
    public SpriteFontBase GetFont(float scale)
    {
        int size = Math.Max(8, (int)(BaseFontSize * scale));
        return _fontSystem!.GetFont(size);
    }

    /// <summary>
    /// Gets a font at a specific size in pixels.
    /// </summary>
    public SpriteFontBase GetFontAtSize(int size)
    {
        return _fontSystem!.GetFont(Math.Max(8, size));
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
        return _fontSystem!.GetFont(size);
    }

    public void Dispose()
    {
        _fontSystem?.Dispose();
    }
}
