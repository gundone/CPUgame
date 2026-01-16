using System.Text.Json;
using System.Text.Json.Serialization;

namespace CPUgame.Core.Services;

/// <summary>
/// User profile containing progress and settings
/// </summary>
public class UserProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("completedLevels")]
    public List<string> CompletedLevels { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool HasCompletedLevel(string levelId)
    {
        return CompletedLevels.Contains(levelId);
    }

    public void CompleteLevel(string levelId)
    {
        if (!CompletedLevels.Contains(levelId))
        {
            CompletedLevels.Add(levelId);
        }
    }
}

public interface IProfileService
{
    UserProfile? CurrentProfile { get; }
    bool HasProfile { get; }
    List<string> GetAvailableProfiles();
    string GetProfileComponentsFolder();

    void CreateProfile(string name);
    void LoadProfile(string name);
    void SaveProfile();
    void DeleteProfile(string name);

    bool IsLevelCompleted(string levelId);
    void CompleteLevel(string levelId);
    bool IsTierUnlocked(int tier, ILevelService levelService);
    List<string> GetUnlockedComponents(ILevelService levelService);

    event Action? OnProfileChanged;
    event Action<string>? OnLevelCompleted;
}

public class ProfileService : IProfileService
{
    private const string ProfilesFolder = "Profiles";
    private const string ProfileFileName = "profile.json";
    private const string ComponentsFolderName = "Components";
    private UserProfile? _currentProfile;

    public UserProfile? CurrentProfile => _currentProfile;
    public bool HasProfile => _currentProfile != null;

    public event Action? OnProfileChanged;
    public event Action<string>? OnLevelCompleted;

    public List<string> GetAvailableProfiles()
    {
        var profiles = new List<string>();
        string profilesPath = GetProfilesPath();

        if (!Directory.Exists(profilesPath))
        {
            return profiles;
        }

        var directories = Directory.GetDirectories(profilesPath);
        foreach (var dir in directories)
        {
            string profileFile = Path.Combine(dir, ProfileFileName);
            if (File.Exists(profileFile))
            {
                profiles.Add(Path.GetFileName(dir));
            }
        }

        return profiles;
    }

    public string GetProfileComponentsFolder()
    {
        if (_currentProfile == null)
        {
            throw new InvalidOperationException("No profile loaded");
        }

        string profilePath = GetProfileFolderPath(_currentProfile.Name);
        string componentsPath = Path.Combine(profilePath, ComponentsFolderName);

        if (!Directory.Exists(componentsPath))
        {
            Directory.CreateDirectory(componentsPath);
        }

        return componentsPath;
    }

    public void CreateProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _currentProfile = new UserProfile
        {
            Name = name,
            CreatedAt = DateTime.Now
        };

        // Create profile folder structure
        string profileFolderPath = GetProfileFolderPath(name);
        if (!Directory.Exists(profileFolderPath))
        {
            Directory.CreateDirectory(profileFolderPath);
        }

        string componentsPath = Path.Combine(profileFolderPath, ComponentsFolderName);
        if (!Directory.Exists(componentsPath))
        {
            Directory.CreateDirectory(componentsPath);
        }

        SaveProfile();
        OnProfileChanged?.Invoke();
    }

    public void LoadProfile(string name)
    {
        string profilePath = Path.Combine(GetProfileFolderPath(name), ProfileFileName);

        if (!File.Exists(profilePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(profilePath);
            _currentProfile = JsonSerializer.Deserialize<UserProfile>(json);
            OnProfileChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load profile {name}: {ex.Message}");
        }
    }

    public void SaveProfile()
    {
        if (_currentProfile == null)
        {
            return;
        }

        string profileFolderPath = GetProfileFolderPath(_currentProfile.Name);
        if (!Directory.Exists(profileFolderPath))
        {
            Directory.CreateDirectory(profileFolderPath);
        }

        string profilePath = Path.Combine(profileFolderPath, ProfileFileName);

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_currentProfile, options);
            File.WriteAllText(profilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save profile: {ex.Message}");
        }
    }

    public void DeleteProfile(string name)
    {
        string profileFolderPath = GetProfileFolderPath(name);

        if (Directory.Exists(profileFolderPath))
        {
            try
            {
                Directory.Delete(profileFolderPath, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete profile folder: {ex.Message}");
            }
        }

        if (_currentProfile?.Name == name)
        {
            _currentProfile = null;
            OnProfileChanged?.Invoke();
        }
    }

    public bool IsLevelCompleted(string levelId)
    {
        return _currentProfile?.HasCompletedLevel(levelId) ?? false;
    }

    public void CompleteLevel(string levelId)
    {
        if (_currentProfile == null)
        {
            return;
        }

        if (!_currentProfile.HasCompletedLevel(levelId))
        {
            _currentProfile.CompleteLevel(levelId);
            SaveProfile();
            OnLevelCompleted?.Invoke(levelId);
        }
    }

    public bool IsTierUnlocked(int tier, ILevelService levelService)
    {
        if (tier <= 1)
        {
            return true;
        }

        // Check if all levels in the previous tier are completed
        foreach (var level in levelService.Levels)
        {
            if (level.Tier == tier - 1 && !IsLevelCompleted(level.Id))
            {
                return false;
            }
        }

        return true;
    }

    public List<string> GetUnlockedComponents(ILevelService levelService)
    {
        var components = new List<string>();
        if (_currentProfile == null)
        {
            return components;
        }

        foreach (var levelId in _currentProfile.CompletedLevels)
        {
            var level = levelService.Levels.Find(l => l.Id == levelId);
            if (level != null && !string.IsNullOrEmpty(level.ComponentName))
            {
                components.Add(level.ComponentName);
            }
        }
        return components;
    }

    private string GetProfilesPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFolder);
    }

    private string GetProfileFolderPath(string name)
    {
        return Path.Combine(GetProfilesPath(), name);
    }
}
