using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Localization;
using CPUgame.Core.Services;
using CPUgame.UI;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

public interface IToolboxManager
{
    Toolbox MainToolbox { get; }
    Toolbox UserToolbox { get; }
    bool IsInteracting { get; }
    void Initialize(int screenWidth, IComponentBuilder componentBuilder);
    void LoadCustomComponents(IEnumerable<string> componentNames);
    void SetLevelModeFilter(bool isLevelMode, IEnumerable<string>? unlockedComponents);
    void Update(Point mousePos, bool primaryPressed, bool primaryJustPressed, bool primaryJustReleased);
    Component? HandleDrops(Point mousePos, Point worldMousePos, Circuit.Circuit circuit, int gridSize, bool showPinValues, bool primaryJustReleased, IStatusService statusService, IComponentBuilder componentBuilder);
    void ClearDragState();
    bool ContainsPoint(Point pos);
}

public class ToolboxManager : IToolboxManager
{
    public Toolbox MainToolbox { get; private set; } = null!;
    public Toolbox UserToolbox { get; private set; } = null!;

    private bool _isLevelMode;
    private HashSet<string> _unlockedComponents = new();
    private HashSet<string> _allComponents = new();

    public bool IsInteracting => MainToolbox.IsDraggingItem || MainToolbox.IsDraggingWindow ||
                                  UserToolbox.IsDraggingItem || UserToolbox.IsDraggingWindow;

    public void Initialize(int screenWidth, IComponentBuilder componentBuilder)
    {
        MainToolbox = new Toolbox(screenWidth - 200, 60);
        UserToolbox = new Toolbox(screenWidth - 200, 300, isUserComponents: true);
        UserToolbox.OnDeleteComponent += name => componentBuilder.DeleteComponent(name);
    }

    public void LoadCustomComponents(IEnumerable<string> componentNames)
    {
        _allComponents.Clear();
        foreach (var name in componentNames)
        {
            _allComponents.Add(name);
        }
        RefreshUserToolbox();
    }

    public void SetLevelModeFilter(bool isLevelMode, IEnumerable<string>? unlockedComponents)
    {
        _isLevelMode = isLevelMode;
        _unlockedComponents.Clear();
        if (unlockedComponents != null)
        {
            foreach (var comp in unlockedComponents)
            {
                _unlockedComponents.Add(comp);
            }
        }
        RefreshUserToolbox();
    }

    private void RefreshUserToolbox()
    {
        // Clear all custom components from toolbox
        var currentComponents = UserToolbox.GetCustomComponentNames();
        foreach (var name in currentComponents)
        {
            UserToolbox.RemoveCustomComponent(name);
        }

        // Add back components based on mode
        foreach (var name in _allComponents)
        {
            if (_isLevelMode)
            {
                // In level mode, only show unlocked components
                if (_unlockedComponents.Contains(name))
                {
                    UserToolbox.AddCustomComponent(name);
                }
            }
            else
            {
                // In sandbox mode, show all components
                UserToolbox.AddCustomComponent(name);
            }
        }
    }

    public void Update(Point mousePos, bool primaryPressed, bool primaryJustPressed, bool primaryJustReleased)
    {
        MainToolbox.Update(mousePos, primaryPressed, primaryJustPressed, primaryJustReleased);
        UserToolbox.Update(mousePos, primaryPressed, primaryJustPressed, primaryJustReleased);
    }

    public Component? HandleDrops(Point mousePos, Point worldMousePos, Circuit.Circuit circuit, int gridSize, bool showPinValues, bool primaryJustReleased, IStatusService statusService, IComponentBuilder componentBuilder)
    {
        if (!primaryJustReleased) return null;

        Component? placedComponent = null;

        if (MainToolbox.DraggingTool != null || MainToolbox.DraggingCustomComponent != null)
        {
            if (!MainToolbox.ContainsPoint(mousePos))
            {
                placedComponent = PlaceFromMainToolbox(worldMousePos, circuit, gridSize, showPinValues, statusService);
            }
            MainToolbox.ClearDragState();
        }

        if (UserToolbox.DraggingCustomComponent != null)
        {
            if (!UserToolbox.ContainsPoint(mousePos))
            {
                placedComponent = PlaceFromUserToolbox(worldMousePos, circuit, gridSize, statusService, componentBuilder);
            }
            UserToolbox.ClearDragState();
        }

        return placedComponent;
    }

    private Component? PlaceFromMainToolbox(Point worldMousePos, Circuit.Circuit circuit, int gridSize, bool showPinValues, IStatusService statusService)
    {
        var x = (worldMousePos.X / gridSize) * gridSize;
        var y = (worldMousePos.Y / gridSize) * gridSize;

        Component? newComponent = MainToolbox.DraggingTool switch
        {
            ToolType.PlaceNand => new NandGate(x, y),
            ToolType.PlaceSwitch => new InputSwitch(x, y),
            ToolType.PlaceLed => new OutputLed(x, y),
            ToolType.PlaceClock => new Clock(x, y),
            ToolType.PlaceBusInput => new BusInput(x, y, MainToolbox.BusInputBits, gridSize),
            ToolType.PlaceBusOutput => new BusOutput(x, y, MainToolbox.BusOutputBits, gridSize),
            _ => null
        };

        if (newComponent != null)
        {
            if (newComponent is BusInput busInput)
                busInput.ShowPinValues = showPinValues;
            else if (newComponent is BusOutput busOutput)
                busOutput.ShowPinValues = showPinValues;

            circuit.AddComponent(newComponent);
            statusService.Show(LocalizationManager.Get("status.placed", newComponent.Name));
        }

        return newComponent;
    }

    private Component? PlaceFromUserToolbox(Point worldMousePos, Circuit.Circuit circuit, int gridSize, IStatusService statusService, IComponentBuilder componentBuilder)
    {
        var x = (worldMousePos.X / gridSize) * gridSize;
        var y = (worldMousePos.Y / gridSize) * gridSize;

        var componentName = UserToolbox.DraggingCustomComponent;
        if (componentName != null)
        {
            var newComponent = componentBuilder.CreateInstance(componentName, x, y);
            if (newComponent != null)
            {
                circuit.AddComponent(newComponent);
                statusService.Show(LocalizationManager.Get("status.placed", newComponent.Name));
                return newComponent;
            }
        }

        return null;
    }

    public void ClearDragState()
    {
        MainToolbox.ClearDragState();
        UserToolbox.ClearDragState();
    }

    public bool ContainsPoint(Point pos)
    {
        return MainToolbox.ContainsPoint(pos) || UserToolbox.ContainsPoint(pos);
    }
}
