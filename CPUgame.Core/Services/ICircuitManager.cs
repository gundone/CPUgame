using CPUgame.Core.Localization;
using CPUgame.Core.Serialization;

namespace CPUgame.Core.Services;

public interface ICircuitManager
{
    Circuit.Circuit Circuit { get; }
    Dictionary<string, CircuitData> CustomComponents { get; }
    event Action? OnCircuitChanged;
    void NewCircuit();
    void SaveCircuit();
    void LoadCircuit();
}

public class CircuitManager : ICircuitManager
{
    private readonly IPlatformServices _platformServices;
    private readonly IStatusService _statusService;

    public Circuit.Circuit Circuit { get; private set; }
    public Dictionary<string, CircuitData> CustomComponents { get; } = new();
    public event Action? OnCircuitChanged;

    public CircuitManager(IPlatformServices platformServices, IStatusService statusService)
    {
        _platformServices = platformServices;
        _statusService = statusService;
        Circuit = new Circuit.Circuit { Name = "My Circuit" };
    }

    public void NewCircuit()
    {
        Circuit = new Circuit.Circuit { Name = "New Circuit" };
        OnCircuitChanged?.Invoke();
        _statusService.Show(LocalizationManager.Get("status.ready"));
    }

    public void SaveCircuit()
    {
        var path = _platformServices.GetDefaultCircuitPath();
        try
        {
            CircuitSerializer.SaveCircuit(Circuit, path);
            _statusService.Show(LocalizationManager.Get("status.saved", path));
        }
        catch (Exception ex)
        {
            _statusService.Show(LocalizationManager.Get("status.save_failed", ex.Message));
        }
    }

    public void LoadCircuit()
    {
        var path = _platformServices.GetDefaultCircuitPath();
        if (!_platformServices.FileExists(path))
        {
            _statusService.Show(LocalizationManager.Get("status.no_saved_circuit"));
            return;
        }

        try
        {
            Circuit = CircuitSerializer.LoadCircuit(path, CustomComponents);
            OnCircuitChanged?.Invoke();
            _statusService.Show(LocalizationManager.Get("status.loaded"));
        }
        catch (Exception ex)
        {
            _statusService.Show(LocalizationManager.Get("status.load_failed", ex.Message));
        }
    }
}
