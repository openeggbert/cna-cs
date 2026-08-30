using Microsoft.Xna.Framework.Graphics;

namespace CNA.XnaCompat.Extensions;

/// <summary>Capabilities reported by CNA renderers beyond the XNA 4.0 contract.</summary>
public enum CnaGraphicsCapability : uint
{
    ThreeD = 0,
    DepthStencilBuffer = 1,
    MultiSampleAntiAliasing = 2,
    MultipleRenderTargets = 3,
    AnisotropicFiltering = 4,
    WireFrame = 5,
    OcclusionQuery = 6,
    CustomEffects = 7,
    Texture3D = 8,
    MultiStreamVertexInput = 9,
    Instancing = 10,
    StencilBuffer = 11,
    AdditiveBlending = 12,
    CompiledEffects = 13,
    FloatRenderTargets = 14,
    HalfFloatRenderTargets = 15,
    HalfFloatTextureLinearFiltering = 16,
    ComputeShaders = 17,
    IndirectDraw = 18,
}

/// <summary>
/// CNA-specific renderer diagnostics kept outside the strict XNA-compatible namespace.
/// </summary>
public static class CnaGraphicsDeviceExtensions
{
    /// <summary>Returns whether the active CNA renderer advertises a capability.</summary>
    public static bool SupportsCnaCapability(
        this GraphicsDevice graphicsDevice,
        CnaGraphicsCapability capability)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return graphicsDevice.SupportsCnaCapabilityCore(capability);
    }

    /// <summary>Returns the active CNA renderer's diagnostic name.</summary>
    public static string GetCnaRendererName(this GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return graphicsDevice.CnaRendererName;
    }

    /// <summary>
    /// Whether this build of CNA has its CNAEXT engine layer -- compute shaders, storage buffers,
    /// GPU timers, render-target pools, post-process passes, PBR material binding and the rest.
    ///
    /// The whole engine-layer surface is exported by every CNA build, and the routes that need a
    /// native engine-layer object answer `NOT_SUPPORTED` when the layer is absent. That keeps the
    /// ABI one shape regardless of build options, and it means a symbol existing proves nothing
    /// about whether the capability does. Ask this first.
    ///
    /// Not a device method: the answer is a property of the linked library, not of a device, and
    /// there is no device to ask before one exists.
    /// </summary>
    public static bool IsCnaEngineLayerAvailable() =>
        CNA.Graphics.GraphicsDevice.IsCnaEngineLayerAvailable();

    /// <summary>
    /// The engine layer's revision, or zero when this build has no engine layer.
    ///
    /// A revision marker rather than an ABI compatibility promise -- CNA says so explicitly. It is
    /// worth reading because a header and a library from different builds disagree here, which is
    /// otherwise invisible.
    /// </summary>
    public static int CnaEngineLayerVersion() =>
        CNA.Graphics.GraphicsDevice.CnaEngineLayerVersion();

    /// <summary>
    /// Tells every content-losable resource on this device that its content is gone.
    ///
    /// <b>For tests, not for games.</b> See
    /// <see cref="CNA.Graphics.GraphicsDevice.NotifyContentLostResourcesForTesting"/>: it exists so
    /// a <c>RenderTarget2D.ContentLost</c> subscription can be made to fire on renderers that never
    /// lose a device, and calling it from a game would tell every render target its content is gone
    /// while it is not.
    /// </summary>
    public static void NotifyCnaContentLostForTesting(this GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        graphicsDevice.Framework.NotifyContentLostResourcesForTesting();
    }
}

/// <summary>How the back buffer is fitted into the window. See
/// <see cref="CnaGraphicsDeviceManagerExtensions.GetCnaPreferredPresentationMode"/>.</summary>
public enum CnaPresentationMode : uint
{
    Letterbox = 0,
    Overscan = 1,
    Stretch = 2,
    NativeBackBuffer = 3,
    FixedHeightDynamicWidth = 4,
}

/// <summary>
/// How CNA presents the back buffer, which XNA has no say in.
///
/// XNA 4.0 stretches the back buffer to the client area, full stop -- a fixed-aspect XNA game
/// letterboxes by drawing its own bars. CNA does this in the presentation step, so a ported game
/// can drop that code and ask for <see cref="CnaPresentationMode.Letterbox"/> instead. It cannot go
/// on <c>Microsoft.Xna.Framework.GraphicsDeviceManager</c>, whose surface is checked member for
/// member against XNA's metadata.
/// </summary>
public static class CnaGraphicsDeviceManagerExtensions
{
    /// <summary>Reads how the back buffer is currently fitted into the window.</summary>
    public static CnaPresentationMode GetCnaPreferredPresentationMode(
        this Microsoft.Xna.Framework.GraphicsDeviceManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return (CnaPresentationMode)(uint)manager.Framework.PreferredPresentationMode;
    }

    /// <summary>Chooses how the back buffer is fitted into the window.</summary>
    public static void SetCnaPreferredPresentationMode(
        this Microsoft.Xna.Framework.GraphicsDeviceManager manager,
        CnaPresentationMode mode)
    {
        ArgumentNullException.ThrowIfNull(manager);
        manager.Framework.PreferredPresentationMode = (CNA.Graphics.PresentationMode)(uint)mode;
    }
}

/// <summary>
/// The mouse cursor image, which XNA has no API for at all.
///
/// XNA's <c>Mouse</c> is <c>GetState</c>, <c>SetPosition</c> and <c>WindowHandle</c> -- a
/// Windows Phone-era framework had no reason for more. MonoGame added
/// <c>Mouse.SetCursor(MouseCursor)</c>, and a game ported from MonoGame calls it: compiling one
/// against this facade, that call is the *only* thing in eighteen thousand lines that does not
/// resolve. It cannot go on <c>Microsoft.Xna.Framework.Input.Mouse</c>, because the strict facade
/// is checked member for member against XNA's metadata and an extra member fails that gate. So it
/// lives here, and the port is one line: <c>Mouse.SetCursor(MouseCursor.Arrow)</c> becomes
/// <c>CnaMouse.SetCursor(CnaMouseCursor.FromStock(CnaMouseCursorStock.Arrow))</c>.
/// </summary>
public static class CnaMouse
{
    /// <summary>Sets the cursor image for the running game.</summary>
    public static void SetCursor(CnaMouseCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        CNA.Input.Mouse.SetCursor(cursor.Framework);
    }
}

/// <summary>Which of the system's own cursors to use. See <see cref="CnaMouse"/>.</summary>
public enum CnaMouseCursorStock : uint
{
    Arrow = 0,
    Crosshair = 1,
    Hand = 2,
    IBeam = 3,
    No = 4,
    SizeAll = 5,
    SizeNesw = 6,
    SizeNs = 7,
    SizeNwse = 8,
    SizeWe = 9,
    Wait = 10,
    WaitArrow = 11,
}

/// <summary>
/// A cursor image. See <see cref="CnaMouse"/>.
///
/// A stock cursor is the system's and disposing it leaves the system's alone; a texture cursor is
/// this object's and is destroyed with it. The texture's pixels are copied, so the texture may be
/// disposed straight after.
/// </summary>
public sealed class CnaMouseCursor : IDisposable
{
    private CnaMouseCursor(CNA.Input.MouseCursor framework) => Framework = framework;

    internal CNA.Input.MouseCursor Framework { get; }

    /// <summary>One of the system's own cursors.</summary>
    public static CnaMouseCursor FromStock(CnaMouseCursorStock stock) =>
        new(CNA.Input.MouseCursor.FromStock((CNA.Input.MouseCursorStock)(uint)stock));

    /// <summary>A cursor drawn from a texture, with a hot spot inside it.</summary>
    public static CnaMouseCursor FromTexture(Texture2D texture, int originX, int originY)
    {
        ArgumentNullException.ThrowIfNull(texture);
        return new CnaMouseCursor(
            CNA.Input.MouseCursor.FromTextureHandle(texture.NativeHandleValue, originX, originY));
    }

    public void Dispose() => Framework.Dispose();
}
