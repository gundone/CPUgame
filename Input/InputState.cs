using Microsoft.Xna.Framework;

namespace CPUgame.Input;

/// <summary>
/// Platform-agnostic input state that can be populated by different input handlers
/// </summary>
public class InputState
{
    // Pointer (mouse/touch) state
    public Point PointerPosition { get; set; }
    public bool PrimaryPressed { get; set; }      // Left mouse / single touch
    public bool PrimaryJustPressed { get; set; }
    public bool PrimaryJustReleased { get; set; }
    public bool PrimaryDoubleClick { get; set; }  // Double-click detected
    public bool SecondaryPressed { get; set; }    // Right mouse / two-finger tap
    public bool SecondaryJustPressed { get; set; }
    public bool MiddlePressed { get; set; }       // Middle mouse (desktop only)
    public bool MiddleJustPressed { get; set; }

    // Scroll/Zoom
    public int ScrollDelta { get; set; }          // Mouse scroll or pinch zoom
    public bool PinchZooming { get; set; }        // Touch pinch gesture active
    public float PinchScale { get; set; } = 1f;   // Pinch zoom scale factor

    // Modifier keys (desktop) / gesture states (mobile)
    public bool CtrlHeld { get; set; }
    public bool ShiftHeld { get; set; }

    // Action commands (abstracted from specific keys/gestures)
    public bool DeleteCommand { get; set; }
    public bool SaveCommand { get; set; }
    public bool LoadCommand { get; set; }
    public bool BuildCommand { get; set; }
    public bool EscapeCommand { get; set; }
    public bool TogglePinValuesCommand { get; set; }

    // Text input (for dialogs)
    public bool EnterPressed { get; set; }
    public bool BackspacePressed { get; set; }
    public bool TabPressed { get; set; }
    public char? CharacterInput { get; set; }

    // Movement commands for selected components
    public bool MoveUp { get; set; }
    public bool MoveDown { get; set; }
    public bool MoveLeft { get; set; }
    public bool MoveRight { get; set; }

    // Component-specific commands
    public bool IncreaseCommand { get; set; }     // + key or gesture
    public bool DecreaseCommand { get; set; }     // - key or gesture
    public bool ToggleCommand { get; set; }       // Space or tap
    public char? NumberInput { get; set; }        // 0-9 for BusInput
    public bool ResizeIncreaseCommand { get; set; }  // Space to increase bus size
    public bool ResizeDecreaseCommand { get; set; }  // Shift+Space to decrease bus size

    public void Clear()
    {
        PrimaryJustPressed = false;
        PrimaryJustReleased = false;
        PrimaryDoubleClick = false;
        SecondaryJustPressed = false;
        MiddleJustPressed = false;
        ScrollDelta = 0;
        DeleteCommand = false;
        SaveCommand = false;
        LoadCommand = false;
        BuildCommand = false;
        EscapeCommand = false;
        TogglePinValuesCommand = false;
        EnterPressed = false;
        BackspacePressed = false;
        TabPressed = false;
        CharacterInput = null;
        MoveUp = false;
        MoveDown = false;
        MoveLeft = false;
        MoveRight = false;
        IncreaseCommand = false;
        DecreaseCommand = false;
        ToggleCommand = false;
        NumberInput = null;
        ResizeIncreaseCommand = false;
        ResizeDecreaseCommand = false;
        PinchScale = 1f;
    }
}
