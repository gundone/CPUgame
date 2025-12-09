using System;
using CPUgame.Core.Primitives;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

/// <summary>
/// Interface for camera zoom and panning operations
/// </summary>
public interface ICameraController
{
    float Zoom { get; }
    Vector2 Offset { get; }
    float MinZoom { get; set; }
    float MaxZoom { get; set; }
    float ZoomStep { get; set; }
    bool IsPanning { get; }

    void HandleZoom(int scrollDelta, Point2 screenMousePos, Func<Point2, Vector2> screenToWorld);
    void HandlePinchZoom(float scale, Point2 screenCenter, Func<Point2, Vector2> screenToWorld);
    void StartPan(Point2 screenMousePos);
    void UpdatePan(Point2 screenMousePos);
    void EndPan();
    Matrix GetTransform();
    Vector2 ScreenToWorld(Point2 screenPos);
    Point2 ScreenToWorldPoint(Point2 screenPos);
}
