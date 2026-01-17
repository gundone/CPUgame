using System;
using System.Collections.Generic;

namespace CPUgame.UI;

public class MenuItem
{
    public string Label { get; }
    public Action? Action { get; }
    public List<MenuItem> SubItems { get; } = new();
    public bool IsSeparator => Label == "-";
    public bool IsHeader => Label.StartsWith("--");

    public MenuItem(string label, Action? action = null)
    {
        Label = label;
        Action = action;
    }
}