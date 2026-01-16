using CPUgame.Core.Circuit;
using CPUgame.Core.Primitives;

namespace CPUgame.Core.Selection;

/// <summary>
/// Manages component selection and selection rectangle
/// </summary>
public class SelectionManager : ISelectionManager
{
    private readonly Circuit.Circuit _circuit;

    // Selection rectangle state
    public bool IsSelecting { get; private set; }
    public Point2 SelectionStart { get; private set; }
    public Point2 SelectionEnd { get; private set; }

    // Dragging state
    public bool IsDraggingSingle { get; private set; }
    public bool IsDraggingMultiple { get; private set; }
    private Component? _draggingComponent;
    private Point2 _dragOffset;
    private Point2 _multiDragStart;
    private Dictionary<Component, Point2>? _multiDragOffsets;
    private bool _hasDragged;
    private Component? _pendingDeselect;

    // Wire selection
    public Pin? SelectedWire { get; set; }

    // Node selection
    private readonly List<WireNode> _selectedNodes = new();
    private Dictionary<WireNode, Point2>? _nodeDragOffsets;

    public SelectionManager(Circuit.Circuit circuit)
    {
        _circuit = circuit;
    }

    public IReadOnlyList<WireNode> GetSelectedNodes() => _selectedNodes;

    public void SetCircuit(Circuit.Circuit circuit)
    {
        // For when circuit is replaced (new/load)
        ClearAll();
    }

    public List<Component> GetSelectedComponents()
    {
        return _circuit.Components.Where(c => c.IsSelected).ToList();
    }

    public bool HasSelection => _circuit.Components.Any(c => c.IsSelected);

    /// <summary>
    /// Select a single component, clearing any previous selection
    /// </summary>
    public void SelectComponent(Component component)
    {
        _circuit.ClearSelection();
        SelectedWire = null;
        component.IsSelected = true;
    }

    /// <summary>
    /// Start selection rectangle
    /// </summary>
    public void StartSelectionRect(Point2 worldPos, bool addToSelection)
    {
        if (!addToSelection)
        {
            _circuit.ClearSelection();
            SelectedWire = null;
            _selectedNodes.Clear();
        }

        IsSelecting = true;
        SelectionStart = worldPos;
        SelectionEnd = worldPos;
    }

    /// <summary>
    /// Update selection rectangle
    /// </summary>
    public void UpdateSelectionRect(Point2 worldPos)
    {
        if (IsSelecting)
        {
            SelectionEnd = worldPos;
        }
    }

    /// <summary>
    /// Complete selection rectangle and select components and wire nodes within
    /// </summary>
    public void CompleteSelectionRect()
    {
        if (!IsSelecting)
        {
            return;
        }

        int minX = Math.Min(SelectionStart.X, SelectionEnd.X);
        int maxX = Math.Max(SelectionStart.X, SelectionEnd.X);
        int minY = Math.Min(SelectionStart.Y, SelectionEnd.Y);
        int maxY = Math.Max(SelectionStart.Y, SelectionEnd.Y);

        // Select components
        foreach (var component in _circuit.Components)
        {
            bool intersects = component.X < maxX && component.X + component.Width > minX &&
                              component.Y < maxY && component.Y + component.Height > minY;

            if (intersects)
            {
                component.IsSelected = true;
            }
        }

        // Select wire nodes (intermediate nodes only)
        foreach (var component in _circuit.Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ManualWirePath != null && input.ManualWirePath.Count > 2)
                {
                    // Check intermediate nodes (indices 1 to Count-2)
                    for (int i = 1; i < input.ManualWirePath.Count - 1; i++)
                    {
                        var node = input.ManualWirePath[i];
                        if (node.X >= minX && node.X <= maxX && node.Y >= minY && node.Y <= maxY)
                        {
                            var wireNode = new WireNode(input, i);
                            if (!_selectedNodes.Any(n => n.Wire == input && n.NodeIndex == i))
                            {
                                _selectedNodes.Add(wireNode);
                            }
                        }
                    }
                }
            }
        }

        IsSelecting = false;
    }

    /// <summary>
    /// Handle click on component
    /// </summary>
    public void HandleComponentClick(Component component, bool addToSelection, Point2 worldMousePos)
    {
        SelectedWire = null;
        _pendingDeselect = null;

        // Check if clicking on an already selected component with multiple selections
        if (component.IsSelected && GetSelectedComponents().Count > 1)
        {
            // Start multi-drag, but remember we might need to deselect on release if no drag occurred
            StartMultiDrag(worldMousePos);
            _pendingDeselect = component;
        }
        else
        {
            if (!addToSelection)
            {
                _circuit.ClearSelection();
                _selectedNodes.Clear();
            }

            component.IsSelected = !component.IsSelected || !addToSelection;
            StartSingleDrag(component, worldMousePos);
        }
    }

    /// <summary>
    /// Handle click on wire
    /// </summary>
    public void HandleWireClick(Pin wire)
    {
        _circuit.ClearSelection();
        SelectedWire = wire;
    }

    /// <summary>
    /// Check if the selected wire is a manual wire (has ManualWirePath).
    /// </summary>
    public bool IsSelectedWireManual => SelectedWire?.ManualWirePath != null && SelectedWire.ManualWirePath.Count >= 2;

    /// <summary>
    /// Handle click on empty space
    /// </summary>
    public void HandleEmptyClick(bool addToSelection)
    {
        if (!addToSelection)
        {
            _circuit.ClearSelection();
            SelectedWire = null;
            _selectedNodes.Clear();
        }
    }

    private void StartSingleDrag(Component component, Point2 worldMousePos)
    {
        IsDraggingSingle = true;
        _draggingComponent = component;
        _dragOffset = new Point2(worldMousePos.X - component.X, worldMousePos.Y - component.Y);

        // Store original positions of selected nodes for dragging
        _multiDragStart = worldMousePos;
        _nodeDragOffsets = new Dictionary<WireNode, Point2>();
        foreach (var node in _selectedNodes)
        {
            _nodeDragOffsets[node] = new Point2(node.X, node.Y);
        }
    }

    private void StartMultiDrag(Point2 worldMousePos)
    {
        IsDraggingMultiple = true;
        _hasDragged = false;
        _multiDragStart = worldMousePos;
        _multiDragOffsets = new Dictionary<Component, Point2>();
        foreach (var selected in GetSelectedComponents())
        {
            _multiDragOffsets[selected] = new Point2(selected.X, selected.Y);
        }

        // Store original positions of selected nodes
        _nodeDragOffsets = new Dictionary<WireNode, Point2>();
        foreach (var node in _selectedNodes)
        {
            _nodeDragOffsets[node] = new Point2(node.X, node.Y);
        }
    }

    /// <summary>
    /// Update dragging position
    /// </summary>
    public void UpdateDrag(Point2 worldMousePos, int gridSize)
    {
        if (IsDraggingSingle && _draggingComponent != null)
        {
            _draggingComponent.X = ((worldMousePos.X - _dragOffset.X) / gridSize) * gridSize;
            _draggingComponent.Y = ((worldMousePos.Y - _dragOffset.Y) / gridSize) * gridSize;

            // Move selected nodes along with the component
            if (_nodeDragOffsets != null && _nodeDragOffsets.Count > 0)
            {
                int deltaX = worldMousePos.X - _multiDragStart.X;
                int deltaY = worldMousePos.Y - _multiDragStart.Y;

                foreach (var kvp in _nodeDragOffsets)
                {
                    var node = kvp.Key;
                    var originalPos = kvp.Value;
                    int newX = ((originalPos.X + deltaX) / gridSize) * gridSize;
                    int newY = ((originalPos.Y + deltaY) / gridSize) * gridSize;
                    node.SetPosition(newX, newY);
                }
            }
        }
        else if (IsDraggingMultiple && _multiDragOffsets != null)
        {
            int deltaX = worldMousePos.X - _multiDragStart.X;
            int deltaY = worldMousePos.Y - _multiDragStart.Y;

            // Mark as dragged if moved beyond a small threshold
            if (Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2)
            {
                _hasDragged = true;
            }

            // Move components
            foreach (var kvp in _multiDragOffsets)
            {
                var comp = kvp.Key;
                var originalPos = kvp.Value;
                comp.X = ((originalPos.X + deltaX) / gridSize) * gridSize;
                comp.Y = ((originalPos.Y + deltaY) / gridSize) * gridSize;
            }

            // Move selected nodes
            if (_nodeDragOffsets != null)
            {
                foreach (var kvp in _nodeDragOffsets)
                {
                    var node = kvp.Key;
                    var originalPos = kvp.Value;
                    int newX = ((originalPos.X + deltaX) / gridSize) * gridSize;
                    int newY = ((originalPos.Y + deltaY) / gridSize) * gridSize;
                    node.SetPosition(newX, newY);
                }
            }
        }
    }

    /// <summary>
    /// End dragging operation
    /// </summary>
    public void EndDrag()
    {
        // If we had a pending deselect and didn't actually drag, deselect only the clicked component
        if (_pendingDeselect != null && !_hasDragged)
        {
            _pendingDeselect.IsSelected = false;
        }

        IsDraggingSingle = false;
        IsDraggingMultiple = false;
        _draggingComponent = null;
        _multiDragOffsets = null;
        _nodeDragOffsets = null;
        _pendingDeselect = null;
        _hasDragged = false;
    }

    public bool IsDragging => IsDraggingSingle || IsDraggingMultiple;

    /// <summary>
    /// Clear all selection state
    /// </summary>
    public void ClearAll()
    {
        _circuit.ClearSelection();
        SelectedWire = null;
        _selectedNodes.Clear();
        IsSelecting = false;
        EndDrag();
    }

    /// <summary>
    /// Delete selected wire
    /// </summary>
    public bool DeleteSelectedWire()
    {
        if (SelectedWire != null)
        {
            SelectedWire.Disconnect();
            SelectedWire = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Delete selected components
    /// </summary>
    public int DeleteSelectedComponents()
    {
        var toDelete = GetSelectedComponents();
        foreach (var component in toDelete)
        {
            _circuit.RemoveComponent(component);
        }
        return toDelete.Count;
    }
}
