using CPUgame.Core.Primitives;

namespace CPUgame.Rendering.Abstractions;

/// <summary>
/// Factory for creating draw contexts with different coordinate systems.
/// </summary>
public interface IDrawContextFactory
{
    /// <summary>
    /// Create a draw context for world-space rendering (affected by camera transform).
    /// </summary>
    IDrawContext CreateWorldContext(Transform2D cameraTransform);

    /// <summary>
    /// Create a draw context for screen-space rendering (UI, overlays).
    /// </summary>
    IDrawContext CreateScreenContext();

    /// <summary>
    /// Current screen width in pixels.
    /// </summary>
    int ScreenWidth { get; }

    /// <summary>
    /// Current screen height in pixels.
    /// </summary>
    int ScreenHeight { get; }
}
