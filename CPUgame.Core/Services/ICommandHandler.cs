using CPUgame.Core.Input;
using CPUgame.Core.Selection;

namespace CPUgame.Core.Services;

public interface ICommandHandler
{
    bool ShowPinValues { get; }
    event Action? OnBuildComponent;
    void HandleCommands(InputState input, ISelectionManager selection, Circuit.Circuit circuit, IWireManager wireManager, int gridSize);
}