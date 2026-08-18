namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>TitleLocation</c>. A thin forwarding facade -- static classes
/// cannot be subclassed, the same reason <see cref="FrameworkDispatcher"/> forwards rather than
/// inherits. Nothing needs re-typing: the one member is a <see cref="string"/>.</summary>
public static class TitleLocation
{
    /// <inheritdoc cref="CNA.TitleLocation.Path"/>
    public static string Path => CNA.TitleLocation.Path;
}
