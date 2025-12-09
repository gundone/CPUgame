using System;
using System.Collections.Generic;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Serialization;

namespace CPUgame.Core;

/// <summary>
/// Interface for building and managing custom components
/// </summary>
public interface IComponentBuilder
{
    IReadOnlyDictionary<string, CircuitData> CustomComponents { get; }

    event Action<string>? OnComponentCreated;
    event Action<string>? OnComponentDeleted;
    event Action<string>? OnError;

    void LoadCustomComponents();
    bool ValidateSelection(List<Component> selected, out string? error);
    bool ValidateName(string name, out string? error);
    bool BuildComponent(string name, List<Component> selected, int gridSize);
    bool DeleteComponent(string name);
    CustomComponent? CreateInstance(string name, int x, int y);
}
