namespace CPUgame.Core.Services;

/// <summary>
/// Result of a file dialog operation
/// </summary>
public class FileDialogResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
}