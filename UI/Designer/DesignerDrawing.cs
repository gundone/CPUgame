using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI.Designer;

/// <summary>
/// Static class containing common drawing utilities for the designer UI.
/// </summary>
public static class DesignerDrawing
{
    public static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public static void DrawTextField(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font,
        Rectangle rect, string text, bool isEditing)
    {
        spriteBatch.Draw(pixel, rect, DesignerColors.InputFieldColor);
        DrawBorder(spriteBatch, pixel, rect, isEditing ? DesignerColors.SelectedColor : DesignerColors.BorderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + 4, rect.Y + (rect.Height - textSize.Y) / 2), DesignerColors.TextColor);

        if (isEditing)
        {
            // Draw cursor
            int cursorX = rect.X + 4 + (int)textSize.X;
            spriteBatch.Draw(pixel, new Rectangle(cursorX, rect.Y + 4, 2, rect.Height - 8), DesignerColors.TextColor);
        }
    }

    public static void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font,
        Rectangle rect, string text, bool isHovered)
    {
        spriteBatch.Draw(pixel, rect, isHovered ? DesignerColors.ButtonHoverColor : DesignerColors.ButtonColor);
        DrawBorder(spriteBatch, pixel, rect, DesignerColors.BorderColor, 1);

        var textSize = font.MeasureString(text);
        font.DrawText(spriteBatch, text, new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2), DesignerColors.TextColor);
    }
}
