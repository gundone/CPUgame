namespace CPUgame.Core.Primitives;

/// <summary>
/// Platform-agnostic 2D integer point.
/// </summary>
public readonly struct Point2 : IEquatable<Point2>
{
    public readonly int X;
    public readonly int Y;

    public Point2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point2 Zero => new(0, 0);

    public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Point2 operator *(Point2 a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static Point2 operator /(Point2 a, int scalar) => new(a.X / scalar, a.Y / scalar);
    public static bool operator ==(Point2 a, Point2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Point2 a, Point2 b) => !(a == b);

    public bool Equals(Point2 other) => this == other;
    public override bool Equals(object? obj) => obj is Point2 p && Equals(p);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";

    public Vector2f ToVector2f() => new(X, Y);

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}
