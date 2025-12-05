using System;
using System.Linq;
using CPUgame.Components;
using CPUgame.Input;

namespace CPUgame.Core;

public interface ICommandHandler
{
    bool ShowPinValues { get; }
    event Action? OnBuildComponent;
    void HandleCommands(InputState input, SelectionManager selection, Circuit circuit, IWireManager wireManager, int gridSize);
}

public class CommandHandler : ICommandHandler
{
    private readonly ICircuitManager _circuitManager;
    private readonly IStatusService _statusService;

    public bool ShowPinValues { get; private set; }
    public event Action? OnBuildComponent;

    public CommandHandler(ICircuitManager circuitManager, IStatusService statusService)
    {
        _circuitManager = circuitManager;
        _statusService = statusService;
    }

    public void HandleCommands(InputState input, SelectionManager selection, Circuit circuit, IWireManager wireManager, int gridSize)
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

        int count = selection.DeleteSelectedComponents();
        if (count > 0)
            _statusService.Show(LocalizationManager.Get("status.deleted", count));
    }

    private void ApplyComponentCommands(InputState input, SelectionManager selection, Circuit circuit, int gridSize)
    {
        var selected = selection.GetSelectedComponents();
        var allInputPins = circuit.Components.SelectMany(c => c.Inputs).ToList();

        foreach (var element in selected)
        {
            element.GridSize = gridSize;

            // Delegate to component-specific command handlers
            if (element is BusInput busInput)
            {
                busInput.HandleCommands(input, allInputPins);
            }
            else if (element is BusOutput busOutput)
            {
                busOutput.HandleCommands(input);
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
