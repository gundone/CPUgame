using System;
using System.Linq;
using CPUgame.Core.Components;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using CPUgame.Core.Selection;
using CPUgame.Core.Services;

namespace CPUgame.Core;

public interface ICommandHandler
{
    bool ShowPinValues { get; }
    event Action? OnBuildComponent;
    void HandleCommands(InputState input, SelectionManager selection, Circuit.Circuit circuit, IWireManager wireManager, int gridSize);
}

public class CommandHandler : ICommandHandler
{
    private readonly ICircuitManager _circuitManager;
    private readonly IStatusService _statusService;
    private readonly ILevelService _levelService;

    public bool ShowPinValues { get; private set; }
    public event Action? OnBuildComponent;

    public CommandHandler(ICircuitManager circuitManager, IStatusService statusService, ILevelService levelService)
    {
        _circuitManager = circuitManager;
        _statusService = statusService;
        _levelService = levelService;
    }

    public void HandleCommands(InputState input, SelectionManager selection, Circuit.Circuit circuit, IWireManager wireManager, int gridSize)
    {
        if (input.DeleteCommand)
        {
            DeleteSelected(selection);
            return;
        }

        if (input.SaveCommand)
        {
            _circuitManager.SaveCircuit();
            return;
        }

        if (input.LoadCommand)
        {
            _circuitManager.LoadCircuit();
            return;
        }

        if (input.BuildCommand)
        {
            OnBuildComponent?.Invoke();
            return;
        }

        if (input.EscapeCommand)
        {
            selection.ClearAll();
            wireManager.Cancel();
            return;
        }

        if (input.TogglePinValuesCommand)
        {
            ShowPinValues = !ShowPinValues;
            foreach (var comp in circuit.Components)
            {
                if (comp is BusInput busInput)
                    busInput.ShowPinValues = ShowPinValues;
                else if (comp is BusOutput busOutput)
                    busOutput.ShowPinValues = ShowPinValues;
            }
            return;
        }

        ApplyComponentCommands(input, selection, circuit, gridSize);
    }

    private void DeleteSelected(SelectionManager selection)
    {
        if (selection.DeleteSelectedWire())
        {
            _statusService.Show(LocalizationManager.Get("status.wire_disconnected"));
            return;
        }

        // Filter out level components that cannot be deleted
        var selectedComponents = selection.GetSelectedComponents();
        int skipped = 0;
        foreach (var component in selectedComponents.ToList())
        {
            if (_levelService.IsLevelComponent(component))
            {
                component.IsSelected = false;
                skipped++;
            }
        }

        int count = selection.DeleteSelectedComponents();
        if (count > 0)
        {
            _statusService.Show(LocalizationManager.Get("status.deleted", count));
        }
        else if (skipped > 0)
        {
            _statusService.Show(LocalizationManager.Get("status.cannot_delete_level_components"));
        }
    }

    private void ApplyComponentCommands(InputState input, SelectionManager selection, Circuit.Circuit circuit, int gridSize)
    {
        var selected = selection.GetSelectedComponents();
        var allInputPins = circuit.Components.SelectMany(c => c.Inputs).ToList();

        foreach (var element in selected)
        {
            element.GridSize = gridSize;

            // Delegate to component-specific command handlers
            // Skip resize commands for level components (they have fixed size)
            bool isLevelComponent = _levelService.IsLevelComponent(element);
            if (element is BusInput busInput)
            {
                if (!isLevelComponent)
                {
                    busInput.HandleCommands(input, allInputPins);
                }
                else
                {
                    // Allow value changes but not resizing for level components
                    busInput.HandleValueCommands(input);
                }
            }
            else if (element is BusOutput busOutput)
            {
                if (!isLevelComponent)
                {
                    busOutput.HandleCommands(input);
                }
            }

            // Movement commands
            if (input.MoveUp) element.Y -= element.GridSize;
            if (input.MoveDown) element.Y += element.GridSize;
            if (input.MoveLeft) element.X -= element.GridSize;
            if (input.MoveRight) element.X += element.GridSize;

            // Toggle command for switches
            if (input.ToggleCommand && element is InputSwitch sw)
                sw.IsOn = !sw.IsOn;
        }
    }
}
