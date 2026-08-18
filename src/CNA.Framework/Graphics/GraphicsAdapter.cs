using System.Text;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>GraphicsAdapter</c>: one graphics device the machine can render with.
///
/// Unlike every other native-backed type here, an adapter is <em>not</em> a resource with a
/// handle. Every C API adapter function takes a graphics-device handle plus an
/// <see cref="AdapterIndex"/> (<c>display.h</c>), because adapters are entries in a list the
/// device enumerates rather than objects the caller owns -- so this type holds a device and an
/// index, and there is nothing to dispose.
///
/// That also explains the one real deviation from XNA's shape: real XNA exposes
/// <c>GraphicsAdapter.Adapters</c> and <c>DefaultAdapter</c> as <em>static</em> members, which is
/// impossible here since enumerating needs a device. <see cref="GetAdapters"/> and
/// <see cref="GetDefaultAdapter"/> take one instead. Game code that only reads
/// <c>GraphicsDevice.Adapter</c> -- by far the common case, and what
/// <c>cna-cs-template</c>'s own renderer probe does -- is unaffected.
/// </summary>
public class GraphicsAdapter
{
    private readonly GraphicsDevice? _graphicsDevice;

    internal GraphicsAdapter(GraphicsDevice graphicsDevice, uint adapterIndex)
    {
        _graphicsDevice = graphicsDevice;
        AdapterIndex = adapterIndex;
    }

    /// <summary>For the static <see cref="Adapters"/>/<see cref="DefaultAdapter"/> path, which has
    /// no managed <see cref="GraphicsDevice"/> to hold -- only the ambient game's handle. Every
    /// member here uses the device for exactly one thing, its handle, so carrying none is
    /// sufficient.</summary>
    private GraphicsAdapter(uint adapterIndex)
    {
        AdapterIndex = adapterIndex;
    }

    /// <summary>This adapter's device handle: the one it was constructed with, or the ambient
    /// game's when it was constructed without.</summary>
    private CnaHandle DeviceHandle => _graphicsDevice?.ResolveNativeDeviceHandle() ?? AmbientDeviceHandle();

    public uint AdapterIndex { get; }

    public bool IsDefaultAdapter => GetInfo().IsDefaultAdapter != 0;

    public bool IsWideScreen => GetInfo().IsWideScreen != 0;

    public int VendorId => GetInfo().VendorId;

    public int DeviceId => GetInfo().DeviceId;

    public int Revision => GetInfo().Revision;

    public int SubSystemId => GetInfo().SubsystemId;

    public unsafe string Description => CopyString(&CopyDescription, GetInfo().DescriptionByteLength);

    public unsafe string DeviceName => CopyString(&CopyDeviceName, GetInfo().DeviceNameByteLength);

    public DisplayMode CurrentDisplayMode
    {
        get
        {
            var native = new CnaDisplayMode();
            CnaResult result = Native.cna_graphics_adapter_get_current_display_mode(
                DeviceHandle, AdapterIndex, ref native);
            CnaException.ThrowIfFailed(result, nameof(CurrentDisplayMode));
            return DisplayMode.FromNative(in native);
        }
    }

    /// <summary>A fresh snapshot on every read, matching the two-call size-then-copy pattern the
    /// ABI uses everywhere -- the mode list can change (a monitor hotplug), and
    /// <see cref="Refresh"/> is what makes native re-enumerate.</summary>
    public unsafe DisplayModeCollection SupportedDisplayModes
    {
        get
        {
            CnaHandle device = DeviceHandle;

            CnaResult countResult = Native.cna_graphics_adapter_get_display_mode_count(device, AdapterIndex, 0, 0, out ulong count);
            CnaException.ThrowIfFailed(countResult, nameof(SupportedDisplayModes));

            if (count == 0)
            {
                return new DisplayModeCollection([]);
            }

            var native = new CnaDisplayMode[count];
            fixed (CnaDisplayMode* nativePtr = native)
            {
                // filterByFormat: 0 -- XNA's SupportedDisplayModes is the unfiltered list; the
                // per-format view is the collection's own format indexer, applied managed-side.
                CnaResult copyResult = Native.cna_graphics_adapter_copy_display_modes(
                    device, AdapterIndex, 0, 0, nativePtr, count, out ulong written);
                CnaException.ThrowIfFailed(copyResult, nameof(SupportedDisplayModes));

                var modes = new DisplayMode[written];
                for (int i = 0; i < modes.Length; i++)
                {
                    modes[i] = DisplayMode.FromNative(in native[i]);
                }

                return new DisplayModeCollection(modes);
            }
        }
    }

    public bool IsProfileSupported(GraphicsProfile profile)
    {
        CnaResult result = Native.cna_graphics_adapter_is_profile_supported(
            DeviceHandle, AdapterIndex, (CnaGraphicsProfile)profile, out byte supported);
        CnaException.ThrowIfFailed(result, nameof(IsProfileSupported));
        return supported != 0;
    }

    /// <summary>
    /// The OS monitor handle this adapter drives (an <c>HMONITOR</c> on Windows). Matches real
    /// XNA's own <c>MonitorHandle</c>, which is likewise an <c>IntPtr</c>.
    ///
    /// <c>cna_graphics_adapter_get_native_monitor_handle</c> is real and bound, but its own header
    /// doc says it "reports the native-monitor-handle mapping as unavailable at the stable C
    /// boundary" and returns <c>CNA_RESULT_NOT_SUPPORTED</c> after validating the adapter index.
    /// So this throws today.
    ///
    /// Calling the route and translating its failure, rather than throwing unconditionally, is the
    /// deliberate choice: the property answers correctly the day the engine implements it, with no
    /// change here, and an invalid adapter index is still reported as an invalid adapter index
    /// rather than being masked by a blanket throw.
    /// </summary>
    /// <exception cref="CnaException">Always, while native reports the mapping unavailable.</exception>
    public nint MonitorHandle
    {
        get
        {
            CnaResult result = Native.cna_graphics_adapter_get_native_monitor_handle(
                DeviceHandle, AdapterIndex, out ulong value);
            CnaException.ThrowIfFailed(result, nameof(MonitorHandle));
            return (nint)value;
        }
    }

    /// <summary>Re-enumerates the machine's adapters. Real XNA has no equivalent -- its adapter
    /// list is fixed for the process lifetime -- but <c>cna_graphics_adapters_refresh</c> is real,
    /// and without exposing it a caller could never see a monitor hotplug.</summary>
    public static void Refresh(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_graphics_adapters_refresh(graphicsDevice.ResolveNativeDeviceHandle());
        CnaException.ThrowIfFailed(result, nameof(Refresh));
    }

    /// <summary>
    /// Every adapter on the system. Matches real XNA's static <c>Adapters</c>.
    ///
    /// Static after all. The doc comment above says enumerating "needs a device", which is true of
    /// the ABI route -- but a device is reachable from the ambient game
    /// (<c>cna_game_get_graphics_device</c>), the same way <c>Keyboard</c>/<c>Mouse</c> reach one.
    /// So the XNA shape is available and <see cref="GetAdapters(GraphicsDevice)"/> stays for a
    /// caller that has a specific device in hand.
    /// </summary>
    public static IReadOnlyList<GraphicsAdapter> Adapters
    {
        get
        {
            CnaResult result = Native.cna_graphics_adapter_get_count(AmbientDeviceHandle(), out ulong count);
            CnaException.ThrowIfFailed(result, nameof(Adapters));

            var adapters = new GraphicsAdapter[count];
            for (uint i = 0; i < count; i++)
            {
                adapters[i] = new GraphicsAdapter(i);
            }

            return adapters;
        }
    }

    /// <summary>The adapter the system considers default. Matches real XNA's static
    /// <c>DefaultAdapter</c>. See <see cref="Adapters"/>.</summary>
    public static GraphicsAdapter DefaultAdapter
    {
        get
        {
            foreach (GraphicsAdapter adapter in Adapters)
            {
                if (adapter.IsDefaultAdapter)
                {
                    return adapter;
                }
            }

            // Matches GetDefaultAdapter's own reasoning: the ABI reports which adapter is default
            // rather than promising index zero, so falling back to zero is a last resort and not
            // the primary answer.
            return new GraphicsAdapter(0);
        }
    }

    /// <summary>The ambient game's graphics device handle. Throws with a message naming the cause
    /// when no game is running, rather than passing a zero handle to native.</summary>
    private static CnaHandle AmbientDeviceHandle()
    {
        CnaResult result = Native.cna_game_get_graphics_device(CnaAmbientGame.Current, out CnaHandle device);
        CnaException.ThrowIfFailed(result, nameof(Adapters));
        return device;
    }

    /// <summary>
    /// What this adapter would actually give you for a requested render-target format.
    ///
    /// Matches real XNA's <c>QueryRenderTargetFormat</c>: the <c>out</c> values are what the adapter
    /// substituted, and the return value says whether anything had to be substituted at all.
    /// </summary>
    public bool QueryRenderTargetFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount) =>
        Query(
            Native.cna_graphics_adapter_query_render_target_format, graphicsProfile, format, depthFormat,
            multiSampleCount, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount,
            nameof(QueryRenderTargetFormat));

    /// <summary>The back-buffer equivalent. See <see cref="QueryRenderTargetFormat"/>.</summary>
    public bool QueryBackBufferFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount) =>
        Query(
            Native.cna_graphics_adapter_query_backbuffer_format, graphicsProfile, format, depthFormat,
            multiSampleCount, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount,
            nameof(QueryBackBufferFormat));

    private delegate CnaResult QueryFunc(
        CnaHandle device, uint adapterIndex, uint profile, uint format, uint depthFormat,
        int multiSampleCount, ref CnaGraphicsFormatSelection outSelection);

    private bool Query(
        QueryFunc query,
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount,
        string context)
    {
        var selection = new CnaGraphicsFormatSelection();
        CnaResult result = query(
            AmbientDeviceHandle(), AdapterIndex, (uint)graphicsProfile, (uint)format, (uint)depthFormat,
            multiSampleCount, ref selection);
        CnaException.ThrowIfFailed(result, context);

        selectedFormat = (SurfaceFormat)selection.Format;
        selectedDepthFormat = (DepthFormat)selection.DepthFormat;
        selectedMultiSampleCount = selection.MultiSampleCount;
        return selection.ExactMatch != 0;
    }

    /// <summary>
    /// Whether device creation should use the null (no-op) device. Matches real XNA's static
    /// <c>UseNullDevice</c>.
    ///
    /// Stored managed-side and pushed to native on the next
    /// <see cref="ApplyDevicePreferences(GraphicsDevice)"/>. It cannot be pushed on write:
    /// <c>cna_graphics_adapter_set_device_preferences</c> takes a graphics device and an adapter
    /// index, and real XNA's property is a static with neither -- a game sets it *before* creating
    /// the device it applies to.
    /// </summary>
    public static bool UseNullDevice { get; set; }

    /// <summary>Whether device creation should use the reference rasterizer. See
    /// <see cref="UseNullDevice"/> for why this is applied rather than pushed.</summary>
    public static bool UseReferenceDevice { get; set; }

    /// <summary>Pushes <see cref="UseNullDevice"/>/<see cref="UseReferenceDevice"/> to native for
    /// one adapter. <c>CNAEXT</c>: real XNA has no such call, because its statics are read by the
    /// device factory rather than pushed -- this ABI needs a device and an index, so the push has to
    /// be explicit.</summary>
    public void ApplyDevicePreferences(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_graphics_adapter_set_device_preferences(
            graphicsDevice.ResolveNativeDeviceHandle(),
            AdapterIndex,
            UseNullDevice ? (byte)1 : (byte)0,
            UseReferenceDevice ? (byte)1 : (byte)0);
        CnaException.ThrowIfFailed(result, nameof(ApplyDevicePreferences));
    }

    /// <summary>Real XNA's <c>GraphicsAdapter.Adapters</c>, which cannot be static here -- see this
    /// class's own doc comment.</summary>
    public static IReadOnlyList<GraphicsAdapter> GetAdapters(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_graphics_adapter_get_count(graphicsDevice.ResolveNativeDeviceHandle(), out ulong count);
        CnaException.ThrowIfFailed(result, nameof(GetAdapters));

        var adapters = new GraphicsAdapter[count];
        for (uint i = 0; i < count; i++)
        {
            adapters[i] = new GraphicsAdapter(graphicsDevice, i);
        }

        return adapters;
    }

    /// <summary>Real XNA's <c>GraphicsAdapter.DefaultAdapter</c>. Returns the adapter whose own
    /// <see cref="IsDefaultAdapter"/> is set rather than assuming index zero -- the C API reports
    /// that flag per adapter precisely because the default is not guaranteed to be first.</summary>
    public static GraphicsAdapter GetDefaultAdapter(GraphicsDevice graphicsDevice)
    {
        foreach (GraphicsAdapter adapter in GetAdapters(graphicsDevice))
        {
            if (adapter.IsDefaultAdapter)
            {
                return adapter;
            }
        }

        throw new InvalidOperationException("No graphics adapter reported itself as the default adapter.");
    }

    private CnaGraphicsAdapterInfo GetInfo()
    {
        var info = new CnaGraphicsAdapterInfo();
        CnaResult result = Native.cna_graphics_adapter_get_info(
            DeviceHandle, AdapterIndex, ref info);
        CnaException.ThrowIfFailed(result, "cna_graphics_adapter_get_info");
        return info;
    }

    // Two trivial static forwarders rather than a delegate over the P/Invoke method group:
    // a function pointer keeps CopyString allocation-free and, more importantly, keeps the
    // pointer-typed signature out of a delegate type that would otherwise need its own
    // unsafe-context declaration at every use site.
    private static unsafe CnaResult CopyDescription(CnaHandle device, uint index, byte* destination, ulong capacity, out ulong outBytes) =>
        Native.cna_graphics_adapter_copy_description(device, index, destination, capacity, out outBytes);

    private static unsafe CnaResult CopyDeviceName(CnaHandle device, uint index, byte* destination, ulong capacity, out ulong outBytes) =>
        Native.cna_graphics_adapter_copy_device_name(device, index, destination, capacity, out outBytes);

    private unsafe string CopyString(
        delegate*<CnaHandle, uint, byte*, ulong, out ulong, CnaResult> copy, ulong byteLength)
    {
        if (byteLength == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteLength];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult result = copy(DeviceHandle, AdapterIndex, bufferPtr, byteLength, out ulong written);
            CnaException.ThrowIfFailed(result, "cna_graphics_adapter_copy_*");
            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
