namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA presentation settings. The CNA value is private implementation state so an XNA value can
/// keep its own exact type and writable <see cref="DeviceWindowHandle"/> contract.
/// </summary>
public class PresentationParameters
{
    private readonly CNA.Graphics.PresentationParameters _framework;
    private nint _deviceWindowHandle;

    public PresentationParameters()
    {
        _framework = new CNA.Graphics.PresentationParameters();
    }

    internal PresentationParameters(CNA.Graphics.PresentationParameters copyFrom)
    {
        ArgumentNullException.ThrowIfNull(copyFrom);
        _framework = copyFrom.Clone();
    }

    private PresentationParameters(CNA.Graphics.PresentationParameters copyFrom, nint deviceWindowHandle)
    {
        ArgumentNullException.ThrowIfNull(copyFrom);
        _framework = copyFrom.Clone();
        _deviceWindowHandle = deviceWindowHandle;
    }

    public SurfaceFormat BackBufferFormat
    {
        get => (SurfaceFormat)(int)_framework.BackBufferFormat;
        set => _framework.BackBufferFormat = (CNA.Graphics.SurfaceFormat)(int)value;
    }

    public int BackBufferHeight
    {
        get => _framework.BackBufferHeight;
        set => _framework.BackBufferHeight = value;
    }

    public int BackBufferWidth
    {
        get => _framework.BackBufferWidth;
        set => _framework.BackBufferWidth = value;
    }

    public DepthFormat DepthStencilFormat
    {
        get => (DepthFormat)(int)_framework.DepthStencilFormat;
        set => _framework.DepthStencilFormat = (CNA.Graphics.DepthFormat)(int)value;
    }

    public nint DeviceWindowHandle
    {
        get => _deviceWindowHandle;
        set => _deviceWindowHandle = value;
    }

    public DisplayOrientation DisplayOrientation
    {
        get => (DisplayOrientation)(int)_framework.DisplayOrientation;
        set => _framework.DisplayOrientation = (CNA.DisplayOrientation)(int)value;
    }

    public bool IsFullScreen
    {
        get => _framework.IsFullScreen;
        set => _framework.IsFullScreen = value;
    }

    public int MultiSampleCount
    {
        get => _framework.MultiSampleCount;
        set => _framework.MultiSampleCount = value;
    }

    public PresentInterval PresentationInterval
    {
        get => (PresentInterval)(int)_framework.PresentationInterval;
        set => _framework.PresentationInterval = (CNA.Graphics.PresentInterval)(int)value;
    }

    public RenderTargetUsage RenderTargetUsage
    {
        get => (RenderTargetUsage)(int)_framework.RenderTargetUsage;
        set => _framework.RenderTargetUsage = (CNA.Graphics.RenderTargetUsage)(int)value;
    }

    public Rectangle Bounds => new(0, 0, BackBufferWidth, BackBufferHeight);

    public PresentationParameters Clone() => new(_framework, _deviceWindowHandle);

    internal CNA.Graphics.PresentationParameters Framework => _framework;
}
