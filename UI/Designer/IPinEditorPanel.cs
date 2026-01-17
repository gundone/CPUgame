using System;
using CPUgame.Core.Designer;
using CPUgame.Rendering;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Interface for the pin editor panel in the designer.
/// </summary>
public interface IPinEditorPanel
{
    bool IsEditingPinName { get; }

    void SetAppearance(ComponentAppearance? appearance);
    void SetSelectedPinIndex(int index);
    void Update(Point mousePos, bool mouseJustPressed, Rectangle bounds, IFontService fontService);
    void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, Rectangle bounds);
    void HandleTextInput(char character);
    void HandleKeyPress(bool backspace, bool enter, bool escape);
    void HandlePaste(string text);
    void Reset();

    event Action? OnPinNameChanged;
}
