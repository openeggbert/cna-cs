using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// An ordered, read-only collection of <see cref="Song"/>s, used with
/// <see cref="MediaPlayer.Play(SongCollection)"/> and returned by every
/// <see cref="MediaLibrary"/> grouping (<see cref="Album.Songs"/>, <see cref="Artist.Songs"/>,
/// <see cref="Genre.Songs"/>, <see cref="Playlist.Songs"/>).
///
/// Real XNA's own constructor is content-pipeline-only; this project has no content pipeline, so --
/// matching the real openeggbert/cna C++ engine's own <c>CNAEXT</c>-marked constructor -- the
/// list-taking one is public. It builds a real native collection over
/// <c>cna_song_collection_create</c> rather than holding the managed list, which is what lets one
/// built here be passed to the same native routes a library-sourced one is.
/// </summary>
public sealed class SongCollection : ReadOnlyMediaCollection<Song>
{
    public SongCollection(IReadOnlyList<Song> songs)
        : base(CreateNative(songs), Native.cna_song_collection_get_count, Native.cna_song_collection_get_at,
               h => Native.cna_song_collection_destroy(h), h => new Song(h))
    {
    }

    internal SongCollection(CnaHandle handle)
        : base(handle, Native.cna_song_collection_get_count, Native.cna_song_collection_get_at,
               h => Native.cna_song_collection_destroy(h), h => new Song(h))
    {
    }

    private static unsafe CnaHandle CreateNative(IReadOnlyList<Song> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);

        CnaHandle[] handles = new CnaHandle[songs.Count];
        for (int i = 0; i < handles.Length; i++)
        {
            handles[i] = songs[i].NativeHandle;
        }

        CnaResult result;
        fixed (CnaHandle* handlesPtr = handles)
        {
            result = Native.cna_song_collection_create(
                CnaAmbientGame.Current, handlesPtr, (ulong)handles.Length, out CnaHandle collection);

            // Every song stays reachable across the native call -- the collection reads their
            // handles here, and a SafeHandle whose owner became unreachable could have had its
            // critical finalizer release one mid-call.
            GC.KeepAlive(songs);
            CnaException.ThrowIfFailed(result, nameof(SongCollection));
            return collection;
        }
    }
}
