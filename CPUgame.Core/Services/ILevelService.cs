using CPUgame.Core.Circuit;
using CPUgame.Core.Levels;
using CPUgame.Core.TruthTable;

namespace CPUgame.Core.Services;

public interface ILevelService
{
    GameMode CurrentMode { get; }
    GameLevel? CurrentLevel { get; }
    int CurrentLevelIndex { get; }
    List<GameLevel> Levels { get; }
    bool IsLevelPassed { get; }
    bool HasNextLevel { get; }
    GameLevel? NextLevelInfo { get; }
    List<Component> LevelComponents { get; }

    void SetMode(GameMode mode);
    void LoadLevels();
    void SelectLevel(int index);
    void NextLevel();
    void PreviousLevel();
    bool CheckLevelCompletion(List<TruthTableRow> simulatedTable);
    void SetupLevelCircuit(Circuit.Circuit circuit, int gridSize);
    bool IsLevelComponent(Component component);

    event Action? OnModeChanged;
    event Action? OnLevelChanged;
    event Action? OnLevelPassed;
}