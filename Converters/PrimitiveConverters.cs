using System.Collections.Generic;
using System.Linq;
using CPUgame.Core.Primitives;
using Microsoft.Xna.Framework;

namespace CPUgame.Converters;

/// <summary>
/// Extension methods for converting between Core primitives and MonoGame types.
/// </summary>
public static class PrimitiveConverters
{
    // Core -> MonoGame conversions

    public static Point ToMonoGame(this Point2 p) => new(p.X, p.Y);

    public static Vector2 ToMonoGame(this Vector2f v) => new(v.X, v.Y);

    public static Rectangle ToMonoGame(this Rect r) => new(r.X, r.Y, r.Width, r.Height);

    public static Color ToMonoGame(this ColorRgba c) => new(c.R, c.G, c.B, c.A);

    public static Matrix ToMonoGame(this Transform2D t)
    {
        // Create a 2D transformation matrix: scale then translate
        // MonoGame uses row-major matrices
        return Matrix.CreateScale(t.Scale) *
               Matrix.CreateTranslation(-t.Offset.X * t.Scale, -t.Offset.Y * t.Scale, 0);
    }

    // MonoGame -> Core conversions

    public static Point2 ToCore(this Point p) => new(p.X, p.Y);

    public static Vector2f ToCore(this Vector2 v) => new(v.X, v.Y);

    public static Rect ToCore(this Rectangle r) => new(r.X, r.Y, r.Width, r.Height);

    public static ColorRgba ToCore(this Color c) => new(c.R, c.G, c.B, c.A);

    // Bulk conversions for collections

    public static List<Point> ToMonoGame(this IEnumerable<Point2> points) =>
        points.Select(p => p.ToMonoGame()).ToList();

    public static List<Point2> ToCore(this IEnumerable<Point> points) =>
        points.Select(p => p.ToCore()).ToList();

    public static List<Vector2> ToMonoGame(this IEnumerable<Vector2f> vectors) =>
        vectors.Select(v => v.ToMonoGame()).ToList();

    public static List<Vector2f> ToCore(this IEnumerable<Vector2> vectors) =>
        vectors.Select(v => v.ToCore()).ToList();
}
