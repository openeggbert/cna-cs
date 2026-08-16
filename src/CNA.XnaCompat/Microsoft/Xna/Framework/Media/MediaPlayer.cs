namespace Microsoft.Xna.Framework.Media;

/// <summary>Thin forwarding static class over <c>CNA.Media.MediaPlayer</c>, same pattern this
/// compat layer's other static subsystems (<c>Mouse</c>, <c>Keyboard</c>) already use.</summary>
public static class MediaPlayer
{
    public static MediaState State => (MediaState)CNA.Media.MediaPlayer.State;

    public static float Volume
    {
        get => CNA.Media.MediaPlayer.Volume;
        set => CNA.Media.MediaPlayer.Volume = value;
    }

    public static bool IsMuted
    {
        get => CNA.Media.MediaPlayer.IsMuted;
        set => CNA.Media.MediaPlayer.IsMuted = value;
    }

    public static bool IsRepeating
    {
        get => CNA.Media.MediaPlayer.IsRepeating;
        set => CNA.Media.MediaPlayer.IsRepeating = value;
    }

    public static TimeSpan PlayPosition => CNA.Media.MediaPlayer.PlayPosition;

    public static void Play(Song song) => CNA.Media.MediaPlayer.Play(song);

    public static void Pause() => CNA.Media.MediaPlayer.Pause();

    public static void Resume() => CNA.Media.MediaPlayer.Resume();

    public static void Stop() => CNA.Media.MediaPlayer.Stop();
}
