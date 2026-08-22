using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA graphics-adapter facade. CNA's adapter is held privately so static adapter discovery and
/// every returned member remain in the Microsoft.Xna.Framework.Graphics type system.
/// </summary>
public sealed class GraphicsAdapter
{
    private readonly CNA.Graphics.GraphicsAdapter _framework;

    private GraphicsAdapter(CNA.Graphics.GraphicsAdapter framework)
    {
        _framework = framework;
    }

    internal CNA.Graphics.GraphicsAdapter Framework => _framework;

    internal static GraphicsAdapter FromFramework(CNA.Graphics.GraphicsAdapter framework) =>
        new(framework ?? throw new ArgumentNullException(nameof(framework)));

    public static ReadOnlyCollection<GraphicsAdapter> Adapters =>
        new(CNA.Graphics.GraphicsAdapter.Adapters.Select(FromFramework).ToList());

    public static GraphicsAdapter DefaultAdapter => FromFramework(CNA.Graphics.GraphicsAdapter.DefaultAdapter);

    public static bool UseNullDevice
    {
        get => CNA.Graphics.GraphicsAdapter.UseNullDevice;
        set => CNA.Graphics.GraphicsAdapter.UseNullDevice = value;
    }

    public static bool UseReferenceDevice
    {
        get => CNA.Graphics.GraphicsAdapter.UseReferenceDevice;
        set => CNA.Graphics.GraphicsAdapter.UseReferenceDevice = value;
    }

    public int DeviceId => _framework.DeviceId;

    public string DeviceName => _framework.DeviceName;

    public string Description => _framework.Description;

    public bool IsDefaultAdapter => _framework.IsDefaultAdapter;

    public bool IsWideScreen => _framework.IsWideScreen;

    public IntPtr MonitorHandle => _framework.MonitorHandle;

    public int Revision => _framework.Revision;

    public int SubSystemId => _framework.SubSystemId;

    public int VendorId => _framework.VendorId;

    public DisplayMode CurrentDisplayMode => DisplayMode.FromFramework(_framework.CurrentDisplayMode);

    public DisplayModeCollection SupportedDisplayModes => DisplayModeCollection.FromFramework(_framework.SupportedDisplayModes);

    public bool IsProfileSupported(GraphicsProfile graphicsProfile) =>
        _framework.IsProfileSupported((CNA.Graphics.GraphicsProfile)(int)graphicsProfile);

    /// <summary>Re-typed: <c>GraphicsProfile</c>, <c>SurfaceFormat</c> and <c>DepthFormat</c> are
    /// all separate enums per namespace.</summary>
    public bool QueryRenderTargetFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount)
    {
        bool exact = _framework.QueryRenderTargetFormat(
            (CNA.Graphics.GraphicsProfile)(int)graphicsProfile,
            (CNA.Graphics.SurfaceFormat)(int)format,
            (CNA.Graphics.DepthFormat)(int)depthFormat,
            multiSampleCount,
            out CNA.Graphics.SurfaceFormat nativeFormat,
            out CNA.Graphics.DepthFormat nativeDepth,
            out selectedMultiSampleCount);

        selectedFormat = (SurfaceFormat)(int)nativeFormat;
        selectedDepthFormat = (DepthFormat)(int)nativeDepth;
        return exact;
    }

    /// <summary>See <see cref="QueryRenderTargetFormat"/>.</summary>
    public bool QueryBackBufferFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount)
    {
        bool exact = _framework.QueryBackBufferFormat(
            (CNA.Graphics.GraphicsProfile)(int)graphicsProfile,
            (CNA.Graphics.SurfaceFormat)(int)format,
            (CNA.Graphics.DepthFormat)(int)depthFormat,
            multiSampleCount,
            out CNA.Graphics.SurfaceFormat nativeFormat,
            out CNA.Graphics.DepthFormat nativeDepth,
            out selectedMultiSampleCount);

        selectedFormat = (SurfaceFormat)(int)nativeFormat;
        selectedDepthFormat = (DepthFormat)(int)nativeDepth;
        return exact;
    }
}
