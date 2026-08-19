using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectTechniqueCollection</c>: the techniques of an effect, reached by index or by name.
///
/// A borrowed view over a native collection the effect owns -- see <see cref="EffectTechnique"/>
/// for the ownership rule. Nothing is cached: <see cref="Count"/> and the indexers each round-trip
/// to native, so the collection cannot go stale relative to the effect it belongs to.
/// </summary>
public class EffectTechniqueCollection : IEnumerable<EffectTechnique>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectTechniqueCollection(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_technique_collection_destroy(new CnaHandle(h)));
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
    /// <see cref="GC.KeepAlive(object)"/> rather than
    /// <see cref="System.Runtime.InteropServices.SafeHandle.DangerousAddRef"/>/<c>DangerousRelease</c>:
    /// it closes the reachability hazard, which is the real one here, but it does not make a
    /// concurrent <c>Dispose</c> from another thread safe. Nothing in this project is thread-safe,
    /// so that is consistent rather than a new gap -- and the ref-counted form is what
    /// <c>plan.md</c> WP17 will apply project-wide.
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
    private readonly Dictionary<int, EffectTechnique> _byIndex = [];

    private int _syntheticKey = -1;

    /// <summary>By-name lookups have no stable index to key on, so each gets its own negative slot.
    /// They still land in the cache, which is what matters: the collection owns them and releases
    /// them, rather than leaving a native object for a finalizer that runs too late.</summary>
    private int NextSyntheticKey() => _syntheticKey--;

    private EffectTechnique Adopt(int key, CnaHandle handle)
    {
        if (_byIndex.TryGetValue(key, out EffectTechnique? existing))
        {
            return existing;
        }

        var created = new EffectTechnique(handle);
        _byIndex[key] = created;
        return created;
    }

    public void Dispose()
    {
        foreach (EffectTechnique cached in _byIndex.Values)
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
            CnaResult result = Native.cna_effect_technique_collection_get_count(_handle, out ulong count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public EffectTechnique this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            CnaResult result = Native.cna_effect_technique_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectTechniqueCollection));
            return Adopt(index, element);
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectTechnique? this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            CnaHandle element = default;
            byte found = 0;
            CnaResult result = CnaStringMarshal.WithStringView(
                name, view => Native.cna_effect_technique_collection_find(_handle, view, out found, out element));
            GC.KeepAlive(this);
                GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectTechniqueCollection));

            return found != 0 ? Adopt(NextSyntheticKey(), element) : null;
        }
    }

    public IEnumerator<EffectTechnique> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
