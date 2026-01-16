using System.Globalization;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Designer;
using CPUgame.Core.Serialization;

namespace CPUgame.Core.Services;

/// <summary>
/// Handles building and managing custom components
/// </summary>
public class ComponentBuilder : IComponentBuilder
{
    private readonly ICircuitManager _circuitManager;
    private readonly IPlatformServices _platformServices;
    private readonly IProfileService _profileService;

    public event Action<string>? OnComponentCreated;
    public event Action<string>? OnComponentDeleted;
    public event Action<string>? OnError;

    public ComponentBuilder(ICircuitManager circuitManager, IPlatformServices platformServices, IProfileService profileService)
    {
        _circuitManager = circuitManager;
        _platformServices = platformServices;
        _profileService = profileService;
    }

    public IReadOnlyDictionary<string, CircuitData> CustomComponents => _circuitManager.CustomComponents;

    /// <summary>
    /// Load all custom components from storage
    /// </summary>
    public void LoadCustomComponents()
    {
        // Clear existing components
        _circuitManager.CustomComponents.Clear();

        // Always load from global folder first
        string globalFolder = _platformServices.GetComponentsFolder();
        LoadComponentsFromFolder(globalFolder);

        // If profile is loaded, also load from profile-specific folder
        if (_profileService.HasProfile)
        {
            try
            {
                string profileFolder = _profileService.GetProfileComponentsFolder();
                LoadComponentsFromFolder(profileFolder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load profile components: {ex.Message}");
            }
        }
    }

    private void LoadComponentsFromFolder(string folder)
    {
        if (!_platformServices.DirectoryExists(folder))
        {
            return;
        }

        foreach (var file in _platformServices.GetFiles(folder, "*.json"))
        {
            try
            {
                var data = CircuitSerializer.LoadCustomComponentData(file);
                if (data.IsCustomComponent)
                {
                    _circuitManager.CustomComponents[data.Name] = data;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Skipped {file}: Not marked as custom component");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load component {file}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                OnError?.Invoke($"Failed to load component from {Path.GetFileName(file)}: {ex.Message}");
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

        if (_circuitManager.CustomComponents.ContainsKey(name))
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
    public bool BuildComponent(string name, List<Component> selected, int gridSize, Designer.ComponentAppearance? appearance = null)
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

        // Clone internal connections and wire paths
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

                        // Clone manual wire path with intermediate nodes
                        if (input.ManualWirePath != null && input.ManualWirePath.Count > 0)
                        {
                            cloneComp.Inputs[i].ManualWirePath = new List<Primitives.Point2>(input.ManualWirePath);
                        }
                    }
                }
            }
        }

        // Save as custom component in profile folder (or global folder if no profile)
        string folder;
        if (_profileService.HasProfile)
        {
            try
            {
                folder = _profileService.GetProfileComponentsFolder();
            }
            catch
            {
                folder = _platformServices.GetComponentsFolder();
            }
        }
        else
        {
            // Use global folder when no profile is loaded (for sandbox mode)
            folder = _platformServices.GetComponentsFolder();
        }

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        // Generate default appearance if not provided
        if (appearance == null)
        {
            appearance = GenerateDefaultAppearance(name, subCircuit);
        }

        var filePath = Path.Combine(folder, $"{name}.json");
        try
        {
            CircuitSerializer.SaveCustomComponent(subCircuit, name, filePath, appearance);
            var data = CircuitSerializer.LoadCustomComponentData(filePath);
            _circuitManager.CustomComponents[name] = data;
            OnComponentCreated?.Invoke(name);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
            return false;
        }
    }

    private ComponentAppearance GenerateDefaultAppearance(string name, Circuit.Circuit circuit)
    {
        var appearance = new ComponentAppearance
        {
            ComponentType = $"Custom:{name}",
            Title = name.ToUpper(CultureInfo.InvariantCulture),
            TitlePosition = TitlePosition.Center
        };

        // Find BusInput and BusOutput components to determine pins
        var busInputs = circuit.Components.OfType<BusInput>().OrderBy(b => b.X).ThenBy(b => b.Y).ToList();
        var busOutputs = circuit.Components.OfType<BusOutput>().OrderBy(b => b.X).ThenBy(b => b.Y).ToList();

        // Calculate total number of input and output pins (counting all bits)
        int totalInputPins = busInputs.Sum(b => b.BitCount);
        int totalOutputPins = busOutputs.Sum(b => b.BitCount);

        // Component size based on requirements:
        // Width: 60 (3 grid cells) for components with >=3 input or output pins, otherwise 2
        // Height: 20 * (Max(inputPinsCount, outputPinsCount) + 1)
        int width = totalOutputPins < 3 && totalInputPins < 3 ? 40 : 60;
        int heightPerPin = 20;
        int maxPins = Math.Max(totalInputPins, totalOutputPins);
        int height = heightPerPin * (maxPins + 1);

        appearance.Width = width;
        appearance.Height = height;

        // Add input pins (from BusInput components)
        // Pin titles must be named as they were named in the level
        int inputPinIndex = 0;
        // Center pins vertically: for N pins, we have (N-1) spacings between them
        int inputY = totalInputPins % 2 == 0 
                ? (height - (totalInputPins - 1) * heightPerPin) / 2
                : heightPerPin;
        foreach (var busInput in busInputs)
        {
            for (int bitIdx = 0; bitIdx < busInput.BitCount; bitIdx++)
            {
                // Use the actual pin title from the BusInput's output pin
                var pin = busInput.Outputs[bitIdx];
                var pinName = !string.IsNullOrEmpty(pin.Title) ? pin.Title : $"In{inputPinIndex}";
                appearance.InputPins.Add(new PinAppearance(pinName, 0, inputY));
                inputY += heightPerPin;
                inputPinIndex++;
            }
        }

        // Add output pins (from BusOutput components)
        // Pin titles must be named as they were named in the level
        int outputPinIndex = 0;
        // Center pins vertically: for N(even) pins, we have (N-1) spacings between them
        int outputY = totalOutputPins % 2 == 0
            ? (height - (totalOutputPins - 1) * heightPerPin) / 2
            : heightPerPin; 
        foreach (var busOutput in busOutputs)
        {
            for (int bitIdx = 0; bitIdx < busOutput.BitCount; bitIdx++)
            {
                // Use the actual pin title from the BusOutput's input pin
                var pin = busOutput.Inputs[bitIdx];
                var pinName = !string.IsNullOrEmpty(pin.Title) ? pin.Title : $"Out{outputPinIndex}";
                appearance.OutputPins.Add(new PinAppearance(pinName, width, outputY));
                outputY += heightPerPin;
                outputPinIndex++;
            }
        }

        return appearance;
    }

    /// <summary>
    /// Delete a custom component
    /// </summary>
    public bool DeleteComponent(string name)
    {
        _circuitManager.CustomComponents.Remove(name);

        // Delete from profile folder (or global folder if no profile)
        string folder;
        if (_profileService.HasProfile)
        {
            try
            {
                folder = _profileService.GetProfileComponentsFolder();
            }
            catch
            {
                folder = _platformServices.GetComponentsFolder();
            }
        }
        else
        {
            folder = _platformServices.GetComponentsFolder();
        }

        var filePath = Path.Combine(folder, $"{name}.json");
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
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
        if (_circuitManager.CustomComponents.TryGetValue(name, out var data))
        {
            var internalCircuit = CircuitSerializer.DeserializeCircuit(data, _circuitManager.CustomComponents);
            return new CustomComponent(x, y, name, internalCircuit, data.Appearance);
        }
        return null;
    }

    private Component? CloneComponent(Component comp, int gridSize)
    {
        // CustomComponent needs special handling
        if (comp is CustomComponent custom)
        {
            return CreateInstance(custom.ComponentName, comp.X, comp.Y);
        }

        // All other components use their Clone method
        return comp.Clone(gridSize);
    }
}
