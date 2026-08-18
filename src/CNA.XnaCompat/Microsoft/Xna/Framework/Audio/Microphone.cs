namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>Microphone</c>. A thin re-typing wrapper rather than a subclass,
/// because <see cref="CNA.Audio.Microphone"/>'s only constructor is private (instances come from
/// the device list, addressed by index -- see that type's doc comment).</summary>
public class Microphone
{
    private readonly CNA.Audio.Microphone _microphone;

    internal Microphone(CNA.Audio.Microphone microphone)
    {
        _microphone = microphone;
    }

    public string Name => _microphone.Name;

    public bool IsHeadset => _microphone.IsHeadset;

    public int SampleRate => _microphone.SampleRate;

    public MicrophoneState State => (MicrophoneState)(int)_microphone.State;

    public TimeSpan BufferDuration
    {
        get => _microphone.BufferDuration;
        set => _microphone.BufferDuration = value;
    }

    public static IReadOnlyList<Microphone> All
    {
        get
        {
            IReadOnlyList<CNA.Audio.Microphone> source = CNA.Audio.Microphone.All;
            var microphones = new Microphone[source.Count];
            for (int i = 0; i < microphones.Length; i++)
            {
                microphones[i] = new Microphone(source[i]);
            }

            return microphones;
        }
    }

    public static Microphone? Default
    {
        get
        {
            CNA.Audio.Microphone? microphone = CNA.Audio.Microphone.Default;
            return microphone is null ? null : new Microphone(microphone);
        }
    }

    public void Start() => _microphone.Start();

    public void Stop() => _microphone.Stop();

    public int GetData(byte[] buffer) => _microphone.GetData(buffer);

    public int GetData(byte[] buffer, int offset, int count) => _microphone.GetData(buffer, offset, count);

    public TimeSpan GetSampleDuration(int sizeInBytes) => _microphone.GetSampleDuration(sizeInBytes);

    public int GetSampleSizeInBytes(TimeSpan duration) => _microphone.GetSampleSizeInBytes(duration);
}
