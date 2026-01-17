using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Designer;
using CPUgame.Core.Services;
using CPUgame.UI;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

public interface IToolboxManager
{
    Toolbox MainToolbox { get; }
    Toolbox UserToolbox { get; }
    bool IsInteracting { get; }
    void Initialize(int screenWidth, IComponentBuilder componentBuilder, IAppearanceService appearanceService);
    void LoadCustomComponents(IEnumerable<string> componentNames);
    void SetLevelModeFilter(bool isLevelMode, IEnumerable<string>? unlockedComponents);
    void Update(Point mousePos, bool primaryPressed, bool primaryJustPressed, bool primaryJustReleased);
    Component? HandleDrops(Point mousePos, Point worldMousePos, Circuit.Circuit circuit, int gridSize, bool showPinValues, bool primaryJustReleased, IStatusService statusService, IComponentBuilder componentBuilder);
    void ClearDragState();
    bool ContainsPoint(Point pos);
}