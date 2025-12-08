namespace CPUgame.Core.Primitives;

/// <summary>
/// Platform-agnostic 2D transformation (translation + uniform scale).
/// Used for camera transforms.
/// </summary>
public readonly struct Transform2D : IEquatable<Transform2D>
{
    public readonly Vector2f Offset;
    public readonly float Scale;

    public Transform2D(Vector2f offset, float scale)
    {
        Offset = offset;
        Scale = scale;
    }

    public static Transform2D Identity => new(Vector2f.Zero, 1f);

    /// <summary>
    /// Transform a world point to screen space.
    /// </summary>
    public Vector2f WorldToScreen(Vector2f worldPoint) => (worldPoint - Offset) * Scale;

    /// <summary>
    /// Transform a screen point to world space.
    /// </summary>
    public Vector2f ScreenToWorld(Vector2f screenPoint) => screenPoint / Scale + Offset;

    /// <summary>
    /// Transform a world point to screen space (integer).
    /// </summary>
    public Point2 WorldToScreen(Point2 worldPoint) => (Point2)WorldToScreen((Vector2f)worldPoint);

    /// <summary>
    /// Transform a screen point to world space (integer).
    /// </summary>
    public Point2 ScreenToWorld(Point2 screenPoint) => (Point2)ScreenToWorld((Vector2f)screenPoint);

    /// <summary>
    /// Transform a rectangle from world to screen space.
    /// </summary>
    public Rect WorldToScreen(Rect worldRect)
    {
        var topLeft = WorldToScreen(new Point2(worldRect.X, worldRect.Y));
        var scaledSize = new Point2((int)(worldRect.Width * Scale), (int)(worldRect.Height * Scale));
        return new Rect(topLeft, scaledSize);
    }

    /// <summary>
    /// Create a translation transform.
    /// </summary>
    public static Transform2D CreateTranslation(Vector2f offset) => new(offset, 1f);

    /// <summary>
    /// Create a scale transform.
    /// </summary>
    public static Transform2D CreateScale(float scale) => new(Vector2f.Zero, scale);

    /// <summary>
    /// Combine two transforms.
    /// </summary>
    public static Transform2D operator *(Transform2D a, Transform2D b) =>
        new(a.Offset + b.Offset / a.Scale, a.Scale * b.Scale);

    public bool Equals(Transform2D other) => Offset.Equals(other.Offset) && MathF.Abs(Scale - other.Scale) < 0.0001f;
    public override bool Equals(object? obj) => obj is Transform2D t && Equals(t);
    public override int GetHashCode() => HashCode.Combine(Offset, Scale);
    public override string ToString() => $"Transform2D(Offset: {Offset}, Scale: {Scale:F2})";

    public static bool operator ==(Transform2D a, Transform2D b) => a.Equals(b);
    public static bool operator !=(Transform2D a, Transform2D b) => !a.Equals(b);
}
