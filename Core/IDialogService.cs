using System;
using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Input;

namespace CPUgame.Core;

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