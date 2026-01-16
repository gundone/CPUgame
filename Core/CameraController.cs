using System;
using CPUgame.Core.Primitives;
using Microsoft.Xna.Framework;

namespace CPUgame.Core;

/// <summary>
/// Handles camera zoom and panning for the circuit view
/// </summary>
public class CameraController : ICameraController
{
    public float Zoom { get; private set; } = 1.0f;
    public Vector2 Offset { get; private set; } = Vector2.Zero;
    public Vector2 ViewportOffset { get; set; } = Vector2.Zero;

    public float MinZoom { get; set; } = 0.25f;
    public float MaxZoom { get; set; } = 3.0f;
    public float ZoomStep { get; set; } = 0.1f;

    private bool _isPanning;
    private Point2 _panStartMouse;
    private Vector2 _panStartCamera;

    /// <summary>
    /// Handle zoom input (scroll wheel or pinch)
    /// </summary>
    public void HandleZoom(int scrollDelta, Point2 screenMousePos, Func<Point2, Vector2> screenToWorld)
    {
        if (scrollDelta == 0)
        {
            return;
        }

        float oldZoom = Zoom;
        var worldMouseBefore = screenToWorld(screenMousePos);

        if (scrollDelta > 0)
        {
            Zoom = Math.Min(Zoom + ZoomStep, MaxZoom);
        }
        else
        {
            Zoom = Math.Max(Zoom - ZoomStep, MinZoom);
        }

        // Zoom towards mouse position
        if (Math.Abs(Zoom - oldZoom) > 0.001f)
        {
            var worldMouseAfter = screenToWorld(screenMousePos);
            Offset += worldMouseBefore - worldMouseAfter;
        }
    }

    /// <summary>
    /// Handle pinch zoom for touch input
    /// </summary>
    public void HandlePinchZoom(float scale, Point2 screenCenter, Func<Point2, Vector2> screenToWorld)
    {
        if (Math.Abs(scale - 1f) < 0.001f)
        {
            return;
        }

        float oldZoom = Zoom;
        var worldCenterBefore = screenToWorld(screenCenter);

        Zoom = Math.Clamp(Zoom * scale, MinZoom, MaxZoom);

        if (Math.Abs(Zoom - oldZoom) > 0.001f)
        {
            var worldCenterAfter = screenToWorld(screenCenter);
            Offset += worldCenterBefore - worldCenterAfter;
        }
    }

    /// <summary>
    /// Start panning operation
    /// </summary>
    public void StartPan(Point2 screenMousePos)
    {
        _isPanning = true;
        _panStartMouse = screenMousePos;
        _panStartCamera = Offset;
    }

    /// <summary>
    /// Update pan position while dragging
    /// </summary>
    public void UpdatePan(Point2 screenMousePos)
    {
        if (!_isPanning)
        {
            return;
        }

        float deltaX = (screenMousePos.X - _panStartMouse.X) / Zoom;
        float deltaY = (screenMousePos.Y - _panStartMouse.Y) / Zoom;
        Offset = _panStartCamera - new Vector2(deltaX, deltaY);
    }

    /// <summary>
    /// End panning operation
    /// </summary>
    public void EndPan()
    {
        _isPanning = false;
    }

    public bool IsPanning => _isPanning;

    /// <summary>
    /// Get the camera transformation matrix
    /// </summary>
    public Matrix GetTransform()
    {
        return Matrix.CreateTranslation(-Offset.X, -Offset.Y, 0) *
               Matrix.CreateScale(Zoom, Zoom, 1) *
               Matrix.CreateTranslation(ViewportOffset.X, ViewportOffset.Y, 0);
    }

    /// <summary>
    /// Convert screen coordinates to world coordinates
    /// </summary>
    public Vector2 ScreenToWorld(Point2 screenPos)
    {
        return new Vector2(
            (screenPos.X - ViewportOffset.X) / Zoom + Offset.X,
            (screenPos.Y - ViewportOffset.Y) / Zoom + Offset.Y);
    }

    /// <summary>
    /// Convert screen coordinates to world point
    /// </summary>
    public Point2 ScreenToWorldPoint(Point2 screenPos)
    {
        var world = ScreenToWorld(screenPos);
        return new Point2((int)world.X, (int)world.Y);
    }
}
