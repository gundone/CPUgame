using CPUgame.Components;
using CPUgame.UI;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

public interface IToolboxManager
{
    Toolbox MainToolbox { get; }
    Toolbox UserToolbox { get; }
    bool IsInteracting { get; }
    void Initialize(int screenWidth, ComponentBuilder componentBuilder);
    void LoadCustomComponents(System.Collections.Generic.IEnumerable<string> componentNames);
    void Update(Point mousePos, bool primaryPressed, bool primaryJustPressed, bool primaryJustReleased);
    Component? HandleDrops(Point mousePos, Point worldMousePos, Circuit circuit, int gridSize, bool showPinValues, bool primaryJustReleased, IStatusService statusService, ComponentBuilder componentBuilder);
    void ClearDragState();
    bool ContainsPoint(Point pos);
}

public class ToolboxManager : IToolboxManager
{
    public Toolbox MainToolbox { get; private set; } = null!;
    public Toolbox UserToolbox { get; private set; } = null!;

    public bool IsInteracting => MainToolbox.IsDraggingItem || MainToolbox.IsDraggingWindow ||
                                  UserToolbox.IsDraggingItem || UserToolbox.IsDraggingWindow;

    public void Initialize(int screenWidth, ComponentBuilder componentBuilder)
    {
        MainToolbox = new Toolbox(screenWidth - 200, 60);
        UserToolbox = new Toolbox(screenWidth - 200, 300, isUserComponents: true);
        UserToolbox.OnDeleteComponent += name => componentBuilder.DeleteComponent(name);
    }

    public void LoadCustomComponents(System.Collections.Generic.IEnumerable<string> componentNames)
    {
        foreach (var name in componentNames)
        {
            UserToolbox.AddCustomComponent(name);
        }
    }

    public void Update(Point mousePos, bool primaryPressed, bool primaryJustPressed, bool primaryJustReleased)
    {
        MainToolbox.Update(mousePos, primaryPressed, primaryJustPressed, primaryJustReleased);
        UserToolbox.Update(mousePos, primaryPressed, primaryJustPressed, primaryJustReleased);
    }

    public Component? HandleDrops(Point mousePos, Point worldMousePos, Circuit circuit, int gridSize, bool showPinValues, bool primaryJustReleased, IStatusService statusService, ComponentBuilder componentBuilder)
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

    private Component? PlaceFromMainToolbox(Point worldMousePos, Circuit circuit, int gridSize, bool showPinValues, IStatusService statusService)
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

    private Component? PlaceFromUserToolbox(Point worldMousePos, Circuit circuit, int gridSize, IStatusService statusService, ComponentBuilder componentBuilder)
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
