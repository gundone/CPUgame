namespace CPUgame.Core.Primitives;

/// <summary>
/// Platform-agnostic 2D floating-point vector.
/// </summary>
public readonly struct Vector2f : IEquatable<Vector2f>
{
    public readonly float X;
    public readonly float Y;

    public Vector2f(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2f Zero => new(0, 0);
    public static Vector2f One => new(1, 1);
    public static Vector2f UnitX => new(1, 0);
    public static Vector2f UnitY => new(0, 1);

    public float Length => MathF.Sqrt(X * X + Y * Y);
    public float LengthSquared => X * X + Y * Y;

    public Vector2f Normalized
    {
        get
        {
            float len = Length;
            return len > 0 ? new(X / len, Y / len) : Zero;
        }
    }

    public static Vector2f operator +(Vector2f a, Vector2f b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2f operator -(Vector2f a, Vector2f b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2f operator -(Vector2f v) => new(-v.X, -v.Y);
    public static Vector2f operator *(Vector2f v, float s) => new(v.X * s, v.Y * s);
    public static Vector2f operator *(float s, Vector2f v) => new(v.X * s, v.Y * s);
    public static Vector2f operator /(Vector2f v, float s) => new(v.X / s, v.Y / s);

    public static float Distance(Vector2f a, Vector2f b) => (a - b).Length;
    public static float DistanceSquared(Vector2f a, Vector2f b) => (a - b).LengthSquared;
    public static float Dot(Vector2f a, Vector2f b) => a.X * b.X + a.Y * b.Y;
    public static Vector2f Lerp(Vector2f a, Vector2f b, float t) => a + (b - a) * t;
    public static Vector2f Min(Vector2f a, Vector2f b) => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));
    public static Vector2f Max(Vector2f a, Vector2f b) => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));

    public static explicit operator Point2(Vector2f v) => new((int)v.X, (int)v.Y);
    public static implicit operator Vector2f(Point2 p) => new(p.X, p.Y);

    public bool Equals(Vector2f other) => MathF.Abs(X - other.X) < 0.0001f && MathF.Abs(Y - other.Y) < 0.0001f;
    public override bool Equals(object? obj) => obj is Vector2f v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:F2}, {Y:F2})";

    public Point2 ToPoint2() => new((int)X, (int)Y);

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }
}
