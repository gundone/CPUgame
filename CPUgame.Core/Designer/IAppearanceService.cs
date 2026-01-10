using CPUgame.Core.Circuit;

namespace CPUgame.Core.Designer;

/// <summary>
/// Service for managing component visual appearances.
/// </summary>
public interface IAppearanceService
{
    /// <summary>
    /// Gets the custom appearance for a component type, or null if using defaults.
    /// </summary>
    /// <param name="componentType">The component type identifier</param>
    ComponentAppearance? GetAppearance(string componentType);

    /// <summary>
    /// Sets a custom appearance for a component type.
    /// </summary>
    /// <param name="appearance">The appearance to save</param>
    void SetAppearance(ComponentAppearance appearance);

    /// <summary>
    /// Removes custom appearance, reverting to default.
    /// </summary>
    /// <param name="componentType">The component type identifier</param>
    void ResetAppearance(string componentType);

    /// <summary>
    /// Gets the default appearance for a component type.
    /// </summary>
    /// <param name="componentType">The component type identifier</param>
    ComponentAppearance GetDefaultAppearance(string componentType);

    /// <summary>
    /// Checks if a component type has a custom appearance.
    /// </summary>
    /// <param name="componentType">The component type identifier</param>
    bool HasCustomAppearance(string componentType);

    /// <summary>
    /// Applies the stored appearance to a component instance.
    /// </summary>
    /// <param name="component">The component to update</param>
    void ApplyAppearance(Component component);

    /// <summary>
    /// Gets the component type identifier for a component instance.
    /// </summary>
    /// <param name="component">The component</param>
    string GetComponentType(Component component);

    /// <summary>
    /// Gets all available component types (built-in and custom).
    /// </summary>
    IEnumerable<string> GetAllComponentTypes();

    /// <summary>
    /// Saves all appearances to persistent storage.
    /// </summary>
    void SaveAll();

    /// <summary>
    /// Loads all appearances from persistent storage.
    /// </summary>
    void LoadAll();

    /// <summary>
    /// Event fired when an appearance is changed.
    /// </summary>
    event Action<string>? OnAppearanceChanged;
}
