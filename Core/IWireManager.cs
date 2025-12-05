using Microsoft.Xna.Framework;

namespace CPUgame.Core;

public interface IWireManager
{
    Pin? WireStartPin { get; }
    Pin? HoveredPin { get; }
    bool IsDraggingWire { get; }
    void Update(Circuit circuit, Point worldMousePos, bool primaryJustPressed, bool primaryJustReleased);
    void Cancel();
}

public class WireManager : IWireManager
{
    private readonly IStatusService _statusService;

    public Pin? WireStartPin { get; private set; }
    public Pin? HoveredPin { get; private set; }
    public bool IsDraggingWire { get; private set; }

    public WireManager(IStatusService statusService)
    {
        _statusService = statusService;
    }

    public void Update(Circuit circuit, Point worldMousePos, bool primaryJustPressed, bool primaryJustReleased)
    {
        HoveredPin = circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);

        if (primaryJustPressed && HoveredPin != null)
        {
            WireStartPin = HoveredPin;
            IsDraggingWire = true;
            return;
        }

        if (IsDraggingWire && primaryJustReleased)
        {
            if (HoveredPin != null && WireStartPin != null &&
                HoveredPin != WireStartPin &&
                HoveredPin.Owner != WireStartPin.Owner &&
                HoveredPin.Type != WireStartPin.Type)
            {
                WireStartPin.Connect(HoveredPin);
                _statusService.Show(LocalizationManager.Get("status.wire_connected"));
            }
            WireStartPin = null;
            IsDraggingWire = false;
        }
    }

    public void Cancel()
    {
        WireStartPin = null;
        IsDraggingWire = false;
    }
}
