namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DisplayMode</c>. See <see cref="Microsoft.Xna.Framework.Color"/>
/// for why this duplicates <see cref="CNA.Graphics.DisplayMode"/> rather than subclassing it
/// (structs cannot inherit).</summary>
public readonly struct DisplayMode : IEquatable<DisplayMode>
{
    internal DisplayMode(int width, int height, float aspectRatio, SurfaceFormat format)
    {
        Width = width;
        Height = height;
        AspectRatio = aspectRatio;
        Format = format;
    }

    public int Width { get; }

    public int Height { get; }

    public float AspectRatio { get; }

    public SurfaceFormat Format { get; }

    public Rectangle TitleSafeArea => new(Width / 10, Height / 10, Width - (Width / 5), Height - (Height / 5));

    internal static DisplayMode FromFramework(CNA.Graphics.DisplayMode source) =>
        new(source.Width, source.Height, source.AspectRatio, (SurfaceFormat)(int)source.Format);

    public bool Equals(DisplayMode other) => Width == other.Width && Height == other.Height && Format == other.Format;

    public override bool Equals(object? obj) => obj is DisplayMode other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Width, Height, Format);

    public static bool operator ==(DisplayMode a, DisplayMode b) => a.Equals(b);

    public static bool operator !=(DisplayMode a, DisplayMode b) => !a.Equals(b);

    public override string ToString() => $"{{Width:{Width} Height:{Height} Format:{Format} AspectRatio:{AspectRatio}}}";
}
