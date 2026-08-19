using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Shared base for the objects reached through a <see cref="MediaLibrary"/>
/// (<see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/<see cref="Playlist"/>/
/// <see cref="Picture"/>/<see cref="PictureAlbum"/>).
///
/// All six have the identical ownership story, stated by <c>media_library.h</c> for each of them:
/// the object itself belongs to the library and is untouched by anything done here, but the
/// *handle* is the caller's to release, and holding one keeps the library alive. So each wrapper
/// owns its handle in a <see cref="NativeResourceHandle"/> -- releasing it is a handle-table
/// operation, not a destroy of the album/artist/etc. behind it.
///
/// <c>Dispose</c> means two different things on these types, which is worth being explicit about
/// because the ABI separates them and XNA does not. The canonical <c>Dispose</c> only sets a flag
/// ("an album is a view into the library's data and owns nothing of its own -- every other member
/// keeps answering"), which is <c>cna_*_dispose</c>; releasing the handle is <c>cna_*_destroy</c>.
/// <see cref="Dispose"/> here does both, in that order, matching what an XNA caller means by
/// disposing one of these.
/// </summary>
public abstract class MediaLibraryObject : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly DisposeFunc _dispose;
    private readonly IsDisposedFunc _isDisposed;

    private protected MediaLibraryObject(
        CnaHandle handle, DisposeFunc dispose, IsDisposedFunc isDisposed, Action<CnaHandle> destroy)
    {
        _dispose = dispose;
        _isDisposed = isDisposed;
        _handle = new NativeResourceHandle(handle.AsNint, h => destroy(new CnaHandle(h)));
    }

    private protected delegate CnaResult DisposeFunc(CnaHandle handle);

    private protected delegate CnaResult IsDisposedFunc(CnaHandle handle, out byte outDisposed);

    /// <summary>See <see cref="MediaLibrary"/> for why every handle read is paired with
    /// <see cref="GC.KeepAlive(object)"/>.</summary>
    private protected CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>Reports the *canonical* disposed flag, read from native rather than tracked here --
    /// so it stays true even when something else in the library disposed this object. Answers
    /// <see langword="true"/> once the handle has been released too, since at that point the
    /// question can no longer be asked.</summary>
    public bool IsDisposed
    {
        get
        {
            if (_handle.IsClosed || _handle.IsInvalid)
            {
                return true;
            }

            CnaResult result = _isDisposed(NativeHandle, out byte disposed);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return disposed != 0;
        }
    }

    public void Dispose()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return;
        }

        // Canonical disposal first (a flag on the library-owned object), then the handle release.
        // The other order would ask native to flag an object through a handle already given back.
        // Failure is deliberately ignored: Dispose must not throw, and a library torn down first
        // makes this an ordinary, harmless failure rather than something a caller can act on.
        DisposeCachedChildren();

        _dispose(NativeHandle);
        GC.KeepAlive(this);
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases this wrapper's handle <b>without</b> the canonical disposal that
    /// <see cref="Dispose"/> performs first.
    ///
    /// The distinction is load-bearing and easy to miss. <c>cna_*_dispose</c> sets a flag on the
    /// library-owned object, which every other handle to that same album or song observes;
    /// <c>cna_*_destroy</c> only gives this handle back. A collection cleaning up the element
    /// handles it handed out wants the second and emphatically not the first -- otherwise merely
    /// disposing an <see cref="AlbumCollection"/> would mark those albums disposed for every other
    /// reader of the same library, which is not what disposing a view means.
    ///
    /// <see cref="Dispose"/> stays the caller-facing operation and keeps doing both, matching XNA.
    /// </summary>
    internal void ReleaseHandleOnly()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Reads a name through the ABI's two-call size/copy pattern, keeping this object
    /// alive across both halves.</summary>
    private protected string ReadName(
        NativeStringReader.SizeFunc size, NativeStringReader.CopyFunc copy, string context)
    {
        string value = NativeStringReader.Read(size, copy, NativeHandle, context);
        GC.KeepAlive(this);
        return value;
    }

    /// <summary>The <c>(out handle, out available)</c> shape the ABI uses for a relationship that
    /// may be absent -- an album with no artist, a picture with no album. Absence is an ordinary
    /// answer there, so it becomes <see langword="null"/> here rather than an exception.</summary>
    private protected TResult? ReadOptional<TResult>(
        OptionalFunc getter, Func<CnaHandle, TResult> wrap, string context)
        where TResult : class
    {
        CnaResult result = getter(NativeHandle, out CnaHandle value, out byte available);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return available != 0 ? wrap(value) : null;
    }

    private protected delegate CnaResult OptionalFunc(CnaHandle handle, out CnaHandle outValue, out byte outAvailable);

    private protected TResult ReadRequired<TResult>(
        RequiredFunc getter, Func<CnaHandle, TResult> wrap, string context)
    {
        CnaResult result = getter(NativeHandle, out CnaHandle value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return wrap(value);
    }

    private readonly Dictionary<string, IDisposable> _cachedChildren = [];

    /// <summary>
    /// The same read, but each property answers one wrapper for the life of this object.
    ///
    /// Every one of these getters mints a <em>new owned</em> native handle, so returning a fresh
    /// wrapper per read leaked one native collection per call --
    /// <c>library.Pictures.Count</c> in a loop leaks per iteration. Identical in shape to the leak
    /// already fixed in the effect family, and identical in consequence: a media library is a
    /// game-child resource, so a game cannot be destroyed while the leaked collections are alive,
    /// and the failure surfaces as an unrelated game failing to create.
    ///
    /// Keyed on the caller's property name, which is why every call site passes
    /// <c>nameof(...)</c>. Released with this object, and released rather than *disposed* where the
    /// element is library-owned -- see <see cref="ReleaseHandleOnly"/> for why the two differ.
    /// </summary>
    private protected TResult ReadCachedChild<TResult>(
        RequiredFunc getter, Func<CnaHandle, TResult> wrap, string context)
        where TResult : class, IDisposable
    {
        if (_cachedChildren.TryGetValue(context, out IDisposable? existing))
        {
            return (TResult)existing;
        }

        TResult created = ReadRequired(getter, wrap, context);
        _cachedChildren[context] = created;
        return created;
    }

    private protected void DisposeCachedChildren()
    {
        foreach (IDisposable child in _cachedChildren.Values)
        {
            child.Dispose();
        }

        _cachedChildren.Clear();
    }

    private protected delegate CnaResult RequiredFunc(CnaHandle handle, out CnaHandle outValue);

    private protected long ReadTicks(TicksFunc getter, string context)
    {
        CnaResult result = getter(NativeHandle, out long ticks);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return ticks;
    }

    private protected delegate CnaResult TicksFunc(CnaHandle handle, out long outTicks);

    private protected int ReadInt(IntFunc getter, string context)
    {
        CnaResult result = getter(NativeHandle, out int value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return value;
    }

    private protected delegate CnaResult IntFunc(CnaHandle handle, out int outValue);

    private protected bool ReadBool(BoolFunc getter, string context)
    {
        CnaResult result = getter(NativeHandle, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return value != 0;
    }

    private protected delegate CnaResult BoolFunc(CnaHandle handle, out byte outValue);

    private protected byte[]? ReadBlob(
        NativeBlobReader.SizeFunc size, NativeBlobReader.CopyFunc copy, string context)
    {
        byte[]? value = NativeBlobReader.Read(size, copy, NativeHandle, context);
        GC.KeepAlive(this);
        return value;
    }

    /// <summary>Equality goes through the ABI's own <c>_equals</c> rather than being reimplemented
    /// from names, because two handles to the same library object are two different handle values.
    /// Comparing those, or reconstructing the canonical rule managed-side (album equality is by
    /// name *and* artist; artist, genre and playlist by name alone), would be a second definition
    /// that can drift from the one native applies.</summary>
    private protected bool NativeEquals(EqualsFunc equals, MediaLibraryObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        CnaResult result = equals(NativeHandle, other.NativeHandle, out byte equal);
        GC.KeepAlive(this);
        GC.KeepAlive(other);
        CnaException.ThrowIfFailed(result, nameof(Equals));
        return equal != 0;
    }

    private protected delegate CnaResult EqualsFunc(CnaHandle left, CnaHandle right, out byte outEqual);
}
