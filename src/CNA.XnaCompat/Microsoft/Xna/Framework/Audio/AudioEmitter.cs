namespace Microsoft.Xna.Framework.Audio;

/// <summary>Describes the position and motion of a 3D sound source.</summary>
public class AudioEmitter
{
    private float _dopplerScale = 1f;

    public Vector3 Position { get; set; }

    public Vector3 Velocity { get; set; }

    public Vector3 Forward { get; set; } = Vector3.Forward;

    public Vector3 Up { get; set; } = Vector3.Up;

    public float DopplerScale
    {
        get => _dopplerScale;
        set
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _dopplerScale = value;
        }
    }

    internal CNA.Audio.AudioEmitter ToFramework() => new()
    {
        Position = new CNA.Vector3(Position.X, Position.Y, Position.Z),
        Velocity = new CNA.Vector3(Velocity.X, Velocity.Y, Velocity.Z),
        Forward = new CNA.Vector3(Forward.X, Forward.Y, Forward.Z),
        Up = new CNA.Vector3(Up.X, Up.Y, Up.Z),
        DopplerScale = DopplerScale,
    };
}
