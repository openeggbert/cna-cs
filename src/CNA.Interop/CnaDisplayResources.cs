using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_DisplayMode</c> exactly
/// (<c>display.h</c>). <see cref="AspectRatio"/> is computed by
/// <see cref="Native.cna_display_mode_init"/> rather than supplied, which is why this struct is
/// always obtained from a native call and never hand-built.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaDisplayMode
{
    public uint StructSize;
    public uint StructVersion;
    public int Width;
    public int Height;
    public float AspectRatio;
    public uint Format;

    public unsafe CnaDisplayMode()
    {
        StructSize = (uint)sizeof(CnaDisplayMode);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GraphicsAdapterInfo</c>
/// exactly (<c>display.h</c>). The two byte-length fields are the size half of the ABI's usual
/// two-call size-then-copy string pattern -- the text itself comes from
/// <see cref="Native.cna_graphics_adapter_copy_description"/>/<c>_copy_device_name</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaGraphicsAdapterInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint AdapterIndex;
    public byte IsDefaultAdapter;
    public byte IsWideScreen;
    public byte UseNullDevice;
    public byte UseReferenceDevice;
    public int VendorId;
    public int DeviceId;
    public int Revision;
    public int SubsystemId;
    public ulong DescriptionByteLength;
    public ulong DeviceNameByteLength;

    public unsafe CnaGraphicsAdapterInfo()
    {
        StructSize = (uint)sizeof(CnaGraphicsAdapterInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_PresentationParameters</c>
/// exactly (<c>display.h</c>), including its two trailing reserved bytes.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaPresentationParameters
{
    public uint StructSize;
    public uint StructVersion;
    public uint BackBufferFormat;
    public int BackBufferWidth;
    public int BackBufferHeight;
    public uint DepthStencilFormat;
    public int MultiSampleCount;
    public uint PresentationInterval;
    public uint DisplayOrientation;
    public uint RenderTargetUsage;
    public byte IsFullScreen;

    /// <summary>A CNA extension with no real-XNA counterpart. <c>CNA.Graphics.PresentationParameters</c>
    /// deliberately does not surface it -- same fidelity rule as <c>TouchLocation.pressure</c>.</summary>
    public byte HeadlessExt;

    public ushort Reserved;

    public unsafe CnaPresentationParameters()
    {
        StructSize = (uint)sizeof(CnaPresentationParameters);
        StructVersion = 1;
    }
}

/// <summary>
/// Mirrors <c>CNA_GraphicsFormatSelection</c> exactly (<c>display.h:99</c>): what an adapter would
/// actually give you for a requested format, and whether it had to substitute.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaGraphicsFormatSelection
{
    public uint StructSize;
    public uint StructVersion;
    public byte ExactMatch;
    public byte Reserved0;
    public byte Reserved1;
    public byte Reserved2;
    public uint Format;
    public uint DepthFormat;
    public int MultiSampleCount;

    public unsafe CnaGraphicsFormatSelection()
    {
        StructSize = (uint)sizeof(CnaGraphicsFormatSelection);
        StructVersion = 1;
    }
}
