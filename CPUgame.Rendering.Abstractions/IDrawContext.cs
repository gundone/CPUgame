using CPUgame.Core.Primitives;

namespace CPUgame.Rendering.Abstractions;

/// <summary>
/// Platform-agnostic drawing context for rendering primitives.
/// Implementations provide the actual rendering using specific graphics APIs.
/// </summary>
public interface IDrawContext
{
    /// <summary>
    /// Fill a rectangle with a solid color.
    /// </summary>
    void FillRect(Rect rect, ColorRgba color);

    /// <summary>
    /// Draw a rectangle outline.
    /// </summary>
    void DrawRect(Rect rect, ColorRgba color, int thickness = 1);

    /// <summary>
    /// Draw a line between two points.
    /// </summary>
    void DrawLine(Vector2f start, Vector2f end, ColorRgba color, int thickness = 2);

    /// <summary>
    /// Fill a circle with a solid color.
    /// </summary>
    void FillCircle(Vector2f center, float radius, ColorRgba color);

    /// <summary>
    /// Draw a circle outline.
    /// </summary>
    void DrawCircle(Vector2f center, float radius, ColorRgba color, int thickness = 1);

    /// <summary>
    /// Draw text at a position.
    /// </summary>
    void DrawText(string text, Vector2f position, ColorRgba color, float scale = 1f);

    /// <summary>
    /// Draw text centered at a position.
    /// </summary>
    void DrawTextCentered(string text, Vector2f center, ColorRgba color, float scale = 1f);

    /// <summary>
    /// Measure the size of text when rendered.
    /// </summary>
    Vector2f MeasureText(string text, float scale = 1f);
}
