using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Interface for the main designer mode controller.
/// </summary>
public interface IDesignerMode
{
    bool IsActive { get; }

    void SetClipboardGetter(Func<string?> getter);
    void Activate();
    void Deactivate();
    void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased,
        bool rightMouseJustPressed, int scrollDelta, int screenWidth, int screenHeight, double deltaTime);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight, Point mousePos);
    void HandleTextInput(char character);
    void HandleKeyPress(bool backspace, bool enter, bool escape);
    void HandlePaste(string text);

    event Action? OnAppearanceSaved;
    event Action? OnCloseRequested;
}
