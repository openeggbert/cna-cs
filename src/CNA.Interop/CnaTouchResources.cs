using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_TouchLocationState</c>
/// exactly (<c>input.h:520-530</c>).</summary>
internal enum CnaTouchLocationState : uint
{
    Invalid = 0,
    Released = 1,
    Pressed = 2,
    Moved = 3,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_TouchLocation</c> exactly
/// (<c>input.h:535-556</c>). A plain fixed struct with no <c>struct_size</c>/<c>struct_version</c>
/// header -- it only ever appears nested inside <see cref="CnaTouchState"/>, which carries the
/// version header for the whole snapshot.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTouchLocation
{
    public int Id;
    public CnaTouchLocationState State;
    public CnaVector2 Position;
    public CnaTouchLocationState PreviousState;
    public CnaVector2 PreviousPosition;

    /// <summary>A CNA extension with no real-XNA counterpart (<c>0..1</c> when available).
    /// <c>CNA.Input.TouchLocation</c> deliberately does not expose it -- see that type's own doc
    /// comment.</summary>
    public float Pressure;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_TouchState</c> exactly
/// (<c>input.h:575-593</c>) -- a fixed-capacity snapshot, not a growable
/// collection.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTouchState
{
    /// <summary>Matches <c>CNA_TOUCH_MAX_TOUCHES</c> (<c>input.h:533</c>). Note this is 8, while
    /// <c>CNA_TouchCapabilities.maximum_touch_count</c> reports XNA's own documented maximum of 4
    /// -- the snapshot has headroom above what XNA promises, so callers must read
    /// <see cref="TouchCount"/> rather than assuming either number.</summary>
    public const int MaxTouches = 8;

    public uint StructSize;
    public uint StructVersion;
    public byte IsConnected;
    public CnaReservedBytes3 Reserved;
    public uint TouchCount;

    [System.Runtime.CompilerServices.InlineArray(MaxTouches)]
    public struct TouchArray
    {
        private CnaTouchLocation _element0;
    }

    public TouchArray Touches;

    public unsafe CnaTouchState()
    {
        StructSize = (uint)sizeof(CnaTouchState);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_TouchCapabilities</c>
/// exactly (<c>input_touch.h</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTouchCapabilities
{
    public uint StructSize;
    public uint StructVersion;
    public byte IsConnected;
    public CnaReservedBytes3 Reserved;
    public uint MaximumTouchCount;

    public unsafe CnaTouchCapabilities()
    {
        StructSize = (uint)sizeof(CnaTouchCapabilities);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GestureSample</c> exactly
/// (<c>input_touch.h:60-87</c>). <see cref="FingerId"/>/<see cref="FingerId2"/> are CNA
/// extensions (<c>_ext</c>-suffixed upstream) with no real-XNA counterpart.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaGestureSample
{
    public uint StructSize;
    public uint StructVersion;
    public uint GestureType;
    public int FingerId;
    public int FingerId2;
    public uint Reserved;
    public long TimestampTicks;
    public CnaVector2 Position;
    public CnaVector2 Position2;
    public CnaVector2 Delta;
    public CnaVector2 Delta2;

    public unsafe CnaGestureSample()
    {
        StructSize = (uint)sizeof(CnaGestureSample);
        StructVersion = 1;
    }
}
