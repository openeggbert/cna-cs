using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectParameterCollection</c>: the parameters of an effect, reached by index or by name.
///
/// An OWNED native collection view -- <c>effects.h</c> mints a fresh one per call rather than
/// aliasing something the effect owns, so this releases it (see <see cref="EffectParameter"/>).
/// Nothing is cached: <see cref="Count"/> and the indexers each round-trip
/// to native, so the collection cannot go stale relative to the effect it belongs to.
/// </summary>
public class EffectParameterCollection : IEnumerable<EffectParameter>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectParameterCollection(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_parameter_collection_destroy(new CnaHandle(h)));
    }

    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    /// <summary>See the element type's own doc comment: this collection view is an owned native
    /// handle, released by its SafeHandle whether or not a caller disposes it.</summary>
    public void Dispose()
    {
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_effect_parameter_collection_get_count(_handle, out ulong count);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public EffectParameter this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            CnaResult result = Native.cna_effect_parameter_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            CnaException.ThrowIfFailed(result, nameof(EffectParameterCollection));
            return new EffectParameter(element);
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectParameter? this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            CnaHandle element = default;
            byte found = 0;
            CnaResult result = CnaStringMarshal.WithStringView(
                name, view => Native.cna_effect_parameter_collection_find_name(_handle, view, out found, out element));
            CnaException.ThrowIfFailed(result, nameof(EffectParameterCollection));

            return found != 0 ? new EffectParameter(element) : null;
        }
    }

    public IEnumerator<EffectParameter> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
