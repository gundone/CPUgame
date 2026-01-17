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