using System;
using System.Collections.Generic;
using CPUgame.Input;

namespace CPUgame.Core;

public interface IDialogService
{
    bool IsNamingComponent { get; }
    string ComponentNameInput { get; }
    void StartNaming(List<Component> selectedComponents);
    void HandleInput(InputState input, IInputHandler inputHandler);
    event Action<string, List<Component>>? OnNameConfirmed;
    event Action? OnCancelled;
}

public class DialogService : IDialogService
{
    private readonly IStatusService _statusService;
    private readonly ComponentBuilder _componentBuilder;
    private List<Component>? _pendingSelection;

    public bool IsNamingComponent { get; private set; }
    public string ComponentNameInput { get; private set; } = "";

    public event Action<string, List<Component>>? OnNameConfirmed;
    public event Action? OnCancelled;

    public DialogService(IStatusService statusService, ComponentBuilder componentBuilder)
    {
        _statusService = statusService;
        _componentBuilder = componentBuilder;
    }

    public void StartNaming(List<Component> selectedComponents)
    {
        if (!_componentBuilder.ValidateSelection(selectedComponents, out var error))
        {
            _statusService.Show(LocalizationManager.Get(error!));
            return;
        }

        _pendingSelection = selectedComponents;
        ComponentNameInput = "";
        IsNamingComponent = true;
    }

    public void HandleInput(InputState input, IInputHandler inputHandler)
    {
        if (!IsNamingComponent) return;

        if (input.EscapeCommand)
        {
            IsNamingComponent = false;
            _pendingSelection = null;
            ComponentNameInput = "";
            inputHandler.EndTextInput();
            _statusService.Show(LocalizationManager.Get("status.cancelled"));
            OnCancelled?.Invoke();
            return;
        }

        if (input.EnterPressed)
        {
            if (!string.IsNullOrWhiteSpace(ComponentNameInput))
            {
                if (!_componentBuilder.ValidateName(ComponentNameInput, out var error))
                {
                    _statusService.Show(LocalizationManager.Get(error!));
                    return;
                }

                IsNamingComponent = false;
                inputHandler.EndTextInput();

                if (_pendingSelection != null)
                {
                    OnNameConfirmed?.Invoke(ComponentNameInput, _pendingSelection);
                }

                _pendingSelection = null;
                ComponentNameInput = "";
            }
            return;
        }

        if (input.BackspacePressed && ComponentNameInput.Length > 0)
        {
            ComponentNameInput = ComponentNameInput[..^1];
            return;
        }

        if (input.CharacterInput.HasValue && ComponentNameInput.Length < 20)
        {
            ComponentNameInput += input.CharacterInput.Value;
        }
    }
}
