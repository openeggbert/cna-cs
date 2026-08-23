using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectAnnotationCollection</c>: the annotations attached to a parameter, technique or pass, reached by index or by name.
///
/// An owned native collection view. Elements are cached by index because each native lookup mints
/// another owned handle while XNA exposes stable managed objects.
/// </summary>
public class EffectAnnotationCollection : IEnumerable<EffectAnnotation>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;
    private readonly Dictionary<int, EffectAnnotation> _byIndex = [];

    internal EffectAnnotationCollection(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_annotation_collection_destroy(new CnaHandle(h)).IsSuccess());
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
        foreach (EffectAnnotation annotation in _byIndex.Values)
        {
            annotation.Dispose();
        }

        _byIndex.Clear();
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_effect_annotation_collection_get_count(_handle, out ulong count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public EffectAnnotation this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                return null!;
            }

            if (_byIndex.TryGetValue(index, out EffectAnnotation? existing))
            {
                return existing;
            }

            CnaResult result = Native.cna_effect_annotation_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(EffectAnnotationCollection));
            var created = new EffectAnnotation(element);
            _byIndex[index] = created;
            return created;
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectAnnotation? this[string name]
    {
        get
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                EffectAnnotation annotation = this[i];
                if (annotation.Name == name)
                {
                    return annotation;
                }
            }

            return null;
        }
    }

    public IEnumerator<EffectAnnotation> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
