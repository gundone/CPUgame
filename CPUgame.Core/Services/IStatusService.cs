using CPUgame.Core.Localization;

namespace CPUgame.Core.Services;

public interface IStatusService
{
    string Message { get; }
    void Show(string message, double duration = 3.0);
    void Update(double deltaTime);
}

public class StatusService : IStatusService
{
    public string Message { get; private set; } = "";
    private double _remainingTime;

    public void Show(string message, double duration = 3.0)
    {
        Message = message;
        _remainingTime = duration;
    }

    public void Update(double deltaTime)
    {
        if (_remainingTime > 0)
        {
            _remainingTime -= deltaTime;
            if (_remainingTime <= 0)
            {
                Message = LocalizationManager.Get("status.ready");
            }
        }
    }
}
