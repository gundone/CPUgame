namespace CPUgame.Core.Primitives;

/// <summary>
/// Platform-agnostic rectangle.
/// </summary>
public readonly struct Rect : IEquatable<Rect>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public Rect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rect(Point2 location, Point2 size)
    {
        X = location.X;
        Y = location.Y;
        Width = size.X;
        Height = size.Y;
    }

    public static Rect Empty => new(0, 0, 0, 0);

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public Point2 Location => new(X, Y);
    public Point2 Size => new(Width, Height);
    public Point2 Center => new(X + Width / 2, Y + Height / 2);

    public bool IsEmpty => Width == 0 || Height == 0;

    public bool Contains(Point2 p) => p.X >= X && p.X < Right && p.Y >= Y && p.Y < Bottom;
    public bool Contains(int px, int py) => px >= X && px < Right && py >= Y && py < Bottom;
    public bool Contains(Rect other) => other.X >= X && other.Right <= Right && other.Y >= Y && other.Bottom <= Bottom;

    public bool Intersects(Rect other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public static Rect Intersect(Rect a, Rect b)
    {
        int x = Math.Max(a.X, b.X);
        int y = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        if (right > x && bottom > y)
        {
            return new Rect(x, y, right - x, bottom - y);
        }

        return Empty;
    }

    public static Rect Union(Rect a, Rect b)
    {
        int x = Math.Min(a.X, b.X);
        int y = Math.Min(a.Y, b.Y);
        int right = Math.Max(a.Right, b.Right);
        int bottom = Math.Max(a.Bottom, b.Bottom);
        return new Rect(x, y, right - x, bottom - y);
    }

    public Rect Inflate(int horizontal, int vertical) =>
        new(X - horizontal, Y - vertical, Width + horizontal * 2, Height + vertical * 2);

    public Rect Offset(int dx, int dy) => new(X + dx, Y + dy, Width, Height);
    public Rect Offset(Point2 delta) => new(X + delta.X, Y + delta.Y, Width, Height);

    public bool Equals(Rect other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    public override bool Equals(object? obj) => obj is Rect r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public override string ToString() => $"({X}, {Y}, {Width}, {Height})";

    public static bool operator ==(Rect a, Rect b) => a.Equals(b);
    public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
}
