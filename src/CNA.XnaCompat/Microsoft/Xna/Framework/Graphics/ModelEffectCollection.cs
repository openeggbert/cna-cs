using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>ModelEffectCollection</c>.
///
/// This closes what <c>ModelMesh</c>'s own doc comment previously called a permanent gap: the
/// compat mirror had no <c>ModelEffectCollection</c> because <c>CNA.Graphics.ModelMesh</c>
/// constructs its collection at field-initializer time with no override seam. The fix is to stop
/// needing a seam -- this wraps the already-constructed base collection instead of replacing it,
/// so there is exactly one collection and no two pieces of state that can desync. It is the same
/// resolution <see cref="DirectionalLight"/> uses for the same problem.
///
/// Element type is <see cref="CNA.Graphics.Effect"/>, not a compat <c>Effect</c>: the compat stock
/// effects derive from their <c>CNA.Graphics</c> counterparts rather than from a parallel compat
/// <c>Effect</c> base (see <see cref="BasicEffect"/>'s doc comment), so that is the type they
/// actually share. Tracked in <c>plan.md</c> WP4c.
/// </summary>
public class ModelEffectCollection : IEnumerable<CNA.Graphics.Effect>
{
    private readonly CNA.Graphics.ModelEffectCollection _effects;

    internal ModelEffectCollection(CNA.Graphics.ModelEffectCollection effects)
    {
        _effects = effects;
    }

    public CNA.Graphics.Effect this[int index] => _effects[index];

    public int Count => _effects.Count;

    public bool Contains(CNA.Graphics.Effect effect) => _effects.Contains(effect);

    public IEnumerator<CNA.Graphics.Effect> GetEnumerator() => ((IEnumerable<CNA.Graphics.Effect>)_effects).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
