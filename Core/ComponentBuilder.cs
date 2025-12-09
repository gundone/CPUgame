using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Serialization;
using CPUgame.Core.Services;

namespace CPUgame.Core;

/// <summary>
/// Handles building and managing custom components
/// </summary>
public class ComponentBuilder
{
    private readonly Dictionary<string, CircuitData> _customComponents;
    private readonly IPlatformServices _platformServices;

    public event Action<string>? OnComponentCreated;
    public event Action<string>? OnComponentDeleted;
    public event Action<string>? OnError;

    public ComponentBuilder(Dictionary<string, CircuitData> customComponents, IPlatformServices platformServices)
    {
        _customComponents = customComponents;
        _platformServices = platformServices;
    }

    public IReadOnlyDictionary<string, CircuitData> CustomComponents => _customComponents;

    /// <summary>
    /// Load all custom components from storage
    /// </summary>
    public void LoadCustomComponents()
    {
        var folder = _platformServices.GetComponentsFolder();
        if (!_platformServices.DirectoryExists(folder)) return;

        foreach (var file in _platformServices.GetFiles(folder, "*.json"))
        {
            try
            {
                var data = CircuitSerializer.LoadCustomComponentData(file);
                if (data.IsCustomComponent)
                {
                    _customComponents[data.Name] = data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load component {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Validate that selection can be built into a component
    /// </summary>
    public bool ValidateSelection(List<Component> selected, out string? error)
    {
        if (selected.Count == 0)
        {
            error = "status.select_first";
            return false;
        }

        int inputs = selected.Count(c => c is BusInput);
        int outputs = selected.Count(c => c is BusOutput);

        if (inputs == 0 || outputs == 0)
        {
            error = "status.need_inputs_outputs";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Validate component name
    /// </summary>
    public bool ValidateName(string name, out string? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "status.name_empty";
            return false;
        }

        if (_customComponents.ContainsKey(name))
        {
            error = "status.name_exists";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Build a custom component from selected components
    /// </summary>
    public bool BuildComponent(string name, List<Component> selected, int gridSize)
    {
        // Create a new circuit with only selected components
        var subCircuit = new Circuit.Circuit { Name = name };
        var componentMap = new Dictionary<Component, Component>();

        // Clone components
        foreach (var comp in selected)
        {
            Component? clone = CloneComponent(comp, gridSize);

            if (clone != null)
            {
                subCircuit.AddComponent(clone);
                componentMap[comp] = clone;
            }
        }

        // Clone internal connections
        foreach (var comp in selected)
        {
            if (!componentMap.TryGetValue(comp, out var cloneComp)) continue;

            for (int i = 0; i < comp.Inputs.Count; i++)
            {
                var input = comp.Inputs[i];
                if (input.ConnectedTo != null &&
                    componentMap.TryGetValue(input.ConnectedTo.Owner, out var fromClone))
                {
                    var fromPinIndex = input.ConnectedTo.Owner.Outputs.IndexOf(input.ConnectedTo);
                    if (fromPinIndex >= 0 && fromPinIndex < fromClone.Outputs.Count)
                    {
                        fromClone.Outputs[fromPinIndex].Connect(cloneComp.Inputs[i]);
                    }
                }
            }
        }

        // Save as custom component
        var filePath = _platformServices.CombinePath(_platformServices.GetComponentsFolder(), $"{name}.json");
        try
        {
            CircuitSerializer.SaveCustomComponent(subCircuit, name, filePath);
            var data = CircuitSerializer.LoadCustomComponentData(filePath);
            _customComponents[name] = data;
            OnComponentCreated?.Invoke(name);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Delete a custom component
    /// </summary>
    public bool DeleteComponent(string name)
    {
        _customComponents.Remove(name);

        var filePath = _platformServices.CombinePath(_platformServices.GetComponentsFolder(), $"{name}.json");
        if (_platformServices.FileExists(filePath))
        {
            try
            {
                _platformServices.DeleteFile(filePath);
                OnComponentDeleted?.Invoke(name);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        OnComponentDeleted?.Invoke(name);
        return true;
    }

    /// <summary>
    /// Create an instance of a custom component
    /// </summary>
    public CustomComponent? CreateInstance(string name, int x, int y)
    {
        if (_customComponents.TryGetValue(name, out var data))
        {
            var internalCircuit = CircuitSerializer.DeserializeCircuit(data, _customComponents);
            return new CustomComponent(x, y, name, internalCircuit);
        }
        return null;
    }

    private Component? CloneComponent(Component comp, int gridSize)
    {
        return comp switch
        {
            NandGate => new NandGate(comp.X, comp.Y),
            InputSwitch sw => new InputSwitch(comp.X, comp.Y, sw.IsOn),
            OutputLed => new OutputLed(comp.X, comp.Y),
            Clock clk => new Clock(comp.X, comp.Y) { Frequency = clk.Frequency },
            BusInput busIn => new BusInput(comp.X, comp.Y, busIn.BitCount, gridSize) { Value = busIn.Value },
            BusOutput busOut => new BusOutput(comp.X, comp.Y, busOut.BitCount, gridSize),
            CustomComponent custom => CreateInstance(custom.ComponentName, comp.X, comp.Y),
            _ => null
        };
    }
}
