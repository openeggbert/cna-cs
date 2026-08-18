using System.Collections;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DisplayModeCollection</c>: the display modes one
/// <see cref="GraphicsAdapter"/> reports. An immutable snapshot taken when the adapter was asked,
/// not a live view -- call <see cref="GraphicsAdapter.SupportedDisplayModes"/> again after
/// <see cref="GraphicsAdapter.Refresh"/> to see changes.</summary>
public class DisplayModeCollection : IEnumerable<DisplayMode>
{
    private readonly DisplayMode[] _modes;

    internal DisplayModeCollection(DisplayMode[] modes)
    {
        _modes = modes;
    }

    public int Count => _modes.Length;

    public DisplayMode this[int index] => _modes[index];

    /// <summary>Matches real XNA's format-filtered indexer.</summary>
    public IEnumerable<DisplayMode> this[SurfaceFormat format] => _modes.Where(m => m.Format == format);

    public IEnumerator<DisplayMode> GetEnumerator() => ((IEnumerable<DisplayMode>)_modes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
