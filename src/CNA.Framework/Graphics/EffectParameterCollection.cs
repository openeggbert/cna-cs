using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectParameterCollection</c>: the parameters of an effect, reached by index or by name.
///
/// An owned native collection view. Elements are cached by index because each native lookup mints
/// another owned handle while XNA exposes stable managed objects.
/// </summary>
public class EffectParameterCollection : IEnumerable<EffectParameter>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<int, EffectParameter> _byIndex = [];

    /// <summary>The device is threaded through purely so
    /// <see cref="EffectParameter.GetValueTexture2D"/> and its siblings can build a real texture
    /// wrapper -- a <see cref="Texture"/> is a <see cref="GraphicsResource"/> and needs one.
    /// Required, not optional: every construction site has a device to pass, so an optional
    /// parameter only made it possible to forget one.</summary>
    internal EffectParameterCollection(CnaHandle handle, GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _graphicsDevice = graphicsDevice;
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_parameter_collection_destroy(new CnaHandle(h)).IsSuccess());
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
    public void Dispose()
    {
        foreach (EffectParameter parameter in _byIndex.Values)
        {
            parameter.Dispose();
        }

        _byIndex.Clear();
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
            if (index < 0 || index >= Count)
            {
                return null!;
            }

            if (_byIndex.TryGetValue(index, out EffectParameter? existing))
            {
                return existing;
            }

            CnaResult result = Native.cna_effect_parameter_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectParameterCollection));
            var created = new EffectParameter(element, _graphicsDevice);
            _byIndex[index] = created;
            return created;
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectParameter? this[string name]
    {
        get
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                EffectParameter parameter = this[i];
                if (parameter.Name == name)
                {
                    return parameter;
                }
            }

            return null;
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
