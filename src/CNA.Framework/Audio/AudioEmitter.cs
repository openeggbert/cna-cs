using CNA.Interop;

namespace CNA.Audio;

/// <summary>Matches real XNA's <c>AudioEmitter</c>: where a 3D sound is coming from. See
/// <see cref="AudioListener"/> for why this is a plain value holder.</summary>
public class AudioEmitter
{
    public Vector3 Position { get; set; }

    public Vector3 Forward { get; set; } = Vector3.Forward;

    public Vector3 Up { get; set; } = Vector3.Up;

    public Vector3 Velocity { get; set; }

    /// <summary>Multiplies the Doppler shift this emitter's motion produces. <c>1</c> is
    /// physically neutral, matching real XNA's default.</summary>
    public float DopplerScale { get; set; } = 1f;

    internal CnaAudioEmitter ToNative() => new()
    {
        DopplerScale = DopplerScale,
        Forward = Forward.ToNative(),
        Position = Position.ToNative(),
        Up = Up.ToNative(),
        Velocity = Velocity.ToNative(),
    };
}
