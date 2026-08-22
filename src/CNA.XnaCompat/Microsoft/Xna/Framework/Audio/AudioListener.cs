namespace Microsoft.Xna.Framework.Audio;

/// <summary>Describes the position and motion of a 3D audio listener.</summary>
public class AudioListener
{
    public Vector3 Position { get; set; }

    public Vector3 Velocity { get; set; }

    public Vector3 Forward { get; set; } = Vector3.Forward;

    public Vector3 Up { get; set; } = Vector3.Up;

    internal CNA.Audio.AudioListener ToFramework() => new()
    {
        Position = new CNA.Vector3(Position.X, Position.Y, Position.Z),
        Velocity = new CNA.Vector3(Velocity.X, Velocity.Y, Velocity.Z),
        Forward = new CNA.Vector3(Forward.X, Forward.Y, Forward.Z),
        Up = new CNA.Vector3(Up.X, Up.Y, Up.Z),
    };
}
