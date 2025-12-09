namespace CPUgame.Core.Services;

/// <summary>
/// Result of a file dialog operation
/// </summary>
public class FileDialogResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
}

/// <summary>
/// Platform-specific services interface.
/// Desktop and Android will have different implementations.
/// </summary>
public interface IPlatformServices
{
    /// <summary>
    /// Get the folder for storing custom components
    /// </summary>
    string GetComponentsFolder();

    /// <summary>
    /// Get the folder for storing circuit saves
    /// </summary>
    string GetSavesFolder();

    /// <summary>
    /// Get the default circuit save file path
    /// </summary>
    string GetDefaultCircuitPath();

    /// <summary>
    /// Combine path segments
    /// </summary>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Check if directory exists
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Create directory if it doesn't exist
    /// </summary>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Check if file exists
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Delete a file
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Get all files in directory matching pattern
    /// </summary>
    IEnumerable<string> GetFiles(string directory, string pattern);

    /// <summary>
    /// Read all text from file
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Write all text to file
    /// </summary>
    void WriteAllText(string path, string content);

    /// <summary>
    /// Show a save file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="defaultFileName">Default file name</param>
    /// <param name="filter">File filter (e.g., "Circuit files|*.json")</param>
    /// <param name="initialDirectory">Initial directory</param>
    /// <returns>Result with success status and selected file path</returns>
    FileDialogResult ShowSaveFileDialog(string title, string defaultFileName, string filter, string? initialDirectory = null);

    /// <summary>
    /// Show an open file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter (e.g., "Circuit files|*.json")</param>
    /// <param name="initialDirectory">Initial directory</param>
    /// <returns>Result with success status and selected file path</returns>
    FileDialogResult ShowOpenFileDialog(string title, string filter, string? initialDirectory = null);
}
