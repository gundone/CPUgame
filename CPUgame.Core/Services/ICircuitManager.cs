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