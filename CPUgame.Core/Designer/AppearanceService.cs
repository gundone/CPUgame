using System.Text.Json;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Services;

namespace CPUgame.Core.Designer;

/// <summary>
/// Manages component visual appearances with JSON persistence.
/// </summary>
public class AppearanceService : IAppearanceService
{
    private readonly IPlatformServices _platformServices;
    private readonly IComponentBuilder _componentBuilder;
    private readonly Dictionary<string, ComponentAppearance> _appearances = new();

    private const string AppearancesFileName = "ComponentAppearances.json";

    public event Action<string>? OnAppearanceChanged;

    public AppearanceService(IPlatformServices platformServices, IComponentBuilder componentBuilder)
    {
        _platformServices = platformServices;
        _componentBuilder = componentBuilder;
    }

    public ComponentAppearance? GetAppearance(string componentType)
    {
        return _appearances.TryGetValue(componentType, out var appearance) ? appearance : null;
    }

    public void SetAppearance(ComponentAppearance appearance)
    {
        _appearances[appearance.ComponentType] = appearance;
        OnAppearanceChanged?.Invoke(appearance.ComponentType);
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

        // Apply input pin positions
        foreach (var pinAppearance in appearance.InputPins)
        {
            var pin = component.Inputs.FirstOrDefault(p => p.Name == pinAppearance.Name);
            if (pin != null)
            {
                pin.LocalX = pinAppearance.LocalX;
                pin.LocalY = pinAppearance.LocalY;
            }
        }

        // Apply output pin positions
        foreach (var pinAppearance in appearance.OutputPins)
        {
            var pin = component.Outputs.FirstOrDefault(p => p.Name == pinAppearance.Name);
            if (pin != null)
            {
                pin.LocalX = pinAppearance.LocalX;
                pin.LocalY = pinAppearance.LocalY;
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

    public void SaveAll()
    {
        try
        {
            var filePath = GetAppearancesFilePath();
            var data = new ComponentAppearanceData { Appearances = _appearances };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save appearances: {ex.Message}");
        }
    }

    public void LoadAll()
    {
        var filePath = GetAppearancesFilePath();

        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var data = JsonSerializer.Deserialize<ComponentAppearanceData>(json, options);
            if (data?.Appearances != null)
            {
                _appearances.Clear();
                foreach (var kvp in data.Appearances)
                {
                    _appearances[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load appearances: {ex.Message}");
        }
    }

    private string GetAppearancesFilePath()
    {
        var folder = _platformServices.GetComponentsFolder();
        return Path.Combine(Path.GetDirectoryName(folder) ?? folder, AppearancesFileName);
    }
}
