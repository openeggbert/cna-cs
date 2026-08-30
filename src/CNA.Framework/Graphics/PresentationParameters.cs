using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>PresentationParameters</c>: how a <see cref="GraphicsDevice"/> presents
/// its back buffer.
///
/// A mutable managed wrapper over the native <c>CNA_PresentationParameters</c> value, seeded from
/// <c>cna_presentation_parameters_init</c> so a default-constructed instance carries whatever the
/// engine considers default rather than C# zeroes. Assigning it to
/// <see cref="GraphicsDevice.PresentationParameters"/> is what applies it.
///
/// The native struct also carries <c>headless_ext</c>, a CNA extension with no real-XNA
/// counterpart, deliberately not surfaced here -- same fidelity rule as
/// <c>TouchLocation.pressure</c>.
/// </summary>
public class PresentationParameters
{
    private CnaPresentationParameters _native;

    public PresentationParameters()
    {
        CnaResult result = Native.cna_presentation_parameters_init(out _native);
        CnaException.ThrowIfFailed(result, nameof(PresentationParameters));
    }

    private PresentationParameters(CnaPresentationParameters native)
    {
        _native = native;
    }

    protected PresentationParameters(PresentationParameters copyFrom)
    {
        ArgumentNullException.ThrowIfNull(copyFrom);
        _native = copyFrom._native;
    }

    public SurfaceFormat BackBufferFormat
    {
        get => (SurfaceFormat)_native.BackBufferFormat;
        set => _native.BackBufferFormat = (uint)value;
    }

    public int BackBufferWidth
    {
        get => _native.BackBufferWidth;
        set => _native.BackBufferWidth = value;
    }

    public int BackBufferHeight
    {
        get => _native.BackBufferHeight;
        set => _native.BackBufferHeight = value;
    }

    public DepthFormat DepthStencilFormat
    {
        get => (DepthFormat)_native.DepthStencilFormat;
        set => _native.DepthStencilFormat = (uint)value;
    }

    public int MultiSampleCount
    {
        get => _native.MultiSampleCount;
        set => _native.MultiSampleCount = value;
    }

    public PresentInterval PresentationInterval
    {
        get => (PresentInterval)_native.PresentationInterval;
        set => _native.PresentationInterval = (uint)value;
    }

    public DisplayOrientation DisplayOrientation
    {
        get => (DisplayOrientation)_native.DisplayOrientation;
        set => _native.DisplayOrientation = (uint)value;
    }

    public RenderTargetUsage RenderTargetUsage
    {
        get => (RenderTargetUsage)_native.RenderTargetUsage;
        set => _native.RenderTargetUsage = (uint)value;
    }

    public bool IsFullScreen
    {
        get => _native.IsFullScreen != 0;
        set => _native.IsFullScreen = (byte)(value ? 1 : 0);
    }

    /// <summary>Matches real XNA's <c>Bounds</c>: the back buffer as a rectangle at the
    /// origin.</summary>
    public Rectangle Bounds
    {
        get
        {
            CnaResult result = Native.cna_presentation_parameters_get_bounds(in _native, out CnaRectangle bounds);
            CnaException.ThrowIfFailed(result, nameof(Bounds));
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }

    /// <summary>
    /// The native window this device presents into. Matches real XNA's <c>DeviceWindowHandle</c>.
    ///
    /// Always zero, and read-only. <c>display.h</c> has
    /// <c>cna_graphics_device_get_native_window_handle</c>, which answers for a *device*; this is a
    /// standalone value type with no device to ask. Setting it has no route at all -- in this ABI
    /// the window belongs to the game, not to a presentation parameter. Present rather than omitted
    /// so ported XNA source compiles.
    /// </summary>
    public nint DeviceWindowHandle => 0;

    public PresentationParameters Clone()
    {
        CnaResult result = Native.cna_presentation_parameters_clone(in _native, out CnaPresentationParameters clone);
        CnaException.ThrowIfFailed(result, nameof(Clone));
        return new PresentationParameters(clone);
    }

    internal CnaPresentationParameters ToNative() => _native;

    internal static PresentationParameters FromNative(CnaPresentationParameters native) => new(native);
}
