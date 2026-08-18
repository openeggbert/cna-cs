using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectParameterCollection</c>: the parameters of an effect, reached by index or by name.
///
/// A borrowed view over a native collection the effect owns -- see <see cref="EffectParameter"/>
/// for the ownership rule. Nothing is cached: <see cref="Count"/> and the indexers each round-trip
/// to native, so the collection cannot go stale relative to the effect it belongs to.
/// </summary>
public class EffectParameterCollection : IEnumerable<EffectParameter>
{
    private readonly CnaHandle _handle;

    internal EffectParameterCollection(CnaHandle handle)
    {
        _handle = handle;
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
