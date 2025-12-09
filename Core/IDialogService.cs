using System;
using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using CPUgame.Core.Services;

namespace CPUgame.Core;

public enum DialogMode
{
    None,
    NamingComponent,
    EditingTitle
}

public interface IDialogService
{
    bool IsActive { get; }
    DialogMode Mode { get; }
    string InputText { get; }
    string DialogTitle { get; }
    void StartNaming(List<Component> selectedComponents);
    void StartEditingTitle(Component component);
    void HandleInput(InputState input, IInputHandler inputHandler);
    event Action<string, List<Component>>? OnNameConfirmed;
    event Action<Component, string>? OnTitleConfirmed;
    event Action? OnCancelled;
}

public class DialogService : IDialogService
{
    private readonly IStatusService _statusService;
    private readonly IComponentBuilder _componentBuilder;
    private List<Component>? _pendingSelection;
    private Component? _editingComponent;

    public bool IsActive => Mode != DialogMode.None;
    public DialogMode Mode { get; private set; } = DialogMode.None;
    public string InputText { get; private set; } = "";
    public string DialogTitle { get; private set; } = "";

    public event Action<string, List<Component>>? OnNameConfirmed;
    public event Action<Component, string>? OnTitleConfirmed;
    public event Action? OnCancelled;

    public DialogService(IStatusService statusService, IComponentBuilder componentBuilder)
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
        InputText = "";
        DialogTitle = LocalizationManager.Get("dialog.name_component");
        Mode = DialogMode.NamingComponent;
    }

    public void StartEditingTitle(Component component)
    {
        _editingComponent = component;
        InputText = component.Title;
        DialogTitle = LocalizationManager.Get("dialog.edit_title");
        Mode = DialogMode.EditingTitle;
    }

    public void HandleInput(InputState input, IInputHandler inputHandler)
    {
        if (!IsActive)
        {
            return;
        }

        if (input.EscapeCommand)
        {
            Cancel(inputHandler);
            return;
        }

        if (input.EnterPressed)
        {
            Confirm(inputHandler);
            return;
        }

        if (input.BackspacePressed && InputText.Length > 0)
        {
            InputText = InputText[..^1];
            return;
        }

        if (input.CharacterInput.HasValue && InputText.Length < 30)
        {
            InputText += input.CharacterInput.Value;
        }
    }

    private void Cancel(IInputHandler inputHandler)
    {
        Mode = DialogMode.None;
        _pendingSelection = null;
        _editingComponent = null;
        InputText = "";
        inputHandler.EndTextInput();
        _statusService.Show(LocalizationManager.Get("status.cancelled"));
        OnCancelled?.Invoke();
    }

    private void Confirm(IInputHandler inputHandler)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        if (Mode == DialogMode.NamingComponent)
        {
            if (!_componentBuilder.ValidateName(InputText, out var error))
            {
                _statusService.Show(LocalizationManager.Get(error!));
                return;
            }

            Mode = DialogMode.None;
            inputHandler.EndTextInput();

            if (_pendingSelection != null)
            {
                OnNameConfirmed?.Invoke(InputText, _pendingSelection);
            }

            _pendingSelection = null;
        }
        else if (Mode == DialogMode.EditingTitle)
        {
            Mode = DialogMode.None;
            inputHandler.EndTextInput();

            if (_editingComponent != null)
            {
                OnTitleConfirmed?.Invoke(_editingComponent, InputText);
            }

            _editingComponent = null;
        }

        InputText = "";
    }
}
