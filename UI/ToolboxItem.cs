namespace CPUgame.UI;

public class ToolboxItem
{
    public string Label { get; }
    public ToolType? Tool { get; }
    public string? CustomName { get; }
    public bool IsCustom => CustomName != null;

    public ToolboxItem(string label, ToolType tool)
    {
        Label = label;
        Tool = tool;
        CustomName = null;
    }

    public ToolboxItem(string customName)
    {
        Label = customName;
        Tool = null;
        CustomName = customName;
    }
}