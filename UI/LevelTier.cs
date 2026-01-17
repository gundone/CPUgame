using System.Collections.Generic;
using CPUgame.Core.Levels;

namespace CPUgame.UI;

internal class LevelTier
{
    public int TierNumber { get; set; }
    public bool IsUnlocked { get; set; }
    public List<GameLevel> Levels { get; set; } = new();
}