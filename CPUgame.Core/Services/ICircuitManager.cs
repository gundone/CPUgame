using CPUgame.Core.Localization;
using CPUgame.Core.Serialization;

namespace CPUgame.Core.Services;

public interface ICircuitManager
{
    Circuit.Circuit Circuit { get; }
    Dictionary<string, CircuitData> CustomComponents { get; }
    string? CurrentFilePath { get; }
    event Action? OnCircuitChanged;
    void NewCircuit();
    void SaveCircuit();
    void SaveCircuitAs();
    void LoadCircuit();
}

public class CircuitManager : ICircuitManager
{
    private readonly IPlatformServices _platformServices;
    private readonly IStatusService _statusService;

    public Circuit.Circuit Circuit { get; private set; }
    public Dictionary<string, CircuitData> CustomComponents { get; } = new();
    public string? CurrentFilePath { get; private set; }
    public event Action? OnCircuitChanged;

    private const string FileFilter = "Circuit files (*.json)|*.json|All files (*.*)|*.*";

    public CircuitManager(IPlatformServices platformServices, IStatusService statusService)
    {
        _platformServices = platformServices;
        _statusService = statusService;
        Circuit = new Circuit.Circuit { Name = "My Circuit" };
    }

    public void NewCircuit()
    {
        Circuit = new Circuit.Circuit { Name = "New Circuit" };
        CurrentFilePath = null;
        OnCircuitChanged?.Invoke();
        _statusService.Show(LocalizationManager.Get("status.ready"));
    }

    public void SaveCircuit()
    {
        // If we have a current file path, save to it directly
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            SaveToPath(CurrentFilePath);
            return;
        }

        // Otherwise, show save dialog
        SaveCircuitAs();
    }

    public void SaveCircuitAs()
    {
        var defaultName = !string.IsNullOrEmpty(Circuit.Name) ? Circuit.Name : "circuit";
        var result = _platformServices.ShowSaveFileDialog(
            LocalizationManager.Get("dialog.save_circuit"),
            defaultName + ".json",
            FileFilter);

        if (result.Success && !string.IsNullOrEmpty(result.FilePath))
        {
            SaveToPath(result.FilePath);
        }
    }

    private void SaveToPath(string path)
    {
        try
        {
            CircuitSerializer.SaveCircuit(Circuit, path);
            CurrentFilePath = path;
            _statusService.Show(LocalizationManager.Get("status.saved", path));
        }
        catch (Exception ex)
        {
            _statusService.Show(LocalizationManager.Get("status.save_failed", ex.Message));
        }
    }

    public void LoadCircuit()
    {
        var result = _platformServices.ShowOpenFileDialog(
            LocalizationManager.Get("dialog.load_circuit"),
            FileFilter);

        if (!result.Success || string.IsNullOrEmpty(result.FilePath))
        {
            return;
        }

        try
        {
            Circuit = CircuitSerializer.LoadCircuit(result.FilePath, CustomComponents);
            CurrentFilePath = result.FilePath;
            OnCircuitChanged?.Invoke();
            _statusService.Show(LocalizationManager.Get("status.loaded"));
        }
        catch (Exception ex)
        {
            _statusService.Show(LocalizationManager.Get("status.load_failed", ex.Message));
        }
    }
}
