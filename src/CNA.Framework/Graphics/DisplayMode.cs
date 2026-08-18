using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DisplayMode</c>. <see cref="AspectRatio"/> is computed by native
/// (<c>cna_display_mode_init</c>) rather than derived here, so a mode obtained from an adapter and
/// one built through the constructor agree exactly.</summary>
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

    /// <summary>Matches real XNA's <c>TitleSafeArea</c>: the inset region guaranteed visible on a
    /// television. XNA's own rule is a 4:3-era 80% centred box, reproduced here rather than bound,
    /// since it is arithmetic on this struct's own fields with no native counterpart.</summary>
    public Rectangle TitleSafeArea => new(Width / 10, Height / 10, Width - (Width / 5), Height - (Height / 5));

    internal static DisplayMode FromNative(in CnaDisplayMode native) =>
        new(native.Width, native.Height, native.AspectRatio, (SurfaceFormat)native.Format);

    /// <summary>Matches real XNA's equality, which compares width, height and format -- but not
    /// <see cref="AspectRatio"/>, since that is derived from the first two and comparing a float
    /// would only add a way for two otherwise-identical modes to differ.</summary>
    public bool Equals(DisplayMode other) =>
        Width == other.Width && Height == other.Height && Format == other.Format;

    public override bool Equals(object? obj) => obj is DisplayMode other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Width, Height, Format);

    public static bool operator ==(DisplayMode a, DisplayMode b) => a.Equals(b);

    public static bool operator !=(DisplayMode a, DisplayMode b) => !a.Equals(b);

    public override string ToString() => $"{{Width:{Width} Height:{Height} Format:{Format} AspectRatio:{AspectRatio}}}";
}
