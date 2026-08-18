using System.Collections;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// The ordered list of songs <see cref="MediaPlayer"/> is currently playing through, exposed via
/// <see cref="MediaPlayer.Queue"/>.
///
/// Native-backed over <c>cna_media_queue_*</c>. It used to be a managed <c>List&lt;Song&gt;</c>
/// that <see cref="MediaPlayer"/> maintained itself, which meant this queue and the one native
/// actually plays from were two separate lists that could disagree -- and native's was the one
/// driving the audio device. A header audit found the whole surface sitting unbound.
///
/// Real XNA's own <c>MediaQueue</c> has no public constructor and no public <c>Add</c>/
/// <c>Clear</c>; both are <c>internal</c>, mutated only by <see cref="MediaPlayer"/>. That
/// encapsulation is kept exactly, and needs no deviation here, because nothing outside
/// <see cref="MediaPlayer"/> ever builds one.
/// </summary>
public class MediaQueue : IEnumerable<Song>, IDisposable
{
    private readonly NativeResourceHandle _handle;

    internal MediaQueue(CnaHandle handle)
    {
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_media_queue_destroy(new CnaHandle(h)));
    }

    /// <summary>See <see cref="MediaLibrary"/> for why every handle read pairs with
    /// <see cref="GC.KeepAlive(object)"/>.</summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_media_queue_get_count(NativeHandle, out int count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return count;
        }
    }

    /// <summary>-1 on an empty queue, matching the canonical constructor's own initial value: 0
    /// would make <see cref="ActiveSong"/> index an entry that does not exist.</summary>
    public int ActiveSongIndex
    {
        get
        {
            CnaResult result = Native.cna_media_queue_get_active_song_index(NativeHandle, out int index);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(ActiveSongIndex));
            return index;
        }
        set
        {
            CnaResult result = Native.cna_media_queue_set_active_song_index(NativeHandle, value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(ActiveSongIndex));
        }
    }

    public Song? ActiveSong
    {
        get
        {
            CnaResult result = Native.cna_media_queue_get_active_song(NativeHandle, out CnaHandle song, out byte available);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(ActiveSong));
            return available != 0 ? new Song(song) : null;
        }
    }

    public Song this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            CnaResult result = Native.cna_media_queue_get_at(NativeHandle, index, out CnaHandle song);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, "MediaQueue indexer");
            return new Song(song);
        }
    }

    internal void Add(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);

        CnaResult result = Native.cna_media_queue_add(NativeHandle, song.NativeHandle);
        GC.KeepAlive(this);
        GC.KeepAlive(song);
        CnaException.ThrowIfFailed(result, nameof(Add));
    }

    internal void Clear()
    {
        CnaResult result = Native.cna_media_queue_clear(NativeHandle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    /// <summary>Releases this view's handle. The queue itself belongs to the player and is
    /// untouched -- <c>cna_media_queue_destroy</c> releases the handle, not the queue. Internal in
    /// spirit: real XNA's <c>MediaQueue</c> is not disposable, and the only caller is
    /// <see cref="MediaPlayer"/> dropping its cache when the game goes away.</summary>
    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Snapshots the count once, then reads each entry. A song appended while an
    /// enumeration is in flight is not seen by it -- deliberate, and the same choice
    /// <c>GameComponentCollection</c> makes: the alternative is re-reading <see cref="Count"/> per
    /// step, which would let native's own queue advance turn a <c>foreach</c> into an unbounded
    /// loop.</summary>
    public IEnumerator<Song> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
