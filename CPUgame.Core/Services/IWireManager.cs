using CPUgame.Core.Circuit;
using CPUgame.Core.Localization;
using CPUgame.Core.Primitives;

namespace CPUgame.Core.Services;

public interface IWireManager
{
    Pin? WireStartPin { get; }
    Pin? HoveredPin { get; }
    bool IsDraggingWire { get; }
    void Update(Circuit.Circuit circuit, Point2 worldMousePos, bool primaryJustPressed, bool primaryJustReleased, bool shiftHeld);
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

    public void Update(Circuit.Circuit circuit, Point2 worldMousePos, bool primaryJustPressed, bool primaryJustReleased, bool shiftHeld)
    {
        HoveredPin = circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);

        // Auto wire mode only starts with Shift held
        if (primaryJustPressed && HoveredPin != null && shiftHeld)
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
