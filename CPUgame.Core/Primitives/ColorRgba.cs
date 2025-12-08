namespace CPUgame.Core.Primitives;

/// <summary>
/// Platform-agnostic RGBA color.
/// </summary>
public readonly struct ColorRgba : IEquatable<ColorRgba>
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public ColorRgba(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public ColorRgba(int r, int g, int b, int a = 255)
        : this((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255), (byte)Math.Clamp(a, 0, 255))
    {
    }

    public ColorRgba(uint packedValue)
    {
        R = (byte)(packedValue & 0xFF);
        G = (byte)((packedValue >> 8) & 0xFF);
        B = (byte)((packedValue >> 16) & 0xFF);
        A = (byte)((packedValue >> 24) & 0xFF);
    }

    // Common colors
    public static ColorRgba White => new(255, 255, 255);
    public static ColorRgba Black => new(0, 0, 0);
    public static ColorRgba Transparent => new(0, 0, 0, 0);
    public static ColorRgba Red => new(255, 0, 0);
    public static ColorRgba Green => new(0, 255, 0);
    public static ColorRgba Blue => new(0, 0, 255);
    public static ColorRgba Yellow => new(255, 255, 0);
    public static ColorRgba Cyan => new(0, 255, 255);
    public static ColorRgba Magenta => new(255, 0, 255);
    public static ColorRgba Gray => new(128, 128, 128);

    public ColorRgba WithAlpha(byte alpha) => new(R, G, B, alpha);
    public ColorRgba WithAlpha(float alpha) => new(R, G, B, (byte)(Math.Clamp(alpha, 0f, 1f) * 255));

    public uint ToPackedValue() => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    public static ColorRgba Lerp(ColorRgba a, ColorRgba b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new ColorRgba(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    public static ColorRgba operator *(ColorRgba color, float scalar)
    {
        return new ColorRgba(
            (byte)Math.Clamp(color.R * scalar, 0, 255),
            (byte)Math.Clamp(color.G * scalar, 0, 255),
            (byte)Math.Clamp(color.B * scalar, 0, 255),
            color.A);
    }

    public bool Equals(ColorRgba other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override bool Equals(object? obj) => obj is ColorRgba c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override string ToString() => $"rgba({R}, {G}, {B}, {A})";

    public static bool operator ==(ColorRgba a, ColorRgba b) => a.Equals(b);
    public static bool operator !=(ColorRgba a, ColorRgba b) => !a.Equals(b);
}
