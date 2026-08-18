using System.Text;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// A media source device a <see cref="MediaLibrary"/> reads content from. Real XNA's own
/// constructor is <c>MediaLibrary</c>-only (matching the real C++ engine's own <c>private</c>,
/// <c>friend class MediaLibrary</c> constructor exactly) -- kept <c>internal</c> here too.
///
/// A snapshot, not a live view. The ABI enumerates sources by index and states that "the list is a
/// point-in-time snapshot -- an index is valid only until the device set changes", so
/// <see cref="Index"/> is only meaningful for as long as the enumeration it came from is current.
/// That is why <see cref="GetAvailableMediaSources"/> re-enumerates on every call rather than
/// caching, and why nothing holds a <see cref="MediaSource"/> across a device change.
/// </summary>
public class MediaSource
{
    internal MediaSource(MediaSourceType mediaSourceType, string name, uint index = 0)
    {
        MediaSourceType = mediaSourceType;
        Name = name;
        Index = index;
    }

    public MediaSourceType MediaSourceType { get; }

    public string Name { get; }

    /// <summary>The enumeration index <c>cna_media_library_create_from_source</c> takes. Not part
    /// of real XNA's <c>MediaSource</c>, which identifies a source by object identity -- this ABI
    /// identifies one by position instead, so the position has to be carried.</summary>
    internal uint Index { get; }

    public override string ToString() => Name;

    /// <summary>
    /// Enumerates every media source the device actually reports, over
    /// <c>cna_media_source_get_available_count</c> and its indexed siblings.
    ///
    /// It used to return a hardcoded single local-device entry, on the stated grounds that this
    /// project had no way to discover a real source. It does -- the same header the media-library
    /// rebinding is built on has had the enumeration all along.
    /// </summary>
    public static unsafe IReadOnlyList<MediaSource> GetAvailableMediaSources()
    {
        CnaResult countResult = Native.cna_media_source_get_available_count(CnaAmbientGame.Current, out uint count);
        CnaException.ThrowIfFailed(countResult, nameof(GetAvailableMediaSources));

        var sources = new MediaSource[count];
        for (uint i = 0; i < count; i++)
        {
            CnaResult typeResult = Native.cna_media_source_get_type_at(CnaAmbientGame.Current, i, out uint type);
            CnaException.ThrowIfFailed(typeResult, nameof(GetAvailableMediaSources));
            sources[i] = new MediaSource((MediaSourceType)type, ReadName(i), i);
        }

        return sources;
    }

    /// <summary>The ABI's two-call size/copy pattern, hand-rolled here rather than routed through
    /// <c>NativeStringReader</c>: these two take <c>(game, index)</c>, where that helper's indexed
    /// overload takes <c>(handle, index)</c> with a <c>ulong</c> index, and this family's index is
    /// <c>uint32_t</c>.</summary>
    private static unsafe string ReadName(uint index)
    {
        CnaResult sizeResult = Native.cna_media_source_get_name_size_at(
            CnaAmbientGame.Current, index, out ulong byteCount);
        CnaException.ThrowIfFailed(sizeResult, nameof(Name));

        if (byteCount == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteCount];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = Native.cna_media_source_copy_name_at(
                CnaAmbientGame.Current, index, bufferPtr, byteCount, out ulong written);
            CnaException.ThrowIfFailed(copyResult, nameof(Name));
            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
