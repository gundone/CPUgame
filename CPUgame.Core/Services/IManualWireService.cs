using CPUgame.Core.Circuit;
using CPUgame.Core.Primitives;

namespace CPUgame.Core.Services;

/// <summary>
/// Service for manual wire tracing mode.
/// Allows users to draw wires by clicking grid nodes to create custom paths.
/// </summary>
public interface IManualWireService
{
    /// <summary>
    /// Whether manual wire tracing mode is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether wire node editing mode is active.
    /// </summary>
    bool IsEditingWire { get; }

    /// <summary>
    /// The wire (input pin) being edited.
    /// </summary>
    Pin? EditingWire { get; }

    /// <summary>
    /// Index of the node being dragged (-1 if not dragging).
    /// </summary>
    int DraggingNodeIndex { get; }

    /// <summary>
    /// The starting pin for the manual wire.
    /// </summary>
    Pin? StartPin { get; }

    /// <summary>
    /// Current path points (grid-snapped).
    /// </summary>
    IReadOnlyList<Point2> PathPoints { get; }

    /// <summary>
    /// Grid size for snapping.
    /// </summary>
    int GridSize { get; }

    /// <summary>
    /// Start manual wire tracing from a pin.
    /// </summary>
    void Start(Pin pin, int gridSize);

    /// <summary>
    /// Add a point to the path (will be snapped to grid).
    /// </summary>
    void AddPoint(Point2 worldPos);

    /// <summary>
    /// Complete the wire connection to the target pin.
    /// Returns true if connection was successful.
    /// </summary>
    bool Complete(Pin targetPin);

    /// <summary>
    /// Cancel manual wire tracing.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Remove the last point from the path (undo).
    /// </summary>
    void RemoveLastPoint();

    /// <summary>
    /// Start editing a selected wire's nodes.
    /// If the wire doesn't have a ManualWirePath, one will be created automatically.
    /// </summary>
    void StartEditingWire(Pin inputPin, int gridSize);

    /// <summary>
    /// Converts an auto-routed wire to a manual wire by creating a path with endpoints.
    /// Returns true if conversion was successful.
    /// </summary>
    bool ConvertToManualWire(Pin inputPin);

    /// <summary>
    /// Stop editing the current wire.
    /// </summary>
    void StopEditingWire();

    /// <summary>
    /// Get node index at world position, or -1 if none.
    /// </summary>
    int GetNodeAtPosition(Point2 worldPos, int tolerance = 10);

    /// <summary>
    /// Start dragging a node.
    /// </summary>
    void StartDraggingNode(int nodeIndex);

    /// <summary>
    /// Update the position of the dragging node.
    /// </summary>
    void UpdateDraggingNode(Point2 worldPos);

    /// <summary>
    /// Stop dragging the node.
    /// </summary>
    void StopDraggingNode();

    /// <summary>
    /// Update wire endpoints when a component moves.
    /// Recalculates path from pin to first/last internal node.
    /// </summary>
    void UpdateWireEndpoints(Pin inputPin);

    /// <summary>
    /// Add a new node at the given position on the wire.
    /// Finds the nearest segment and inserts the node.
    /// </summary>
    bool AddNodeAtPosition(Point2 worldPos);

    /// <summary>
    /// Remove the node at the given index.
    /// Only internal nodes can be removed (not endpoints).
    /// </summary>
    bool RemoveNode(int nodeIndex);

    /// <summary>
    /// Get the segment index at world position, or -1 if none.
    /// Returns the index of the first point of the segment.
    /// </summary>
    int GetSegmentAtPosition(Point2 worldPos, int tolerance = 8);
}