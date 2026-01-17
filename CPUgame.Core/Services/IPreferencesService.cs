using System;
using System.IO;

namespace CPUgame.Core.Services;

public interface IPreferencesService
{
    string? LastProfile { get; set; }
    int LastLevelIndex { get; set; }

    void Load();
    void Save();
    void SetLastSession(string profileName, int levelIndex);

    event Action? OnPreferencesChanged;
}