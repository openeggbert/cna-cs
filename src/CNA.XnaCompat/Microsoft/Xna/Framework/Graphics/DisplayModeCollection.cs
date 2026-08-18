using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DisplayModeCollection</c>. Duplicates rather than subclasses,
/// because its element type differs per namespace (see <see cref="DisplayMode"/>).</summary>
public class DisplayModeCollection : IEnumerable<DisplayMode>
{
    private readonly DisplayMode[] _modes;

    internal DisplayModeCollection(DisplayMode[] modes)
    {
        _modes = modes;
    }

    public int Count => _modes.Length;

    public DisplayMode this[int index] => _modes[index];

    public IEnumerable<DisplayMode> this[SurfaceFormat format] => _modes.Where(m => m.Format == format);

    public IEnumerator<DisplayMode> GetEnumerator() => ((IEnumerable<DisplayMode>)_modes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static DisplayModeCollection FromFramework(CNA.Graphics.DisplayModeCollection source)
    {
        var modes = new DisplayMode[source.Count];
        for (int i = 0; i < modes.Length; i++)
        {
            modes[i] = DisplayMode.FromFramework(source[i]);
        }

        return new DisplayModeCollection(modes);
    }
}
