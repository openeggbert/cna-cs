using System.Runtime.InteropServices;
using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// The storage format one representation of a <see cref="CnbTexture"/> is kept in.
///
/// Public and CNA's own vocabulary, not <see cref="SurfaceFormat"/>: the two are not the same set.
/// A CNB file can name a format this runtime has no surface format for, which is a fact about the
/// file that <see cref="CnbTexture.SelectRepresentation"/> has to be able to state. Use
/// <see cref="CnbTextureFormatExtensions.TryToSurfaceFormat"/> to cross over.
/// </summary>
public enum CnbTextureFormat
{
    Unknown = 0,
    Rgba8 = 1,
    Bgra8 = 2,
    Rgba8Srgb = 3,
    Bgr565 = 4,
    Bgra5551 = 5,
    Bgra4444 = 6,
    Alpha8 = 7,
    R8 = 8,
    R16 = 9,
    Rg16 = 10,
    Rgba16 = 11,
    Rg8Snorm = 12,
    Rgba8Snorm = 13,
    Rgb10A2 = 14,
    R32Float = 15,
    Rg32Float = 16,
    Rgba32Float = 17,
    R16Float = 18,
    Rg16Float = 19,
    Rgba16Float = 20,
    HdrBlendable = 21,
    Bc1 = 22,
    Bc2 = 23,
    Bc3 = 24,
    Bc3Srgb = 25,
    Bc7 = 26,
    Bc7Srgb = 27,
}

/// <summary>Crossing between CNB's storage formats and the runtime's surface formats.</summary>
public static class CnbTextureFormatExtensions
{
    /// <summary>
    /// The <see cref="SurfaceFormat"/> this storage format maps onto, or <see langword="false"/>
    /// when CNA has no equivalent.
    ///
    /// Answers <see langword="false"/> rather than throwing, because "this file uses a format this
    /// runtime cannot represent" is an ordinary outcome for a container that carries several
    /// representations on purpose -- the caller moves to the next one.
    /// </summary>
    public static bool TryToSurfaceFormat(this CnbTextureFormat format, out SurfaceFormat surfaceFormat)
    {
        CnaResult result = Native.cna_cnb_texture_format_to_surface_format(
            (CnaCnbTextureFormat)format, out uint value);

        if (result.IsSuccess())
        {
            surfaceFormat = (SurfaceFormat)value;
            return true;
        }

        surfaceFormat = default;
        return false;
    }

    /// <summary>Whether the format stores 4x4 blocks rather than individual texels, which decides
    /// how a level's byte count relates to its dimensions.</summary>
    public static bool IsBlockCompressed(this CnbTextureFormat format)
    {
        CnaResult result = Native.cna_cnb_is_block_compressed_texture_format(
            (CnaCnbTextureFormat)format, out byte blockCompressed);
        CnaException.ThrowIfFailed(result, nameof(IsBlockCompressed));
        return blockCompressed != 0;
    }
}

/// <summary>
/// A texture decoded out of a <c>.cnb</c> container: its shape, its representations, and its level
/// bytes.
///
/// <b>What a representation is, and why this type exists rather than a byte array.</b> One CNB
/// texture can carry the same image several times over -- once as <c>RGBA8</c>, once as <c>BC7</c>,
/// once as something else -- so a runtime picks whichever its GPU supports without shipping a second
/// asset. That choice is the whole point of the format and it cannot be made by the file; it needs
/// the device. So this type stops at "here are the representations and their formats", and
/// <see cref="CnbTextureLoader"/> is what turns one into a real <see cref="Texture2D"/>.
///
/// <b>Ownership.</b> The native description is owned and destroyed here. Level bytes are copied
/// into caller arrays rather than exposed as spans over native memory, for the same reason
/// <see cref="CnbDocument"/> copies chunk data: a span would keep looking valid after
/// <see cref="Dispose"/>.
///
/// <b>Levels are ordered face-major, then mip</b>, which is CNA's own ordering: for a cube map that
/// is <c>+X</c> mip 0, <c>+X</c> mip 1, ..., then <c>-X</c> mip 0. <see cref="LevelIndex"/> exists
/// so a caller never has to spell that multiplication itself, because spelling it the other way
/// round produces a valid index to the wrong image.
/// </summary>
public sealed class CnbTexture : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly CnaCnbTextureInfo _info;

    private CnbTexture(nint handleValue, CnaCnbTextureInfo info)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_texture_data_destroy(new CnaHandle(h)).IsSuccess());
        _info = info;
    }

    /// <summary>
    /// Decodes the 2D texture a container holds.
    ///
    /// A document that is not a 2D texture, uses a schema version CNA does not know, or whose
    /// declared counts and payload lengths disagree, fails here with the native <c>Io</c> result
    /// rather than producing a plausible texture from misread bytes.
    /// </summary>
    public static CnbTexture DecodeTexture2D(CnbDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CnaResult result = Native.cna_cnb_decode_texture2d(document.NativeHandle, out CnaHandle texture);
        CnaException.ThrowIfFailed(result, nameof(DecodeTexture2D));
        GC.KeepAlive(document);

        return Adopt(texture.AsNint, nameof(DecodeTexture2D));
    }

    /// <summary>Wraps a handle CNA has just produced, reading its shape once.
    ///
    /// The shape is read here rather than on each property because it cannot change: a decoded
    /// description is immutable, and six properties each making a native call would be six chances
    /// for a disposed handle to be used.</summary>
    internal static CnbTexture Adopt(nint handleValue, string operation)
    {
        var info = CnaCnbTextureInfo.Versioned();
        CnaResult result = Native.cna_cnb_texture_data_get_info(new CnaHandle(handleValue), ref info);
        if (!result.IsSuccess())
        {
            // The handle is ours the moment the decode succeeded, so it has to be released even
            // though this constructor never completed.
            _ = Native.cna_cnb_texture_data_destroy(new CnaHandle(handleValue));
            CnaException.ThrowIfFailed(result, operation);
        }

        return new CnbTexture(handleValue, info);
    }

    /// <summary>Width of mip level 0, in texels.</summary>
    public int Width => checked((int)_info.Width);

    /// <summary>Height of mip level 0, in texels.</summary>
    public int Height => checked((int)_info.Height);

    /// <summary>Depth of mip level 0, in texels; 1 for a 2D or cube texture.</summary>
    public int Depth => checked((int)_info.Depth);

    /// <summary>1 for a 2D or 3D texture, 6 for a cube.</summary>
    public int FaceCount => checked((int)_info.FaceCount);

    /// <summary>Number of mip levels, at least 1.</summary>
    public int MipCount => checked((int)_info.MipCount);

    /// <summary>How many times the same image is carried, in different storage formats.</summary>
    public int RepresentationCount => checked((int)_info.RepresentationCount);

    /// <summary>The index of one face's one mip level, in CNA's face-major order.</summary>
    public int LevelIndex(int face, int mipLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(face);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(face, FaceCount);
        ArgumentOutOfRangeException.ThrowIfNegative(mipLevel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(mipLevel, MipCount);

        return (face * MipCount) + mipLevel;
    }

    /// <summary>The storage format of one representation.</summary>
    public CnbTextureFormat GetRepresentationFormat(int representation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(representation);

        CnaResult result = Native.cna_cnb_texture_data_get_representation_format(
            Handle, (ulong)representation, out CnaCnbTextureFormat format);
        CnaException.ThrowIfFailed(result, nameof(GetRepresentationFormat));
        GC.KeepAlive(this);
        return (CnbTextureFormat)format;
    }

    /// <summary>How many levels one representation holds -- <c>FaceCount * MipCount</c>, asked of
    /// native rather than multiplied here, so a file whose representation is short is a failure
    /// from CNA rather than an out-of-range copy.</summary>
    public int GetLevelCount(int representation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(representation);

        CnaResult result = Native.cna_cnb_texture_data_get_level_count(
            Handle, (ulong)representation, out ulong count);
        CnaException.ThrowIfFailed(result, nameof(GetLevelCount));
        GC.KeepAlive(this);
        return checked((int)count);
    }

    /// <summary>The dimensions of one mip level, which halve per level and never fall below 1.</summary>
    public (int Width, int Height, int Depth) GetLevelDimensions(int mipLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mipLevel);

        CnaResult result = Native.cna_cnb_texture_data_get_level_dimensions(
            Handle, (uint)mipLevel, out uint width, out uint height, out uint depth);
        CnaException.ThrowIfFailed(result, nameof(GetLevelDimensions));
        GC.KeepAlive(this);
        return (checked((int)width), checked((int)height), checked((int)depth));
    }

    /// <summary>One level's payload bytes, in a fresh array. See this type's own doc comment for
    /// why the bytes are copied rather than viewed.</summary>
    public unsafe byte[] CopyLevel(int representation, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(representation);
        ArgumentOutOfRangeException.ThrowIfNegative(level);

        // Capacity zero is the documented way to ask for the size, and it is asked rather than
        // computed: for a block-compressed format the byte count is not width * height * bytes.
        //
        // BufferTooSmall is the *expected* answer to that question and not a failure -- the route
        // writes the required count and performs no partial write. Treating it as a failure is what
        // the first version of this did, and every call failed.
        CnaResult sizeResult = Native.cna_cnb_texture_data_copy_level(
            Handle, (ulong)representation, (ulong)level, null, 0, out ulong required);
        if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
        {
            CnaException.ThrowIfFailed(sizeResult, nameof(CopyLevel));
        }

        var data = new byte[checked((int)required)];
        if (data.Length == 0)
        {
            GC.KeepAlive(this);
            return data;
        }

        fixed (byte* destination = data)
        {
            CnaResult result = Native.cna_cnb_texture_data_copy_level(
                Handle, (ulong)representation, (ulong)level, destination, (ulong)data.Length, out ulong written);
            CnaException.ThrowIfFailed(result, nameof(CopyLevel));
            GC.KeepAlive(this);

            if (written != (ulong)data.Length)
            {
                throw new CnaException(
                    $"CNB texture representation {representation} level {level} reported {required} bytes but produced {written}.");
            }
        }

        return data;
    }

    /// <summary>
    /// The index of the first representation <paramref name="supported"/> accepts, or -1.
    ///
    /// <b>The selection order is CNA's, not this binding's.</b> The header defines it as "preferring
    /// the earliest supported one", and a managed loop reproducing that would be a second copy of a
    /// rule that can change. So the native route is called and the predicate is what crosses --
    /// which is also the only callback in this family and the reason it is worth binding at all.
    ///
    /// <b>The callback cannot outlive this call.</b> CNA invokes it during
    /// <c>select_representation</c> and never retains it, so the delegate is rooted for exactly that
    /// span by a pinned <see cref="GCHandle"/> released in a <c>finally</c>. A managed exception is
    /// never allowed to unwind into C: the predicate's failure is captured, the predicate then
    /// answers "not supported" for everything remaining, and the exception is rethrown here once the
    /// native frame has returned.
    /// </summary>
    public unsafe int SelectRepresentation(Func<CnbTextureFormat, bool> supported)
    {
        ArgumentNullException.ThrowIfNull(supported);

        var state = new SelectionState(supported);
        GCHandle context = GCHandle.Alloc(state);
        try
        {
            CnaResult result = Native.cna_cnb_texture_data_select_representation(
                Handle,
                (nint)(delegate* unmanaged<CnaCnbTextureFormat, nint, byte>)&SupportedTrampoline,
                GCHandle.ToIntPtr(context),
                out byte found,
                out ulong index);

            GC.KeepAlive(this);

            if (state.Failure is { } failure)
            {
                throw failure;
            }

            CnaException.ThrowIfFailed(result, nameof(SelectRepresentation));
            return found != 0 ? checked((int)index) : -1;
        }
        finally
        {
            context.Free();
        }
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    /// <summary>Carries the caller's predicate and whatever it threw across the native frame.</summary>
    private sealed class SelectionState(Func<CnbTextureFormat, bool> predicate)
    {
        internal Func<CnbTextureFormat, bool> Predicate { get; } = predicate;

        internal Exception? Failure { get; set; }
    }

    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    private static byte SupportedTrampoline(CnaCnbTextureFormat format, nint context)
    {
        // Nothing may escape into C from here, which is why the whole body is guarded rather than
        // only the predicate call.
        try
        {
            if (GCHandle.FromIntPtr(context).Target is not SelectionState state || state.Failure is not null)
            {
                return 0;
            }

            return state.Predicate((CnbTextureFormat)format) ? (byte)1 : (byte)0;
        }
        catch (Exception failure)
        {
            if (GCHandle.FromIntPtr(context).Target is SelectionState state)
            {
                state.Failure = failure;
            }

            return 0;
        }
    }
}
