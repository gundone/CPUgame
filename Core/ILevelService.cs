using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CPUgame.Components;
using CPUgame.UI;

namespace CPUgame.Core;

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
    void SetupLevelCircuit(Circuit circuit, int gridSize);
    bool IsLevelComponent(Component component);

    event Action? OnModeChanged;
    event Action? OnLevelChanged;
    event Action? OnLevelPassed;
}

public class LevelService : ILevelService
{
    private const string LevelsFolder = "Levels";
    private List<GameLevel> _levels = new();
    private int _currentLevelIndex = -1;
    private GameMode _currentMode = GameMode.Sandbox;
    private bool _isLevelPassed;
    private List<Component> _levelComponents = new();

    public GameMode CurrentMode => _currentMode;
    public GameLevel? CurrentLevel => _currentLevelIndex >= 0 && _currentLevelIndex < _levels.Count ? _levels[_currentLevelIndex] : null;
    public int CurrentLevelIndex => _currentLevelIndex;
    public List<GameLevel> Levels => _levels;
    public bool IsLevelPassed => _isLevelPassed;
    public bool HasNextLevel => _currentLevelIndex >= 0 && _currentLevelIndex < _levels.Count - 1;
    public GameLevel? NextLevelInfo => HasNextLevel ? _levels[_currentLevelIndex + 1] : null;
    public List<Component> LevelComponents => _levelComponents;

    public event Action? OnModeChanged;
    public event Action? OnLevelChanged;
    public event Action? OnLevelPassed;

    public void SetMode(GameMode mode)
    {
        if (_currentMode != mode)
        {
            _currentMode = mode;
            _isLevelPassed = false;

            if (mode == GameMode.Levels && _levels.Count > 0 && _currentLevelIndex < 0)
            {
                _currentLevelIndex = 0;
            }

            OnModeChanged?.Invoke();
        }
    }

    public void LoadLevels()
    {
        _levels.Clear();

        string levelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LevelsFolder);

        if (!Directory.Exists(levelsPath))
        {
            Directory.CreateDirectory(levelsPath);
            return;
        }

        var levelFiles = Directory.GetFiles(levelsPath, "*.json").OrderBy(f => f).ToList();

        foreach (var file in levelFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var level = JsonSerializer.Deserialize<GameLevel>(json);
                if (level != null)
                {
                    _levels.Add(level);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load level {file}: {ex.Message}");
            }
        }
    }

    public void SelectLevel(int index)
    {
        if (index >= 0 && index < _levels.Count)
        {
            _currentLevelIndex = index;
            _isLevelPassed = false;
            OnLevelChanged?.Invoke();
        }
    }

    public void NextLevel()
    {
        if (_currentLevelIndex < _levels.Count - 1)
        {
            SelectLevel(_currentLevelIndex + 1);
        }
    }

    public void PreviousLevel()
    {
        if (_currentLevelIndex > 0)
        {
            SelectLevel(_currentLevelIndex - 1);
        }
    }

    public bool CheckLevelCompletion(List<TruthTableRow> simulatedTable)
    {
        if (CurrentLevel == null || _currentMode != GameMode.Levels)
        {
            return false;
        }

        var levelTable = CurrentLevel.TruthTable;

        // Check if tables have same number of rows
        if (simulatedTable.Count != levelTable.Count)
        {
            _isLevelPassed = false;
            return false;
        }

        // Check each row
        for (int i = 0; i < simulatedTable.Count; i++)
        {
            var simRow = simulatedTable[i];
            var levelRow = levelTable[i];

            // Check inputs match
            if (simRow.InputValues.Count != levelRow.Inputs.Count)
            {
                _isLevelPassed = false;
                return false;
            }

            for (int j = 0; j < simRow.InputValues.Count; j++)
            {
                if (simRow.InputValues[j] != levelRow.Inputs[j])
                {
                    _isLevelPassed = false;
                    return false;
                }
            }

            // Check outputs match
            if (simRow.OutputValues.Count != levelRow.Outputs.Count)
            {
                _isLevelPassed = false;
                return false;
            }

            for (int j = 0; j < simRow.OutputValues.Count; j++)
            {
                if (simRow.OutputValues[j] != levelRow.Outputs[j])
                {
                    _isLevelPassed = false;
                    return false;
                }
            }
        }

        bool wasPassed = _isLevelPassed;
        _isLevelPassed = true;

        if (!wasPassed)
        {
            OnLevelPassed?.Invoke();
        }

        return true;
    }

    public void SetupLevelCircuit(Circuit circuit, int gridSize)
    {
        // Clear previous level components tracking
        _levelComponents.Clear();

        if (CurrentLevel == null || _currentMode != GameMode.Levels)
        {
            return;
        }

        // Clear existing circuit
        circuit.Components.Clear();

        // Create input bus(es) based on level input count
        int inputCount = CurrentLevel.InputCount;
        var inputBus = new BusInput(gridSize * 2, gridSize * 4, inputCount, gridSize);
        inputBus.Title = "IN";
        circuit.AddComponent(inputBus);
        _levelComponents.Add(inputBus);

        // Create output bus(es) based on level output count
        int outputCount = CurrentLevel.OutputCount;
        var outputBus = new BusOutput(gridSize * 12, gridSize * 4, outputCount, gridSize);
        outputBus.Title = "OUT";
        circuit.AddComponent(outputBus);
        _levelComponents.Add(outputBus);
    }

    public bool IsLevelComponent(Component component)
    {
        return _currentMode == GameMode.Levels && _levelComponents.Contains(component);
    }
}
