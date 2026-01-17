using CPUgame.Core.Designer;

namespace CPUgame.Core.Serialization;

public class CircuitData
{
    public string Name { get; set; } = "";
    public bool IsCustomComponent { get; set; }
    public List<ComponentData> Components { get; set; } = new();
    public List<ConnectionData> Connections { get; set; } = new();
    public ComponentAppearance? Appearance { get; set; }
}