using System;
using CPUgame.Core.Designer;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Interface for the properties panel in the designer.
/// </summary>
public interface IPropertiesPanel
{
    bool IsEditing { get; }

    void SetAppearance(ComponentAppearance? appearance);
    void Update(Point mousePos, bool mouseJustPressed, bool rightMouseJustPressed, Rectangle bounds, int screenWidth, int screenHeight);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle bounds);
    void HandleTextInput(char character);
    void HandleKeyPress(bool backspace, bool enter, bool escape);
    void HandlePaste(string text);
    void ShowContextMenu(Point mousePos, int screenWidth, int screenHeight);
    void Reset();

    event Action? OnAppearanceChanged;
    event Action<Rectangle>? OnContextMenuRequested;
}
