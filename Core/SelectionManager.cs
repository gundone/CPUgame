using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

/// <summary>
/// Manages component selection and selection rectangle
/// </summary>
public class SelectionManager
{
    private readonly Circuit _circuit;

    // Selection rectangle state
    public bool IsSelecting { get; private set; }
    public Point SelectionStart { get; private set; }
    public Point SelectionEnd { get; private set; }

    // Dragging state
    public bool IsDraggingSingle { get; private set; }
    public bool IsDraggingMultiple { get; private set; }
    private Component? _draggingComponent;
    private Point _dragOffset;
    private Point _multiDragStart;
    private Dictionary<Component, Point>? _multiDragOffsets;

    // Wire selection
    public Pin? SelectedWire { get; set; }

    public SelectionManager(Circuit circuit)
    {
        _circuit = circuit;
    }

    public void SetCircuit(Circuit circuit)
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
    public void StartSelectionRect(Point worldPos, bool addToSelection)
    {
        if (!addToSelection)
        {
            _circuit.ClearSelection();
            SelectedWire = null;
        }

        IsSelecting = true;
        SelectionStart = worldPos;
        SelectionEnd = worldPos;
    }

    /// <summary>
    /// Update selection rectangle
    /// </summary>
    public void UpdateSelectionRect(Point worldPos)
    {
        if (IsSelecting)
        {
            SelectionEnd = worldPos;
        }
    }

    /// <summary>
    /// Complete selection rectangle and select components within
    /// </summary>
    public void CompleteSelectionRect()
    {
        if (!IsSelecting) return;

        int minX = Math.Min(SelectionStart.X, SelectionEnd.X);
        int maxX = Math.Max(SelectionStart.X, SelectionEnd.X);
        int minY = Math.Min(SelectionStart.Y, SelectionEnd.Y);
        int maxY = Math.Max(SelectionStart.Y, SelectionEnd.Y);

        foreach (var component in _circuit.Components)
        {
            bool intersects = component.X < maxX && component.X + component.Width > minX &&
                              component.Y < maxY && component.Y + component.Height > minY;

            if (intersects)
            {
                component.IsSelected = true;
            }
        }

        IsSelecting = false;
    }

    /// <summary>
    /// Handle click on component
    /// </summary>
    public void HandleComponentClick(Component component, bool addToSelection, Point worldMousePos)
    {
        SelectedWire = null;

        // Check if clicking on an already selected component with multiple selections
        if (component.IsSelected && GetSelectedComponents().Count > 1)
        {
            StartMultiDrag(worldMousePos);
        }
        else
        {
            if (!addToSelection)
            {
                _circuit.ClearSelection();
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
        }
    }

    private void StartSingleDrag(Component component, Point worldMousePos)
    {
        IsDraggingSingle = true;
        _draggingComponent = component;
        _dragOffset = new Point(worldMousePos.X - component.X, worldMousePos.Y - component.Y);
    }

    private void StartMultiDrag(Point worldMousePos)
    {
        IsDraggingMultiple = true;
        _multiDragStart = worldMousePos;
        _multiDragOffsets = new Dictionary<Component, Point>();
        foreach (var selected in GetSelectedComponents())
        {
            _multiDragOffsets[selected] = new Point(selected.X, selected.Y);
        }
    }

    /// <summary>
    /// Update dragging position
    /// </summary>
    public void UpdateDrag(Point worldMousePos, int gridSize)
    {
        if (IsDraggingSingle && _draggingComponent != null)
        {
            _draggingComponent.X = ((worldMousePos.X - _dragOffset.X) / gridSize) * gridSize;
            _draggingComponent.Y = ((worldMousePos.Y - _dragOffset.Y) / gridSize) * gridSize;
        }
        else if (IsDraggingMultiple && _multiDragOffsets != null)
        {
            int deltaX = worldMousePos.X - _multiDragStart.X;
            int deltaY = worldMousePos.Y - _multiDragStart.Y;

            foreach (var kvp in _multiDragOffsets)
            {
                var comp = kvp.Key;
                var originalPos = kvp.Value;
                comp.X = ((originalPos.X + deltaX) / gridSize) * gridSize;
                comp.Y = ((originalPos.Y + deltaY) / gridSize) * gridSize;
            }
        }
    }

    /// <summary>
    /// End dragging operation
    /// </summary>
    public void EndDrag()
    {
        IsDraggingSingle = false;
        IsDraggingMultiple = false;
        _draggingComponent = null;
        _multiDragOffsets = null;
    }

    public bool IsDragging => IsDraggingSingle || IsDraggingMultiple;

    /// <summary>
    /// Clear all selection state
    /// </summary>
    public void ClearAll()
    {
        _circuit.ClearSelection();
        SelectedWire = null;
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
