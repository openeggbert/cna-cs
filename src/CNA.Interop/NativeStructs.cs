using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>ABI-shaped 2D vector. See ../../cnabinding/analysis_binding.md §23.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaVector2
{
    public readonly float X;
    public readonly float Y;

    public CnaVector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>ABI-shaped RGBA color, one byte per channel. See analysis_binding.md §8.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaColor
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public CnaColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

/// <summary>
/// ABI-shaped frame time value. Ticks use the same resolution as
/// <see cref="System.TimeSpan.Ticks"/> (100ns), converted at the CNA.Framework boundary.
/// See ../../cnabinding/analysis_binding_sharp_runtime.md §42.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGameTime
{
    public readonly long TotalGameTimeTicks;
    public readonly long ElapsedGameTimeTicks;
    public readonly byte IsRunningSlowly;

    public CnaGameTime(long totalGameTimeTicks, long elapsedGameTimeTicks, bool isRunningSlowly)
    {
        TotalGameTimeTicks = totalGameTimeTicks;
        ElapsedGameTimeTicks = elapsedGameTimeTicks;
        IsRunningSlowly = isRunningSlowly ? (byte)1 : (byte)0;
    }
}

/// <summary>
/// A full-keyboard snapshot, one bit per CNA key ordinal (0-255), packed into four 64-bit
/// words so callers do not need <c>unsafe</c>/fixed-buffer access. See the "input as snapshots"
/// guidance in ../../cnabinding/analysis_binding.md §25.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaKeyboardState
{
    public readonly ulong Bits0;
    public readonly ulong Bits1;
    public readonly ulong Bits2;
    public readonly ulong Bits3;

    public bool IsKeyDown(int keyOrdinal)
    {
        if (keyOrdinal is < 0 or > 255)
        {
            return false;
        }

        ulong word = keyOrdinal switch
        {
            < 64 => Bits0,
            < 128 => Bits1,
            < 192 => Bits2,
            _ => Bits3,
        };

        return (word & (1UL << (keyOrdinal % 64))) != 0;
    }
}
