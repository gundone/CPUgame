using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Input;

/// <summary>
/// Desktop (Windows/Linux/Mac) input handler using Mouse and Keyboard
/// </summary>
public class DesktopInputHandler : IInputHandler
{
    private MouseState _prevMouse;
    private KeyboardState _prevKeyboard;
    private bool _textInputActive;

    public bool IsTextInputActive => _textInputActive;

    public void Update(InputState state)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        // Pointer position
        state.PointerPosition = new Point(mouse.X, mouse.Y);

        // Primary button (left mouse)
        state.PrimaryPressed = mouse.LeftButton == ButtonState.Pressed;
        state.PrimaryJustPressed = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        state.PrimaryJustReleased = mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed;

        // Secondary button (right mouse)
        state.SecondaryPressed = mouse.RightButton == ButtonState.Pressed;
        state.SecondaryJustPressed = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

        // Middle button
        state.MiddlePressed = mouse.MiddleButton == ButtonState.Pressed;
        state.MiddleJustPressed = mouse.MiddleButton == ButtonState.Pressed && _prevMouse.MiddleButton == ButtonState.Released;

        // Scroll
        state.ScrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;

        // Modifiers
        state.CtrlHeld = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        state.ShiftHeld = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        // Commands
        state.DeleteCommand = IsKeyJustPressed(Keys.Delete, keyboard) || IsKeyJustPressed(Keys.Back, keyboard);
        state.SaveCommand = state.CtrlHeld && IsKeyJustPressed(Keys.S, keyboard);
        state.LoadCommand = state.CtrlHeld && IsKeyJustPressed(Keys.O, keyboard);
        state.BuildCommand = state.CtrlHeld && IsKeyJustPressed(Keys.B, keyboard);
        state.EscapeCommand = IsKeyJustPressed(Keys.Escape, keyboard);
        state.TogglePinValuesCommand = IsKeyJustPressed(Keys.V, keyboard);

        // Text input
        state.EnterPressed = IsKeyJustPressed(Keys.Enter, keyboard);
        state.BackspacePressed = IsKeyJustPressed(Keys.Back, keyboard);
        state.CharacterInput = GetCharacterInput(keyboard);

        // Movement
        state.MoveUp = IsKeyJustPressed(Keys.Up, keyboard);
        state.MoveDown = IsKeyJustPressed(Keys.Down, keyboard);
        state.MoveLeft = IsKeyJustPressed(Keys.Left, keyboard);
        state.MoveRight = IsKeyJustPressed(Keys.Right, keyboard);

        // Component commands
        state.IncreaseCommand = IsKeyJustPressed(Keys.OemPlus, keyboard) || IsKeyJustPressed(Keys.Add, keyboard);
        state.DecreaseCommand = IsKeyJustPressed(Keys.OemMinus, keyboard) || IsKeyJustPressed(Keys.Subtract, keyboard);
        state.ToggleCommand = !state.ShiftHeld && IsKeyJustPressed(Keys.Space, keyboard);
        state.NumberInput = GetNumberInput(keyboard);

        // Resize commands (spacebar / shift+space for bus components)
        state.ResizeIncreaseCommand = !state.ShiftHeld && IsKeyJustPressed(Keys.Space, keyboard);
        state.ResizeDecreaseCommand = state.ShiftHeld && IsKeyJustPressed(Keys.Space, keyboard);

        _prevMouse = mouse;
        _prevKeyboard = keyboard;
    }

    public void SetCursor(CursorType cursor)
    {
        Mouse.SetCursor(cursor switch
        {
            CursorType.Move => MouseCursor.SizeAll,
            CursorType.Hand => MouseCursor.Hand,
            CursorType.Crosshair => MouseCursor.Crosshair,
            _ => MouseCursor.Arrow
        });
    }

    public void BeginTextInput()
    {
        _textInputActive = true;
    }

    public void EndTextInput()
    {
        _textInputActive = false;
    }

    private bool IsKeyJustPressed(Keys key, KeyboardState current)
    {
        return current.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
    }

    private char? GetCharacterInput(KeyboardState keyboard)
    {
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        foreach (Keys key in keyboard.GetPressedKeys())
        {
            if (_prevKeyboard.IsKeyDown(key)) continue;

            if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)('a' + (key - Keys.A));
                return shift ? char.ToUpper(c) : c;
            }
            if (key >= Keys.D0 && key <= Keys.D9 && !shift)
            {
                return (char)('0' + (key - Keys.D0));
            }
            if (key == Keys.Space)
            {
                return ' ';
            }
            if (key == Keys.OemMinus || key == Keys.Subtract)
            {
                return shift ? '_' : '-';
            }
        }

        return null;
    }

    private char? GetNumberInput(KeyboardState keyboard)
    {
        for (int i = 0; i <= 9; i++)
        {
            var key = Keys.D0 + i;
            if (IsKeyJustPressed(key, keyboard))
            {
                return (char)('0' + i);
            }
        }
        return null;
    }
}
