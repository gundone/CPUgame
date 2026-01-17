namespace CPUgame.Core;

public interface IGameSettings
{
    float TitleFontScale { get; set; }
    event Action? OnSettingsChanged;
}