namespace Microsoft.Xna.Framework.Audio;

/// <summary>
/// XNA 4.0-compatible <c>SoundEffectInstance</c>. <c>Play</c>/<c>Pause</c>/<c>Resume</c>/
/// <c>Stop</c>/<c>Volume</c>/<c>Pitch</c>/<c>Pan</c>/<c>IsLooped</c>/<c>Dispose</c> are all
/// inherited unchanged from <see cref="CNA.Audio.SoundEffectInstance"/> -- every one of those
/// members' types (<c>float</c>, <c>bool</c>) needs no conversion. <c>State</c> needs a `new`
/// override since <c>SoundState</c> is an enum (no conversion operators possible), same reason
/// <c>Buttons</c>/<c>Keys</c>/<c>SpriteEffects</c> need explicit casts at their own boundaries.
/// Like real XNA, has no public constructor -- only reachable via
/// <see cref="SoundEffect.CreateInstance"/>.
/// </summary>
public class SoundEffectInstance : CNA.Audio.SoundEffectInstance
{
    protected internal SoundEffectInstance(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    public new SoundState State => (SoundState)(int)base.State;

    /// <summary>Inherited behaviour, re-typed only so the compat
    /// <see cref="AudioListener"/>/<see cref="AudioEmitter"/> bind -- they subclass the CNA ones,
    /// so this could have been inherited unchanged; it is declared for discoverability alongside
    /// the rest of this namespace's audio surface.</summary>
    public void Apply3D(AudioListener listener, AudioEmitter emitter) => base.Apply3D(listener, emitter);

}
