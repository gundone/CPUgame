namespace CPUgame.Core.Services;

public interface IStatusService
{
    string Message { get; }
    void Show(string message, double duration = 3.0);
    void Update(double deltaTime);
}