using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectPassCollection</c>: the passes of a technique, reached by index or by name.
///
/// An owned native collection view. Elements are cached by index because each native lookup mints
/// another owned handle while XNA exposes stable managed objects.
/// </summary>
public class EffectPassCollection : IEnumerable<EffectPass>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectPassCollection(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_pass_collection_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>.
    ///
    /// Every caller pairs this with <see cref="GC.KeepAlive(object)"/> after the native call. That
    /// is not decoration: these wrappers are routinely temporaries -- <c>effect.Parameters["World"]
    /// .SetValue(m)</c> leaves the <see cref="EffectParameter"/> unreachable the moment its handle
    /// has been read -- and the moment they are unreachable the <see cref="System.Runtime.InteropServices.SafeHandle"/>
    /// finalizer is free to run <c>destroy</c> while the native call is still in flight. Giving
    /// these types SafeHandle ownership is what fixed their leak; it is also what introduced this
    /// hazard, since before that they held a bare handle with no finalizer at all.
    ///
    /// <see cref="GC.KeepAlive(object)"/> closes the ordinary reachability hazard. In addition,
    /// <see cref="NativeResourceHandle"/> defers finalizer-thread and cross-thread releases to an
    /// owner-thread safe point, so an unreachable wrapper cannot destroy this raw handle during a
    /// native call. This project still does not promise concurrent <c>Dispose</c>/operation safety.
    /// </summary>
    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    /// <summary>See the element type's own doc comment: this collection view is an owned native
    /// handle, released by its SafeHandle whether or not a caller disposes it.</summary>
    /// <summary>
    /// Per-index wrapper cache. Each read of the native accessor mints a *new owned* handle, so
    /// returning a fresh wrapper per read leaked one native object per call -- and
    /// `effect.CurrentTechnique.Passes[0].Apply()`, which is ordinary XNA written once per frame,
    /// leaked three.
    ///
    /// That is not merely untidy. Native refuses to destroy a game while a resource created against
    /// it is alive, so the leak surfaced as `cna_game_destroy` answering INVALID_STATE and the
    /// *next* game failing to create. Relying on SafeHandle finalizers could not fix it: XNA's
    /// EffectTechnique and EffectPass are not IDisposable, no ported game disposes them, and the GC
    /// has no idea about native's ordering rule.
    ///
    /// Same shape ReadOnlyMediaCollection already uses for library-owned elements: one wrapper per
    /// index, owned by this collection, released with it.
    /// </summary>
    private readonly Dictionary<int, EffectPass> _byIndex = [];

    public void Dispose()
    {
        foreach (EffectPass cached in _byIndex.Values)
        {
            cached.Dispose();
        }

        _byIndex.Clear();
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_effect_pass_collection_get_count(_handle, out ulong count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public EffectPass this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                return null!;
            }

            if (_byIndex.TryGetValue(index, out EffectPass? existing))
            {
                return existing;
            }

            CnaResult result = Native.cna_effect_pass_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectPassCollection));
            var created = new EffectPass(element);
            _byIndex[index] = created;
            return created;
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectPass? this[string name]
    {
        get
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                EffectPass pass = this[i];
                if (pass.Name == name)
                {
                    return pass;
                }
            }

            return null;
        }
    }

    public IEnumerator<EffectPass> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
