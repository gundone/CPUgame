namespace CPUgame.Input;

/// <summary>
/// Interface for platform-specific input handling.
/// Desktop implementation uses Mouse/Keyboard, Android will use Touch.
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Update input state from platform-specific sources
    /// </summary>
    void Update(InputState state);

    /// <summary>
    /// Update input state with delta time for time-based detection (double-click)
    /// </summary>
    void Update(InputState state, double deltaTime);

    /// <summary>
    /// Set the cursor appearance (desktop only, no-op on mobile)
    /// </summary>
    void SetCursor(CursorType cursor);

    /// <summary>
    /// Check if text input mode is active (for on-screen keyboard on mobile)
    /// </summary>
    bool IsTextInputActive { get; }

    /// <summary>
    /// Begin text input mode (shows on-screen keyboard on mobile)
    /// </summary>
    void BeginTextInput();

    /// <summary>
    /// End text input mode (hides on-screen keyboard on mobile)
    /// </summary>
    void EndTextInput();
}

public enum CursorType
{
    Arrow,
    Hand,
    Move,
    Crosshair
}
