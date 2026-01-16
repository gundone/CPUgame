using System.Text.Json;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Serialization;
using CPUgame.Core.Services;

namespace CPUgame.Core.Designer;

/// <summary>
/// Manages component visual appearances with JSON persistence.
/// </summary>
public class AppearanceService : IAppearanceService
{
    private readonly IPlatformServices _platformServices;
    private readonly IComponentBuilder _componentBuilder;
    private readonly IProfileService _profileService;
    private readonly ICircuitManager _circuitManager;
    private readonly Dictionary<string, ComponentAppearance> _appearances = new();

    public event Action<string>? OnAppearanceChanged;

    public AppearanceService(IPlatformServices platformServices, IComponentBuilder componentBuilder, IProfileService profileService, ICircuitManager circuitManager)
    {
        _platformServices = platformServices;
        _componentBuilder = componentBuilder;
        _profileService = profileService;
        _circuitManager = circuitManager;
    }

    public ComponentAppearance? GetAppearance(string componentType)
    {
        // Check in-memory cache first (for built-in components)
        if (_appearances.TryGetValue(componentType, out var appearance))
        {
            return appearance;
        }

        // For custom components, check if appearance is stored in CircuitData
        if (componentType.StartsWith("Custom:"))
        {
            var customName = componentType.Substring(7);
            if (_componentBuilder.CustomComponents.TryGetValue(customName, out var circuitData))
            {
                return circuitData.Appearance;
            }
        }

        return null;
    }

    public void SetAppearance(ComponentAppearance appearance)
    {
        _appearances[appearance.ComponentType] = appearance;
        OnAppearanceChanged?.Invoke(appearance.ComponentType);

        // For custom components, also update the JSON file
        if (appearance.ComponentType.StartsWith("Custom:"))
        {
            var customName = appearance.ComponentType.Substring(7);
            UpdateCustomComponentAppearance(customName, appearance);
        }
    }

    public void ResetAppearance(string componentType)
    {
        if (_appearances.Remove(componentType))
        {
            OnAppearanceChanged?.Invoke(componentType);
        }
    }

    public bool HasCustomAppearance(string componentType)
    {
        return _appearances.ContainsKey(componentType);
    }

    public ComponentAppearance GetDefaultAppearance(string componentType)
    {
        var appearance = new ComponentAppearance { ComponentType = componentType };

        switch (componentType)
        {
            case "NAND":
                appearance.Width = 40;
                appearance.Height = 60;
                appearance.TitlePosition = TitlePosition.Center;
                appearance.InputPins.Add(new PinAppearance("A", 0, 20));
                appearance.InputPins.Add(new PinAppearance("B", 0, 40));
                appearance.OutputPins.Add(new PinAppearance("Out", 40, 40));
                break;

            case "Switch":
                appearance.Width = 40;
                appearance.Height = 40;
                appearance.TitlePosition = TitlePosition.Center;
                appearance.OutputPins.Add(new PinAppearance("Out", 40, 20));
                break;

            case "LED":
                appearance.Width = 40;
                appearance.Height = 40;
                appearance.TitlePosition = TitlePosition.Center;
                appearance.InputPins.Add(new PinAppearance("In", 0, 20));
                break;

            case "Clock":
                appearance.Width = 50;
                appearance.Height = 40;
                appearance.TitlePosition = TitlePosition.Center;
                appearance.OutputPins.Add(new PinAppearance("Out", 50, 20));
                break;

            default:
                // For custom components, create a temporary instance to get pin info
                if (componentType.StartsWith("Custom:"))
                {
                    var customName = componentType.Substring(7);
                    var tempComponent = _componentBuilder.CreateInstance(customName, 0, 0);
                    if (tempComponent != null)
                    {
                        appearance.Width = tempComponent.Width;
                        appearance.Height = tempComponent.Height;
                        appearance.Title = customName;

                        foreach (var pin in tempComponent.Inputs)
                        {
                            appearance.InputPins.Add(new PinAppearance(pin.Name, pin.LocalX, pin.LocalY));
                        }
                        foreach (var pin in tempComponent.Outputs)
                        {
                            appearance.OutputPins.Add(new PinAppearance(pin.Name, pin.LocalX, pin.LocalY));
                        }
                    }
                }
                else
                {
                    appearance.Width = 60;
                    appearance.Height = 100;
                }
                appearance.TitlePosition = TitlePosition.Center;
                break;
        }

        return appearance;
    }

    public void ApplyAppearance(Component component)
    {
        var componentType = GetComponentType(component);
        var appearance = GetAppearance(componentType);

        if (appearance == null)
        {
            return;
        }

        component.Width = appearance.Width;
        component.Height = appearance.Height;
        component.TitlePosition = appearance.TitlePosition;
        component.TitleOffsetX = appearance.TitleOffsetX;
        component.TitleOffsetY = appearance.TitleOffsetY;

        // Apply input pin positions and titles
        foreach (var pinAppearance in appearance.InputPins)
        {
            var pin = component.Inputs.FirstOrDefault(p => p.Name == pinAppearance.Name);
            if (pin != null)
            {
                pin.LocalX = pinAppearance.LocalX;
                pin.LocalY = pinAppearance.LocalY;
                pin.Title = pinAppearance.Name;
            }
        }

        // Apply output pin positions and titles
        foreach (var pinAppearance in appearance.OutputPins)
        {
            var pin = component.Outputs.FirstOrDefault(p => p.Name == pinAppearance.Name);
            if (pin != null)
            {
                pin.LocalX = pinAppearance.LocalX;
                pin.LocalY = pinAppearance.LocalY;
                pin.Title = pinAppearance.Name;
            }
        }
    }

    public string GetComponentType(Component component)
    {
        return component switch
        {
            NandGate => "NAND",
            InputSwitch => "Switch",
            OutputLed => "LED",
            Clock => "Clock",
            BusInput => "BusInput",
            BusOutput => "BusOutput",
            CustomComponent custom => $"Custom:{custom.ComponentName}",
            _ => component.GetType().Name
        };
    }

    public IEnumerable<string> GetAllComponentTypes()
    {
        // Built-in types
        yield return "NAND";
        yield return "Switch";
        yield return "LED";
        yield return "Clock";
        yield return "BusInput";
        yield return "BusOutput";

        // Custom components
        foreach (var name in _componentBuilder.CustomComponents.Keys)
        {
            yield return $"Custom:{name}";
        }
    }


    public bool UpdateCustomComponentAppearance(string componentName, ComponentAppearance appearance)
    {
        // Check both possible locations for the component file
        var foldersToCheck = new List<string>();

        // Add profile folder if available
        if (_profileService.HasProfile)
        {
            try
            {
                foldersToCheck.Add(_profileService.GetProfileComponentsFolder());
            }
            catch
            {
                // Profile folder not accessible
            }
        }

        // Always check global folder
        foldersToCheck.Add(_platformServices.GetComponentsFolder());

        // Find and update the component file in whichever folder it exists
        foreach (var folder in foldersToCheck)
        {
            var filePath = Path.Combine(folder, $"{componentName}.json");
            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                // Load existing component data
                var circuitData = CircuitSerializer.LoadCustomComponentData(filePath);

                // Update appearance
                circuitData.Appearance = appearance;

                // Save back to file
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(circuitData, options);
                File.WriteAllText(filePath, json);

                // Update in-memory cache in CircuitManager
                if (_circuitManager.CustomComponents.ContainsKey(componentName))
                {
                    _circuitManager.CustomComponents[componentName] = circuitData;
                }

                OnAppearanceChanged?.Invoke($"Custom:{componentName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update component appearance in {folder}: {ex.Message}");
                // Continue to try next folder
            }
        }

        return false;
    }
}
