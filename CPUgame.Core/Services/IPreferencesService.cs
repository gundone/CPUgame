using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CPUgame.Core.Services;

/// <summary>
/// User preferences containing last session information and UI settings
/// </summary>
public class UserPreferences
{
    [JsonPropertyName("lastProfile")]
    public string? LastProfile { get; set; }

    [JsonPropertyName("lastLevelIndex")]
    public int LastLevelIndex { get; set; } = -1;
}

public interface IPreferencesService
{
    string? LastProfile { get; set; }
    int LastLevelIndex { get; set; }

    void Load();
    void Save();
    void SetLastSession(string profileName, int levelIndex);

    event Action? OnPreferencesChanged;
}

public class PreferencesService : IPreferencesService
{
    private const string _preferencesFolder = "Preferences";
    private const string _preferencesFileName = "user.json";

    private UserPreferences _preferences = new();

    public string? LastProfile
    {
        get => _preferences.LastProfile;
        set
        {
            if (_preferences.LastProfile != value)
            {
                _preferences.LastProfile = value;
                Save();
                OnPreferencesChanged?.Invoke();
            }
        }
    }

    public int LastLevelIndex
    {
        get => _preferences.LastLevelIndex;
        set
        {
            if (_preferences.LastLevelIndex != value)
            {
                _preferences.LastLevelIndex = value;
                Save();
                OnPreferencesChanged?.Invoke();
            }
        }
    }

    public event Action? OnPreferencesChanged;

    public void Load()
    {
        string preferencesPath = GetPreferencesPath();

        if (!File.Exists(preferencesPath))
        {
            _preferences = new UserPreferences();
            return;
        }

        try
        {
            string json = File.ReadAllText(preferencesPath);
            var loadedPreferences = JsonSerializer.Deserialize<UserPreferences>(json);
            if (loadedPreferences != null)
            {
                _preferences = loadedPreferences;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load preferences: {ex.Message}");
            _preferences = new UserPreferences();
        }
    }

    public void Save()
    {
        string folder = GetPreferencesFolder();
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string preferencesPath = GetPreferencesPath();

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_preferences, options);
            File.WriteAllText(preferencesPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save preferences: {ex.Message}");
        }
    }

    public void SetLastSession(string profileName, int levelIndex)
    {
        _preferences.LastProfile = profileName;
        _preferences.LastLevelIndex = levelIndex;
        Save();
        OnPreferencesChanged?.Invoke();
    }

    private static string GetPreferencesFolder()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _preferencesFolder);
    }

    private static string GetPreferencesPath()
    {
        return Path.Combine(GetPreferencesFolder(), _preferencesFileName);
    }
}
