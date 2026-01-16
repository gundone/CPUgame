using CPUgame.Core.Circuit;
using CPUgame.Core.Levels;

namespace CPUgame.UI;

/// <summary>
/// Represents an open editor tab with a circuit and metadata
/// </summary>
public class EditorTab
{
    public string Name { get; set; }
    public Circuit Circuit { get; set; }
    public bool IsDirty { get; set; }
    public bool IsMainCircuit { get; }
    public GameMode GameMode { get; set; }
    public GameLevel? Level { get; set; }

    public EditorTab(string name, Circuit circuit, GameMode gameMode, GameLevel? level = null, bool isMainCircuit = false)
    {
        Name = name;
        Circuit = circuit;
        GameMode = gameMode;
        Level = level;
        IsDirty = false;
        IsMainCircuit = isMainCircuit;
    }

    /// <summary>
    /// Gets the display name for the tab based on game mode and level
    /// </summary>
    public string GetDisplayName()
    {
        return GameMode switch
        {
            GameMode.Levels when Level != null => $"Level: {Level.Name}",
            GameMode.Sandbox => "Sandbox",
            GameMode.Designer => "Designer",
            _ => Name
        };
    }
}
