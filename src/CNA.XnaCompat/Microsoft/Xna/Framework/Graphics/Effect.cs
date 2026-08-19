namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>Effect</c>: the base every stock effect in this namespace derives from,
/// so <c>Effect e = someBasicEffect;</c> and an <c>Effect</c>-typed field compile exactly as they
/// do in XNA.
///
/// Added by Phase 8 WP4c, and it cost a real architectural decision. Until then the compat stock
/// effects derived straight from their <c>CNA.Graphics</c> counterparts to inherit ~87
/// native-backed members between them; C# single inheritance means they cannot do that *and*
/// derive from this class. Deriving here instead means each holds its CNA counterpart by
/// composition and forwards, which is a documented, deliberate exception to
/// docs/architecture.md's "no duplicated logic for reference types" rule -- the forwards carry no
/// logic, but there are a lot of them.
///
/// The exception is contained by one thing: <see cref="CNA.Graphics.Effect.NativeEffectHandleValue"/>
/// is overridden to report the *inner* effect's handle, so this object and the one it wraps share
/// a single native effect. Without that they would be two effects with independently drifting
/// state -- the bug class this project has already fixed twice (see
/// <c>GraphicsDevice.Indices</c> and <c>ModelEffectCollection</c>).
/// </summary>
public class Effect : CNA.Graphics.Effect
{
    private protected Effect(GraphicsDevice graphicsDevice, CNA.Graphics.Effect inner)
        : base(graphicsDevice)
    {
        Inner = inner;
    }

    /// <summary>
    /// Compiled Effect Framework bytecode -- real XNA's <c>Effect(GraphicsDevice, byte[])</c>.
    ///
    /// Concrete for the same reason the CNA base is: XNA's <c>Effect</c> is not abstract, and this
    /// constructor is how a game loads a shader it read from disk itself. Composes a CNA effect
    /// rather than deriving one, matching every other compat effect, so there is exactly one owner
    /// of the native handle.
    /// </summary>
    public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
        : this(graphicsDevice, new CNA.Graphics.Effect(graphicsDevice, effectCode))
    {
    }

    /// <summary>Wraps an already-loaded CNA effect -- the compat <c>Load&lt;Effect&gt;</c> route's
    /// landing point. A factory rather than a constructor because the constructor with this
    /// signature is <c>private protected</c>, which the compat <c>ContentManager</c> cannot reach:
    /// it is in this assembly but is not a subclass.</summary>
    internal static Effect Adopt(GraphicsDevice graphicsDevice, CNA.Graphics.Effect inner) =>
        new(graphicsDevice, inner);

    /// <summary>The CNA effect this one delegates to. Every forwarding member below reads it, and
    /// it is the single owner of the native effect -- see this class's own doc comment.
    /// <c>internal</c> rather than <c>private protected</c> so the model builders in this assembly
    /// can hand it to <c>CNA.Content</c>'s shared effect-populating helpers, which take the CNA
    /// type; that reuse is what keeps those helpers from being duplicated compat-side.</summary>
    internal CNA.Graphics.Effect Inner { get; }

    /// <summary>Reports the inner effect's handle, so <c>Parameters</c>/<c>Techniques</c>/
    /// <c>CurrentTechnique</c> inherited from the base read the same native effect the forwarding
    /// members below drive.</summary>
    protected internal override nint NativeEffectHandleValue => Inner.NativeEffectHandleValue;

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;

    protected override void OnApply() => Inner.Apply();

    public override void Dispose()
    {
        Inner.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Matches real XNA's <c>Effect.Clone</c>. Throws on this base, as on the CNA one.
    ///
    /// The compat effects <em>compose</em> their <c>CNA.Graphics</c> counterpart rather than
    /// deriving from it, so cloning has to build both halves: the native clone and a matching compat
    /// wrapper around it. Only a concrete effect knows which pair to build, and every concrete
    /// compat effect overrides this.
    ///
    /// <b>Returns this namespace's <see cref="Effect"/>, not the CNA one</b>, through a covariant
    /// return. It used to return the base type, so ported source writing
    /// <c>Effect clone = effect.Clone();</c> did not compile -- it got a
    /// <c>CNA.Graphics.Effect</c> where it wanted this one, and needed a cast XNA never asks for.
    /// Found the first time the compat layer was compiled against, which was also the first time it
    /// ran: a member whose *return type* is wrong is invisible to a type-level diff and to any test
    /// written against the other layer.
    /// </summary>
    public override Effect Clone() =>
        throw new NotSupportedException(
            $"{GetType().Name} does not implement Clone. Cloning a compat effect needs both the " +
            "native clone and a matching compat wrapper, which only the concrete effect type can build.");

}
