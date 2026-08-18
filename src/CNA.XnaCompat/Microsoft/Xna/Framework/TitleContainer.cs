namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>TitleContainer</c>. Forwards to
/// <see cref="CNA.TitleContainer"/>, which is pure BCL -- see that type for why.</summary>
public static class TitleContainer
{
    public static Stream OpenStream(string name) => CNA.TitleContainer.OpenStream(name);
}
