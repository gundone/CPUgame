using CPUgame.Core.Circuit;
using CPUgame.Core.Primitives;

namespace CPUgame.Core.Selection;

/// <summary>
/// Interface for managing component selection and selection rectangle
/// </summary>
public interface ISelectionManager
{
    bool IsSelecting { get; }
    Point2 SelectionStart { get; }
    Point2 SelectionEnd { get; }
    bool IsDraggingSingle { get; }
    bool IsDraggingMultiple { get; }
    bool IsDragging { get; }
    Pin? SelectedWire { get; set; }
    bool HasSelection { get; }
    bool IsSelectedWireManual { get; }

    void SetCircuit(Circuit.Circuit circuit);
    List<Component> GetSelectedComponents();
    IReadOnlyList<WireNode> GetSelectedNodes();
    void SelectComponent(Component component);
    void StartSelectionRect(Point2 worldPos, bool addToSelection);
    void UpdateSelectionRect(Point2 worldPos);
    void CompleteSelectionRect();
    void HandleComponentClick(Component component, bool addToSelection, Point2 worldMousePos);
    void HandleWireClick(Pin wire);
    void HandleEmptyClick(bool addToSelection);
    void UpdateDrag(Point2 worldMousePos, int gridSize);
    void EndDrag();
    void ClearAll();
    bool DeleteSelectedWire();
    int DeleteSelectedComponents();
}
