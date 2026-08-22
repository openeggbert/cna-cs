using CNA;
using CNA.Graphics.PackedVector;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// The packed-vector formats are pure managed arithmetic, so unlike almost everything else added in
/// this phase they are fully testable here -- and worth testing, because a wrong shift or a wrong
/// divisor produces plausible-looking colours rather than an obvious failure.
///
/// Each expectation below is derived from the engine's own
/// <c>Microsoft/Xna/Framework/Graphics/PackedVector/*.hpp</c>, not from the format name. The three
/// things most likely to be got wrong, and therefore pinned explicitly, are: channel *order* (the
/// BGRA formats do not store R first), XNA's clamp-then-<see cref="Math.Round(double)"/> nearest-even
/// quantization (including unnormalized integer formats), and which components a narrow format
/// leaves at their defaults.
/// </summary>
public class PackedVectorTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void Alpha8_UsesWOnly_AndReportsZeroForRgb()
    {
        var value = new Alpha8(1f);

        Assert.Equal(255, value.PackedValue);
        Vector4 expanded = value.ToVector4();
        Assert.Equal(0f, expanded.X);
        Assert.Equal(0f, expanded.Y);
        Assert.Equal(0f, expanded.Z);
        Assert.Equal(1f, expanded.W);
    }

    /// <summary>Alpha8 packs from W, not X -- packing a vector whose W is zero must give zero even
    /// when X is one. Reading the wrong component is the mistake this catches.</summary>
    [Fact]
    public void Alpha8_PackFromVector4_ReadsW_NotX()
    {
        var value = default(Alpha8);
        value.PackFromVector4(new Vector4(1f, 1f, 1f, 0f));

        Assert.Equal(0, value.PackedValue);
    }

    [Fact]
    public void Alpha8_RoundsScaledTieToEven()
    {
        // 0.5 * 255 = 127.5, whose nearest even integer is 128.
        Assert.Equal(128, new Alpha8(0.5f).PackedValue);
    }

    [Fact]
    public void Alpha8_ClampsOutOfRange()
    {
        Assert.Equal(0, new Alpha8(-1f).PackedValue);
        Assert.Equal(255, new Alpha8(2f).PackedValue);
    }

    /// <summary>Green occupies the middle six bits, so its divisor is 63 while red and blue use 31.
    /// A single divisor for all three is the classic 565 bug.</summary>
    [Fact]
    public void Bgr565_GreenHasSixBits()
    {
        var value = new Bgr565(0f, 1f, 0f);

        Assert.Equal(0x07E0, value.PackedValue);
        Assert.Equal(1f, value.ToVector4().Y);
    }

    [Fact]
    public void Bgr565_RedOccupiesTheHighBits()
    {
        Assert.Equal(0xF800, new Bgr565(1f, 0f, 0f).PackedValue);
        Assert.Equal(0x001F, new Bgr565(0f, 0f, 1f).PackedValue);
    }

    [Fact]
    public void Bgr565_ToVector4_LeavesAlphaOpaque()
    {
        Assert.Equal(1f, new Bgr565(0.2f, 0.4f, 0.6f).ToVector4().W);
    }

    /// <summary>Blue is in the low nibble and alpha in the high one -- the name's channel order is
    /// the storage order, not the argument order.</summary>
    [Fact]
    public void Bgra4444_StoresBlueLowestAndAlphaHighest()
    {
        Assert.Equal(0x000F, new Bgra4444(0f, 0f, 1f, 0f).PackedValue);
        Assert.Equal(0xF000, new Bgra4444(0f, 0f, 0f, 1f).PackedValue);
        Assert.Equal(0x0F00, new Bgra4444(1f, 0f, 0f, 0f).PackedValue);
        Assert.Equal(0x00F0, new Bgra4444(0f, 1f, 0f, 0f).PackedValue);
    }

    /// <summary>One alpha bit means the value rounds to fully opaque or fully transparent, with the
    /// threshold at 0.5.</summary>
    [Theory]
    [InlineData(0.49f, 0f)]
    [InlineData(0.5f, 0f)]
    [InlineData(0.5001f, 1f)]
    [InlineData(1f, 1f)]
    public void Bgra5551_AlphaIsOneBit(float alpha, float expected)
    {
        Assert.Equal(expected, new Bgra5551(0f, 0f, 0f, alpha).ToVector4().W);
    }

    /// <summary>Byte4's components are [0, 255], not [0, 1], and use XNA's nearest-even
    /// quantization.</summary>
    [Fact]
    public void Byte4_IsUnnormalizedAndRoundsTiesToEven()
    {
        var value = new Byte4(1.5f, 2.5f, 3.5f, 4.5f);

        Assert.Equal(2u | (2u << 8) | (4u << 16) | (4u << 24), value.PackedValue);
        Assert.Equal(2f, value.ToVector4().X);
    }

    [Fact]
    public void Byte4_ClampsToByteRange()
    {
        Assert.Equal(255u, new Byte4(300f, 0f, 0f, 0f).PackedValue & 0xFF);
        Assert.Equal(0u, new Byte4(-5f, 0f, 0f, 0f).PackedValue & 0xFF);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-2.5f)]
    [InlineData(65504f)]
    public void HalfSingle_RoundTripsValuesRepresentableInBinary16(float value)
    {
        Assert.Equal(value, new HalfSingle(value).ToSingle());
    }

    [Fact]
    public void HalfVector2_PacksXInTheLowHalf()
    {
        var value = new HalfVector2(1f, 2f);

        Assert.Equal(1f, value.ToVector2().X);
        Assert.Equal(2f, value.ToVector2().Y);
        Assert.Equal(BitConverter.HalfToUInt16Bits((Half)1f), (ushort)(value.PackedValue & 0xFFFF));
    }

    [Fact]
    public void HalfVector4_RoundTripsAllFourComponents()
    {
        Vector4 expanded = new HalfVector4(1f, -2f, 0.5f, 4f).ToVector4();

        Assert.Equal(1f, expanded.X);
        Assert.Equal(-2f, expanded.Y);
        Assert.Equal(0.5f, expanded.Z);
        Assert.Equal(4f, expanded.W);
    }

    /// <summary>Signed-normalized formats scale first and then use nearest-even rounding. A tie
    /// whose lower integer is even distinguishes that rule from half-away-from-zero.</summary>
    [Fact]
    public void NormalizedByte2_RoundsTiesToEven()
    {
        const float half = 62.5f / 127f;

        Assert.Equal(62, (sbyte)(new NormalizedByte2(half, 0f).PackedValue & 0xFF));
        Assert.Equal(-62, (sbyte)(new NormalizedByte2(-half, 0f).PackedValue & 0xFF));
    }

    [Fact]
    public void NormalizedByte2_ClampsToUnitRange()
    {
        Assert.Equal(127, (sbyte)(new NormalizedByte2(5f, 0f).PackedValue & 0xFF));
        Assert.Equal(-127, (sbyte)(new NormalizedByte2(-5f, 0f).PackedValue & 0xFF));
    }

    [Fact]
    public void NormalizedByte4_RoundTripsEachComponent()
    {
        Vector4 expanded = new NormalizedByte4(1f, -1f, 0f, 1f).ToVector4();

        Assert.Equal(1f, expanded.X, Tolerance);
        Assert.Equal(-1f, expanded.Y, Tolerance);
        Assert.Equal(0f, expanded.Z, Tolerance);
        Assert.Equal(1f, expanded.W, Tolerance);
    }

    [Fact]
    public void NormalizedShort2_UsesTheSignedShortRange()
    {
        var value = new NormalizedShort2(1f, -1f);

        Assert.Equal(32767, (short)(value.PackedValue & 0xFFFF));
        Assert.Equal(-32767, (short)((value.PackedValue >> 16) & 0xFFFF));
    }

    [Fact]
    public void NormalizedShort4_RoundTripsEachComponent()
    {
        Vector4 expanded = new NormalizedShort4(0.5f, -0.5f, 1f, 0f).ToVector4();

        Assert.Equal(0.5f, expanded.X, Tolerance);
        Assert.Equal(-0.5f, expanded.Y, Tolerance);
        Assert.Equal(1f, expanded.Z, Tolerance);
        Assert.Equal(0f, expanded.W, Tolerance);
    }

    [Fact]
    public void Rg32_LeavesBlueZeroAndAlphaOpaque()
    {
        Vector4 expanded = new Rg32(1f, 1f).ToVector4();

        Assert.Equal(1f, expanded.X, Tolerance);
        Assert.Equal(1f, expanded.Y, Tolerance);
        Assert.Equal(0f, expanded.Z);
        Assert.Equal(1f, expanded.W);
    }

    /// <summary>Two alpha bits give four levels, so 0.5 quantizes to 2/3 rather than staying
    /// 0.5.</summary>
    [Fact]
    public void Rgba1010102_AlphaHasFourLevels()
    {
        Assert.Equal(0f, new Rgba1010102(0f, 0f, 0f, 0f).ToVector4().W, Tolerance);
        Assert.Equal(2f / 3f, new Rgba1010102(0f, 0f, 0f, 0.5f).ToVector4().W, Tolerance);
        Assert.Equal(1f, new Rgba1010102(0f, 0f, 0f, 1f).ToVector4().W, Tolerance);
    }

    [Fact]
    public void Rgba1010102_RedOccupiesTheLowTenBits()
    {
        Assert.Equal(0x3FFu, new Rgba1010102(1f, 0f, 0f, 0f).PackedValue);
    }

    [Fact]
    public void Rgba64_RoundTripsFullRange()
    {
        Vector4 expanded = new Rgba64(0f, 0.5f, 1f, 0.25f).ToVector4();

        Assert.Equal(0f, expanded.X, Tolerance);
        Assert.Equal(0.5f, expanded.Y, Tolerance);
        Assert.Equal(1f, expanded.Z, Tolerance);
        Assert.Equal(0.25f, expanded.W, Tolerance);
    }

    /// <summary>Short2 is unnormalized: a component of 1 means the integer 1, not full scale. A
    /// normalized implementation would answer 32767 here.</summary>
    [Fact]
    public void Short2_IsUnnormalized()
    {
        var value = new Short2(1f, -1f);

        Assert.Equal(1, (short)(value.PackedValue & 0xFFFF));
        Assert.Equal(-1, (short)((value.PackedValue >> 16) & 0xFFFF));
        Assert.Equal(1f, value.ToVector4().X);
    }

    [Fact]
    public void Short2_RoundsTiesToEven()
    {
        var value = new Short2(1.5f, -2.5f);

        Assert.Equal(2, (short)(value.PackedValue & 0xFFFF));
        Assert.Equal(-2, (short)((value.PackedValue >> 16) & 0xFFFF));
    }

    [Fact]
    public void Short4_ClampsToTheSignedShortRange()
    {
        var value = new Short4(40000f, -40000f, 0f, 0f);

        Assert.Equal(32767, (short)(value.PackedValue & 0xFFFF));
        Assert.Equal(-32768, (short)((value.PackedValue >> 16) & 0xFFFF));
    }

    /// <summary>Every format must round-trip through the untyped
    /// <see cref="IPackedVector"/> contract, which is the whole reason the interface exists. Run
    /// over all seventeen so a new format cannot be added without satisfying it.</summary>
    [Theory]
    [InlineData(typeof(Alpha8))]
    [InlineData(typeof(Bgr565))]
    [InlineData(typeof(Bgra4444))]
    [InlineData(typeof(Bgra5551))]
    [InlineData(typeof(Byte4))]
    [InlineData(typeof(HalfSingle))]
    [InlineData(typeof(HalfVector2))]
    [InlineData(typeof(HalfVector4))]
    [InlineData(typeof(NormalizedByte2))]
    [InlineData(typeof(NormalizedByte4))]
    [InlineData(typeof(NormalizedShort2))]
    [InlineData(typeof(NormalizedShort4))]
    [InlineData(typeof(Rg32))]
    [InlineData(typeof(Rgba1010102))]
    [InlineData(typeof(Rgba64))]
    [InlineData(typeof(Short2))]
    [InlineData(typeof(Short4))]
    public void EveryFormat_ImplementsIPackedVector_AndRoundTripsThroughIt(Type type)
    {
        var packed = (IPackedVector)Activator.CreateInstance(type)!;
        packed.PackFromVector4(new Vector4(0f, 0f, 0f, 0f));
        Vector4 zeroed = packed.ToVector4();

        packed.PackFromVector4(new Vector4(1f, 1f, 1f, 1f));
        Vector4 ones = packed.ToVector4();

        // Every format has at least one component that distinguishes all-zero from all-one input.
        // Byte4/Short2/Short4 are unnormalized, so their "1" stays 1 rather than saturating -- the
        // assertion is that the two inputs are distinguishable, not that they hit any fixed value.
        Assert.NotEqual(
            (zeroed.X, zeroed.Y, zeroed.Z, zeroed.W),
            (ones.X, ones.Y, ones.Z, ones.W));
    }
}
