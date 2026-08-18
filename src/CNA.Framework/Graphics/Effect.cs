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
    private protected virtual CnaHandle NativeEffectHandle =>
        throw new NotSupportedException(
            $"{GetType().Name} is not backed by a native CNA effect, so Parameters, Techniques and " +
            "CurrentTechnique are unavailable. Only this assembly's own stock effects (BasicEffect, " +
            "AlphaTestEffect, DualTextureEffect, EnvironmentMapEffect, SkinnedEffect) provide one.");

    public EffectParameterCollection Parameters
    {
        get
        {
            CnaResult result = Native.cna_effect_get_parameters(NativeEffectHandle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Parameters));
            return new EffectParameterCollection(collection);
        }
    }

    public EffectTechniqueCollection Techniques
    {
        get
        {
            CnaResult result = Native.cna_effect_get_techniques(NativeEffectHandle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Techniques));
            return new EffectTechniqueCollection(collection);
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
            CnaException.ThrowIfFailed(result, nameof(CurrentTechnique));
        }
    }

    public void Apply() => OnApply();

    protected abstract void OnApply();

    public virtual void Dispose()
    {
    }
}
