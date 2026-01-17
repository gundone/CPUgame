namespace CPUgame.Core;

public class GameSettings : IGameSettings
{
    private float _titleFontScale = 0.8f;

    public float TitleFontScale
    {
        get => _titleFontScale;
        set
        {
            if (Math.Abs(_titleFontScale - value) > 0.01f)
            {
                _titleFontScale = Math.Clamp(value, 0.5f, 2.0f);
                OnSettingsChanged?.Invoke();
            }
        }
    }

    public event Action? OnSettingsChanged;
}
