using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

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
    IReadOnlyList<Point> PathPoints { get; }

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
    void AddPoint(Point worldPos);

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
    /// Start editing a selected manual wire's nodes.
    /// </summary>
    void StartEditingWire(Pin inputPin, int gridSize);

    /// <summary>
    /// Stop editing the current wire.
    /// </summary>
    void StopEditingWire();

    /// <summary>
    /// Get node index at world position, or -1 if none.
    /// </summary>
    int GetNodeAtPosition(Point worldPos, int tolerance = 10);

    /// <summary>
    /// Start dragging a node.
    /// </summary>
    void StartDraggingNode(int nodeIndex);

    /// <summary>
    /// Update the position of the dragging node.
    /// </summary>
    void UpdateDraggingNode(Point worldPos);

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
    bool AddNodeAtPosition(Point worldPos);

    /// <summary>
    /// Remove the node at the given index.
    /// Only internal nodes can be removed (not endpoints).
    /// </summary>
    bool RemoveNode(int nodeIndex);

    /// <summary>
    /// Get the segment index at world position, or -1 if none.
    /// Returns the index of the first point of the segment.
    /// </summary>
    int GetSegmentAtPosition(Point worldPos, int tolerance = 8);
}

public class ManualWireService : IManualWireService
{
    private readonly IStatusService _statusService;
    private readonly List<Point> _pathPoints = new();
    private int _gridSize = 20;

    // Wire editing state
    private Pin? _editingWire;
    private int _draggingNodeIndex = -1;

    public bool IsActive { get; private set; }
    public Pin? StartPin { get; private set; }
    public IReadOnlyList<Point> PathPoints => _pathPoints;
    public int GridSize => _gridSize;

    public bool IsEditingWire => _editingWire != null;
    public Pin? EditingWire => _editingWire;
    public int DraggingNodeIndex => _draggingNodeIndex;

    public ManualWireService(IStatusService statusService)
    {
        _statusService = statusService;
    }

    public void Start(Pin pin, int gridSize)
    {
        StopEditingWire();
        _gridSize = gridSize;
        StartPin = pin;
        IsActive = true;
        _pathPoints.Clear();

        // Add starting point (the pin location, snapped to grid)
        _pathPoints.Add(SnapToGrid(new Point(pin.WorldX, pin.WorldY)));
    }

    public void AddPoint(Point worldPos)
    {
        if (!IsActive)
        {
            return;
        }

        var snapped = SnapToGrid(worldPos);

        // Don't add duplicate points
        if (_pathPoints.Count > 0 && _pathPoints[^1] == snapped)
        {
            return;
        }

        _pathPoints.Add(snapped);
    }

    public bool Complete(Pin targetPin)
    {
        if (!IsActive || StartPin == null)
        {
            return false;
        }

        // Validate connection
        if (targetPin == StartPin ||
            targetPin.Owner == StartPin.Owner ||
            targetPin.Type == StartPin.Type)
        {
            return false;
        }

        // Add final point (target pin location)
        var endPoint = SnapToGrid(new Point(targetPin.WorldX, targetPin.WorldY));
        if (_pathPoints.Count == 0 || _pathPoints[^1] != endPoint)
        {
            _pathPoints.Add(endPoint);
        }

        // Determine which pin is input and which is output
        Pin inputPin, outputPin;
        List<Point> path;

        if (StartPin.Type == PinType.Output && targetPin.Type == PinType.Input)
        {
            outputPin = StartPin;
            inputPin = targetPin;
            path = new List<Point>(_pathPoints);
        }
        else
        {
            outputPin = targetPin;
            inputPin = StartPin;
            // Reverse path since we drew from input to output
            path = new List<Point>(_pathPoints);
            path.Reverse();
        }

        // Make connection
        inputPin.ConnectedTo = outputPin;
        inputPin.ManualWirePath = path;

        _statusService.Show(LocalizationManager.Get("status.wire_connected"));

        Cancel();
        return true;
    }

    public void Cancel()
    {
        IsActive = false;
        StartPin = null;
        _pathPoints.Clear();
    }

    public void RemoveLastPoint()
    {
        if (_pathPoints.Count > 1)
        {
            _pathPoints.RemoveAt(_pathPoints.Count - 1);
        }
    }

    public void StartEditingWire(Pin inputPin, int gridSize)
    {
        if (inputPin.ManualWirePath == null || inputPin.ManualWirePath.Count < 2)
        {
            return;
        }

        Cancel(); // Cancel any active wire drawing
        _gridSize = gridSize;
        _editingWire = inputPin;
        _draggingNodeIndex = -1;
    }

    public void StopEditingWire()
    {
        _editingWire = null;
        _draggingNodeIndex = -1;
    }

    public int GetNodeAtPosition(Point worldPos, int tolerance = 10)
    {
        if (_editingWire?.ManualWirePath == null)
        {
            return -1;
        }

        var path = _editingWire.ManualWirePath;

        // Check internal nodes only (not pin endpoints)
        // Path goes: output pin location -> internal nodes -> input pin location
        // We allow moving internal nodes (indices 1 to path.Count - 2)
        for (int i = 1; i < path.Count - 1; i++)
        {
            var node = path[i];
            int dx = worldPos.X - node.X;
            int dy = worldPos.Y - node.Y;
            if (dx * dx + dy * dy <= tolerance * tolerance)
            {
                return i;
            }
        }

        return -1;
    }

    public void StartDraggingNode(int nodeIndex)
    {
        if (_editingWire?.ManualWirePath == null)
        {
            return;
        }

        // Only allow dragging internal nodes (not endpoints)
        if (nodeIndex > 0 && nodeIndex < _editingWire.ManualWirePath.Count - 1)
        {
            _draggingNodeIndex = nodeIndex;
        }
    }

    public void UpdateDraggingNode(Point worldPos)
    {
        if (_editingWire?.ManualWirePath == null || _draggingNodeIndex < 0)
        {
            return;
        }

        var path = _editingWire.ManualWirePath;
        if (_draggingNodeIndex > 0 && _draggingNodeIndex < path.Count - 1)
        {
            // Snap to grid and update position
            path[_draggingNodeIndex] = SnapToGrid(worldPos);
        }
    }

    public void StopDraggingNode()
    {
        _draggingNodeIndex = -1;
    }

    public void UpdateWireEndpoints(Pin inputPin)
    {
        if (inputPin.ManualWirePath == null || inputPin.ManualWirePath.Count < 2 || inputPin.ConnectedTo == null)
        {
            return;
        }

        var path = inputPin.ManualWirePath;
        var outputPin = inputPin.ConnectedTo;

        // Update first point (output pin location)
        path[0] = new Point(outputPin.WorldX, outputPin.WorldY);

        // Update last point (input pin location)
        path[path.Count - 1] = new Point(inputPin.WorldX, inputPin.WorldY);
    }

    public int GetSegmentAtPosition(Point worldPos, int tolerance = 8)
    {
        if (_editingWire?.ManualWirePath == null)
        {
            return -1;
        }

        var path = _editingWire.ManualWirePath;

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (IsPointNearSegment(worldPos, path[i], path[i + 1], tolerance))
            {
                return i;
            }
        }

        return -1;
    }

    public bool AddNodeAtPosition(Point worldPos)
    {
        if (_editingWire?.ManualWirePath == null)
        {
            return false;
        }

        int segmentIndex = GetSegmentAtPosition(worldPos);
        if (segmentIndex < 0)
        {
            return false;
        }

        var path = _editingWire.ManualWirePath;
        var snappedPoint = SnapToGrid(worldPos);

        // Don't add if it's the same as an existing point
        if (snappedPoint == path[segmentIndex] || snappedPoint == path[segmentIndex + 1])
        {
            return false;
        }

        // Insert the new node after the segment's first point
        path.Insert(segmentIndex + 1, snappedPoint);
        return true;
    }

    public bool RemoveNode(int nodeIndex)
    {
        if (_editingWire?.ManualWirePath == null)
        {
            return false;
        }

        var path = _editingWire.ManualWirePath;

        // Can only remove internal nodes (not endpoints)
        // Need at least 2 points to have a valid wire
        if (nodeIndex <= 0 || nodeIndex >= path.Count - 1 || path.Count <= 2)
        {
            return false;
        }

        path.RemoveAt(nodeIndex);
        return true;
    }

    private bool IsPointNearSegment(Point p, Point segStart, Point segEnd, int tolerance)
    {
        float px = p.X, py = p.Y;
        float x1 = segStart.X, y1 = segStart.Y;
        float x2 = segEnd.X, y2 = segEnd.Y;

        float dx = x2 - x1;
        float dy = y2 - y1;
        float lengthSq = dx * dx + dy * dy;

        if (lengthSq == 0)
        {
            // Segment is a point
            float distSq = (px - x1) * (px - x1) + (py - y1) * (py - y1);
            return distSq <= tolerance * tolerance;
        }

        // Project point onto line
        float t = System.Math.Max(0, System.Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSq));
        float projX = x1 + t * dx;
        float projY = y1 + t * dy;

        float distSq2 = (px - projX) * (px - projX) + (py - projY) * (py - projY);
        return distSq2 <= tolerance * tolerance;
    }

    private Point SnapToGrid(Point p)
    {
        int x = (int)(System.Math.Round((double)p.X / _gridSize) * _gridSize);
        int y = (int)(System.Math.Round((double)p.Y / _gridSize) * _gridSize);
        return new Point(x, y);
    }
}
