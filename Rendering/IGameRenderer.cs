using CPUgame.Core;
using CPUgame.Core.Circuit;
using CPUgame.Core.Primitives;
using CPUgame.Core.Selection;
using CPUgame.Core.Services;
using CPUgame.UI;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

public interface IGameRenderer
{
    int GridSize { get; }
    Texture2D Pixel { get; }
    float TitleFontScale { get; set; }
    void Initialize(GraphicsDevice graphicsDevice, IFontService fontService);
    void DrawWorld(SpriteBatch spriteBatch, Circuit circuit, ICameraController camera, ISelectionManager selection, IWireManager wireManager, IManualWireService manualWireService, Pin? hoveredPin, Point2 mousePos, int screenWidth, int screenHeight, bool isDraggingItem);
    void DrawUI(SpriteBatch spriteBatch, IToolboxManager toolboxManager, MainMenu mainMenu, IStatusService statusService, IDialogService dialogService, ITruthTableService truthTableService, ICameraController camera, Point2 mousePos, int screenWidth, int screenHeight, SpriteFontBase font, bool isMenuVisible = true);
}