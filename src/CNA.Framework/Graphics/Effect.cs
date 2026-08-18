using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Base for stock/built-in shader effects. Custom user-authored <c>.fx</c> shader loading is still
/// not implemented, matching the real openeggbert/cna C++ engine itself (its own bytecode
/// <c>Effect</c> constructor throws <c>NotImplementedException</c> there too).
///
/// Phase 8 WP4a made the reflection surface real. <see cref="CurrentTechnique"/>,
/// <see cref="Techniques"/> and <see cref="Parameters"/> now read the effect's actual native
/// objects (<c>cna_effect_get_current_technique</c>/<c>_get_techniques</c>/<c>_get_parameters</c>)
/// instead of the single fabricated one-pass technique this class used to hand out. That fake
/// existed only so <c>CurrentTechnique.Passes[0].Apply()</c> would compile and route back to
/// <see cref="Apply"/>; it reported a made-up name, always exactly one pass, and no parameters at
/// all, none of which a caller could distinguish from truth.
///
/// <see cref="Apply"/> remains a documented deviation from standard XNA (which exposes only
/// <c>EffectPass.Apply()</c>) that this project's own C++ engine already makes -- both paths reach
/// the same native code.
/// </summary>
public abstract class Effect : IDisposable
{
    protected Effect(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        GraphicsDevice = graphicsDevice;
    }

    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// The native effect handle a concrete subclass owns. Declared here rather than passed to the
    /// constructor because subclasses create their handle through their own
    /// <c>cna_*_effect_create</c> call, and C# evaluates a <c>base(...)</c> argument before any
    /// instance state exists.
    ///
    /// <see langword="virtual"/> with a throwing default rather than <see langword="abstract"/>:
    /// it is <c>private protected</c>, so a subclass outside this assembly could never override it
    /// and an abstract declaration would make <see cref="Effect"/> impossible to subclass at all
    /// from outside -- which would break, among other things, the test doubles
    /// <c>CNA.Framework.Tests</c> builds to exercise <see cref="ModelMesh"/> without a real
    /// effect. Such a subclass simply has no reflection surface, which the message says plainly
    /// instead of failing as a null handle deeper in native code.
    /// </summary>
    /// <summary>Typed <see cref="nint"/>, not <see cref="CnaHandle"/>, and
    /// <c>protected internal</c> rather than <c>private protected</c> -- both so CNA.XnaCompat can
    /// take part. Phase 8 WP4c made <c>Microsoft.Xna.Framework.Graphics.Effect</c> a real base
    /// class of the compat stock effects, which therefore hold a <c>CNA.Graphics</c> effect by
    /// composition and must forward this handle out of it; the <c>internal</c> half of
    /// <c>protected internal</c> is what lets them read it off that inner instance (CNA.Framework
    /// grants <c>InternalsVisibleTo</c> to CNA.XnaCompat), and the <c>protected</c> half is what
    /// lets them override it. Same "raw <see cref="nint"/> across the assembly boundary" rule
    /// <c>GraphicsDevice.NativeGameHandleValue</c> and <c>Texture2D</c>'s handle constructor
    /// already follow -- see docs/architecture.md.</summary>
    protected internal virtual nint NativeEffectHandleValue =>
        throw new NotSupportedException(
            $"{GetType().Name} is not backed by a native CNA effect, so Parameters, Techniques and " +
            "CurrentTechnique are unavailable. Only this project's own stock effects (BasicEffect, " +
            "AlphaTestEffect, DualTextureEffect, EnvironmentMapEffect, SkinnedEffect, EffectMaterial) " +
            "provide one.");

    private CnaHandle NativeEffectHandle => new(NativeEffectHandleValue);

    private EffectParameterCollection? _parameters;
    private EffectTechniqueCollection? _techniques;

    /// <summary>Cached: each read of the underlying native call mints a new owned handle (see
    /// <see cref="EffectParameterCollection"/>), so re-resolving per access would churn native
    /// handles for a collection whose identity never changes over an effect's life.</summary>
    public EffectParameterCollection Parameters
    {
        get
        {
            if (_parameters is null)
            {
                CnaResult result = Native.cna_effect_get_parameters(NativeEffectHandle, out CnaHandle collection);
                CnaException.ThrowIfFailed(result, nameof(Parameters));
                _parameters = new EffectParameterCollection(collection, GraphicsDevice);
            }

            return _parameters;
        }
    }

    /// <summary>Cached for the same reason as <see cref="Parameters"/>.</summary>
    public EffectTechniqueCollection Techniques
    {
        get
        {
            if (_techniques is null)
            {
                CnaResult result = Native.cna_effect_get_techniques(NativeEffectHandle, out CnaHandle collection);
                CnaException.ThrowIfFailed(result, nameof(Techniques));
                _techniques = new EffectTechniqueCollection(collection);
            }

            return _techniques;
        }
    }

    public EffectTechnique CurrentTechnique
    {
        get
        {
            CnaResult result = Native.cna_effect_get_current_technique(NativeEffectHandle, out CnaHandle technique);
            CnaException.ThrowIfFailed(result, nameof(CurrentTechnique));
            return new EffectTechnique(technique);
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_effect_set_current_technique(NativeEffectHandle, value.NativeHandle);
            GC.KeepAlive(value);
            CnaException.ThrowIfFailed(result, nameof(CurrentTechnique));
        }
    }

    public void Apply() => OnApply();

    /// <summary>
    /// An independent copy of this effect, matching real XNA's <c>Effect.Clone</c>.
    ///
    /// Abstract-by-default rather than implemented here: the ABI clones into "an owned clone of the
    /// same concrete native type", so rewrapping the result needs to know which managed class to
    /// build -- which only the concrete effect knows. Every effect this project ships overrides it.
    /// The base throws rather than returning something of the wrong type.
    /// </summary>
    public virtual Effect Clone() =>
        throw new NotSupportedException(
            $"{GetType().Name} does not implement Clone. The native clone route exists " +
            "(cna_effect_clone), but rewrapping its result requires the concrete effect type to " +
            "construct the matching managed class.");

    /// <summary>Clones the native effect and hands back the new handle for a subclass to wrap.
    /// The caller owns it.</summary>
    private protected CnaHandle CloneNativeHandle()
    {
        CnaResult result = Native.cna_effect_clone(new CnaHandle(NativeEffectHandleValue), out CnaHandle clone);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Clone));
        return clone;
    }

    protected abstract void OnApply();

    public virtual void Dispose()
    {
        _parameters?.Dispose();
        _techniques?.Dispose();
    }
}
