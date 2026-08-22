using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>Microphone</c>. A thin re-typing wrapper rather than a subclass,
/// because <see cref="CNA.Audio.Microphone"/>'s only constructor is private (instances come from
/// the device list, addressed by index -- see that type's doc comment).
///
/// One wrapper per underlying microphone, cached, for the reason the CNA type caches its own:
/// <see cref="BufferReady"/> makes an instance stateful, so handing out a fresh wrapper per read of
/// <see cref="All"/> would make <c>Microphone.Default.BufferReady += h</c> subscribe an object the
/// caller can never reach again. The cache is weak on the underlying instance, so dropping the CNA
/// cache at game disposal drops these too rather than pinning them for the process.</summary>
public sealed class Microphone
{
    private static readonly ConditionalWeakTable<CNA.Audio.Microphone, Microphone> Wrappers = [];

    private readonly CNA.Audio.Microphone _microphone;
    private readonly object _bufferReadyLock = new();
    private EventHandler<EventArgs>? _bufferReady;
    private bool _forwarding;

    internal Microphone(CNA.Audio.Microphone microphone)
    {
        _microphone = microphone;
        Name = microphone.Name;
    }

    ~Microphone()
    {
    }

    private static Microphone Wrap(CNA.Audio.Microphone microphone) =>
        Wrappers.GetValue(microphone, static m => new Microphone(m));

    /// <summary>Raised when captured audio is ready to read. Forwards the CNA event, re-raising it
    /// with this wrapper as the sender rather than passing the inner object through -- an XNA
    /// handler that casts <c>sender</c> to <c>Microphone</c> means this one.</summary>
    public event EventHandler<EventArgs>? BufferReady
    {
        add
        {
            lock (_bufferReadyLock)
            {
                if (!_forwarding)
                {
                    _forwarding = true;
                    _microphone.BufferReady += (_, e) => _bufferReady?.Invoke(this, e);
                }

                _bufferReady += value;
            }
        }
        remove
        {
            lock (_bufferReadyLock)
            {
                _bufferReady -= value;
            }
        }
    }

    public readonly string Name;

    public bool IsHeadset => _microphone.IsHeadset;

    public int SampleRate => _microphone.SampleRate;

    public MicrophoneState State => (MicrophoneState)(int)_microphone.State;

    public TimeSpan BufferDuration
    {
        get => _microphone.BufferDuration;
        set => _microphone.BufferDuration = value;
    }

    public static ReadOnlyCollection<Microphone> All
    {
        get
        {
            IReadOnlyList<CNA.Audio.Microphone> source = CNA.Audio.Microphone.All;
            var microphones = new Microphone[source.Count];
            for (int i = 0; i < microphones.Length; i++)
            {
                microphones[i] = Wrap(source[i]);
            }

            return Array.AsReadOnly(microphones);
        }
    }

    public static Microphone? Default
    {
        get
        {
            CNA.Audio.Microphone? microphone = CNA.Audio.Microphone.Default;
            return microphone is null ? null : Wrap(microphone);
        }
    }

    public void Start() => _microphone.Start();

    public void Stop() => _microphone.Stop();

    public int GetData(byte[] buffer) => _microphone.GetData(buffer);

    public int GetData(byte[] buffer, int offset, int count) => _microphone.GetData(buffer, offset, count);

    public TimeSpan GetSampleDuration(int sizeInBytes) => _microphone.GetSampleDuration(sizeInBytes);

    public int GetSampleSizeInBytes(TimeSpan duration) => _microphone.GetSampleSizeInBytes(duration);
}
