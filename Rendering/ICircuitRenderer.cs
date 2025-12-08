using System.Collections.Generic;
using CPUgame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// Interface for rendering circuit components and wires.
/// </summary>
public interface ICircuitRenderer
{
    int GridSize { get; set; }
    Texture2D Pixel { get; }

    void Initialize(GraphicsDevice graphicsDevice, SpriteFont font);
    void DrawGrid(SpriteBatch spriteBatch, float cameraX, float cameraY, int screenWidth, int screenHeight, float zoom);
    void DrawCircuit(SpriteBatch spriteBatch, Circuit circuit, Pin? selectedWire = null);
    void DrawWire(SpriteBatch spriteBatch, Pin from, Pin to, bool isSelected = false);
    void DrawWirePreview(SpriteBatch spriteBatch, Vector2 start, Vector2 end);
    void DrawManualWirePreview(SpriteBatch spriteBatch, IReadOnlyList<Point> pathPoints, Vector2 currentMousePos);
    void DrawComponent(SpriteBatch spriteBatch, Component component);
    void DrawPinHighlight(SpriteBatch spriteBatch, Pin pin);

    /// <summary>
    /// Draw editable nodes for a selected manual wire.
    /// </summary>
    void DrawManualWireNodes(SpriteBatch spriteBatch, Pin inputPin, int draggingNodeIndex = -1);
}
