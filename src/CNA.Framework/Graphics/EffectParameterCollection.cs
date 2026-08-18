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

    private readonly GraphicsDevice? _graphicsDevice;

    /// <summary>The device is threaded through purely so
    /// <see cref="EffectParameter.GetValueTexture2D"/> and its siblings can build a real texture
    /// wrapper -- a <see cref="Texture"/> is a <see cref="GraphicsResource"/> and needs one.
    /// Nullable because a collection reached through nested elements or structure members may not
    /// carry it; those parameters report that rather than guessing a device.</summary>
    internal EffectParameterCollection(CnaHandle handle, GraphicsDevice? graphicsDevice = null)
    {
        _graphicsDevice = graphicsDevice;
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_parameter_collection_destroy(new CnaHandle(h)));
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
            GC.KeepAlive(this);
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
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectParameterCollection));
            return new EffectParameter(element, _graphicsDevice);
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
            GC.KeepAlive(this);
                GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectParameterCollection));

            return found != 0 ? new EffectParameter(element, _graphicsDevice) : null;
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
