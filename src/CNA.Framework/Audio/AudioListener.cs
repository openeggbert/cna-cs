using CNA.Interop;

namespace CNA.Audio;

/// <summary>Matches real XNA's <c>AudioListener</c>: where the "ears" are for 3D audio. A plain
/// value holder -- the native <c>CNA_AudioListener</c> is a versioned struct passed by pointer to
/// each 3D call, not a resource, so nothing here is handle-backed.</summary>
public class AudioListener
{
    public Vector3 Position { get; set; }

    public Vector3 Forward { get; set; } = Vector3.Forward;

    public Vector3 Up { get; set; } = Vector3.Up;

    public Vector3 Velocity { get; set; }

    internal CnaAudioListener ToNative() => new()
    {
        Forward = Forward.ToNative(),
        Position = Position.ToNative(),
        Up = Up.ToNative(),
        Velocity = Velocity.ToNative(),
    };
}
