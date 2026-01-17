using CPUgame.Core.Circuit;
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