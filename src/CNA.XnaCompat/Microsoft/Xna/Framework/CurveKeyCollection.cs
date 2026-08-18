namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>CurveKeyCollection</c>. Everything -- sorting, duplicate-position
/// rejection, <c>Count</c>, enumeration -- is inherited unchanged from
/// <see cref="CNA.CurveKeyCollection"/>; this exists so the type name resolves in this namespace
/// and so the indexer reports this namespace's <see cref="CurveKey"/>.
///
/// The indexer's getter uses a pattern match rather than a hard cast: a caller can legitimately
/// hand the base collection a base-typed <see cref="CNA.CurveKey"/> (nothing stops it -- the
/// inherited <c>Add</c> takes the base type), and that must not turn a read into an
/// <see cref="InvalidCastException"/>. It wraps such a key in a compat-typed copy instead, which
/// costs reference identity only for keys that were never compat-typed to begin with.
/// </summary>
public class CurveKeyCollection : CNA.CurveKeyCollection
{
    public new CurveKey this[int index]
    {
        get => base[index] as CurveKey ?? Wrap(base[index]);
        set => base[index] = value;
    }

    private static CurveKey Wrap(CNA.CurveKey key) =>
        new(key.Position, key.Value, key.TangentIn, key.TangentOut, (CurveContinuity)(int)key.Continuity);
}
