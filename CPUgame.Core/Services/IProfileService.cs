namespace CPUgame.Core.Services;

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