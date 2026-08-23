using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>AudioEngine</c>: the root of the XACT authored-audio system, loaded from
/// a project settings file (<c>.xgs</c>). <see cref="WaveBank"/>s and <see cref="SoundBank"/>s are
/// loaded against it, and <see cref="Cue"/>s are played from those.
///
/// <see cref="Update"/> must be called once per frame, exactly as in real XNA -- XACT's own
/// per-cue state machine and variable evaluation run there, so without it cues never advance.
/// Unlike <c>MediaPlayer.Update</c>, this is *not* pumped by
/// <see cref="FrameworkDispatcher.Update"/>: an engine is created and owned by game code, and this
/// project has no registry of live engines to walk.
/// </summary>
public class AudioEngine : IDisposable
{
    // Owned: the engine is the XACT dependency root. XNA keeps weak registrations for every
    // Cue/SoundBank/WaveBank and disposes live dependants before releasing the engine.
    private readonly NativeResourceHandle _handle;
    private readonly object _dependantsLock = new();
    private readonly List<WeakReference<IDisposable>> _dependants = [];
    private bool _isDisposing;

    public AudioEngine(string settingsFile)
        : this(Create(settingsFile))
    {
    }

    public AudioEngine(string settingsFile, TimeSpan lookAheadTime, string rendererId)
        : this(Create(settingsFile, lookAheadTime, rendererId))
    {
    }

    private AudioEngine(CnaHandle engine)
    {
        _handle = new NativeResourceHandle(engine.AsNint, h => Native.cna_audio_engine_destroy(new CnaHandle(h)).IsSuccess());
    }

    private static CnaHandle Create(string settingsFile)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);

        CnaHandle engine = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            settingsFile, view => Native.cna_audio_engine_create(CnaAmbientGame.Current, view, out engine));
        CnaException.ThrowIfFailed(result, nameof(AudioEngine));
        return engine;
    }

    private static CnaHandle Create(string settingsFile, TimeSpan lookAheadTime, string rendererId)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);
        // XNA performs no managed null check for rendererId. The C ABI's empty view selects the
        // default renderer, so a null supplied through the strict facade follows that safe route.
        rendererId ??= string.Empty;

        CnaHandle engine = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            settingsFile, settingsView => CnaStringMarshal.WithStringView(
                rendererId, rendererView => Native.cna_audio_engine_create_with_renderer(
                    CnaAmbientGame.Current,
                    settingsView,
                    lookAheadTime.Ticks,
                    rendererView,
                    out engine)));
        CnaException.ThrowIfFailed(result, nameof(AudioEngine));
        return engine;
    }

    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_audio_engine_get_is_disposed(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return value != 0;
        }
    }

    /// <summary>
    /// Every audio renderer this engine describes. Matches real XNA's <c>RendererDetails</c>.
    ///
    /// Re-enumerated on every read rather than cached: the ABI addresses a renderer by index into a
    /// list that reflects the devices present now, so a cached array would go stale the moment one
    /// is plugged in or removed. The header notes this engine currently describes exactly one
    /// renderer -- a fact about the backend, not a shape this property assumes.
    /// </summary>
    public unsafe IReadOnlyList<RendererDetail> RendererDetails
    {
        get
        {
            CnaResult countResult = Native.cna_audio_engine_get_renderer_count(NativeHandle, out ulong count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(countResult, nameof(RendererDetails));

            var details = new RendererDetail[count];
            for (ulong i = 0; i < count; i++)
            {
                string friendlyName = NativeStringReader.ReadIndexed(
                    Native.cna_audio_engine_get_renderer_friendly_name_size,
                    Native.cna_audio_engine_copy_renderer_friendly_name,
                    NativeHandle, i, nameof(RendererDetails));

                string rendererId = NativeStringReader.ReadIndexed(
                    Native.cna_audio_engine_get_renderer_id_size,
                    Native.cna_audio_engine_copy_renderer_id,
                    NativeHandle, i, nameof(RendererDetails));

                CnaResult hashResult = Native.cna_audio_engine_get_renderer_hash_code(NativeHandle, i, out int hash);
                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(hashResult, nameof(RendererDetails));

                details[i] = new RendererDetail(friendlyName, rendererId, hash);
            }

            return details;
        }
    }

    /// <summary>The XACT content version this engine expects, matching real XNA's own
    /// <c>ContentVersion</c> constant. A compile-time constant there and here: it describes the
    /// format, not the engine instance, so there is nothing to ask native about.</summary>
    public const int ContentVersion = 46;

    /// <summary>Advances the audio engine. Call once per frame -- see this class's own doc
    /// comment.</summary>
    public void Update()
    {
        CnaResult result = Native.cna_audio_engine_update(NativeHandle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Update));
    }

    public float GetGlobalVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        float value = 0f;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_get_global_variable(NativeHandle, view, out value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetGlobalVariable));
        return value;
    }

    public void SetGlobalVariable(string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_set_global_variable(NativeHandle, view, value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetGlobalVariable));
    }

    public AudioCategory GetCategory(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaHandle category = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_get_category(NativeHandle, view, out category));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetCategory));
        return new AudioCategory(category.AsNint, this);
    }

    public void Dispose()
    {
        List<IDisposable> liveDependants;
        lock (_dependantsLock)
        {
            if (_isDisposing || _handle.IsClosed)
            {
                return;
            }

            _isDisposing = true;
            liveDependants = _dependants
                .Select(reference => reference.TryGetTarget(out IDisposable? target) ? target : null)
                .Where(target => target is not null)
                .Cast<IDisposable>()
                // Children are registered after their parents. Native refuses a SoundBank release
                // while one of its Cue handles is live, so XNA's child-before-parent order is the
                // reverse of registration order.
                .Reverse()
                .ToList();
            _dependants.Clear();
        }

        // This order is observable in XNA: dependent cues/banks are disposed before the engine.
        Exception? pending = null;
        foreach (IDisposable dependant in liveDependants)
        {
            try
            {
                dependant.Dispose();
            }
            catch (Exception exception)
            {
                pending ??= exception;
            }
        }

        NativeEventBridge? disposingBridge = _disposingBridge;
        _disposingBridge = null;
        _disposingHandler = null;
        _handle.Dispose();

        // Releasing the owned handle raises Disposing. Unsubscribe afterwards so the event is not
        // suppressed, but before returning so the registration cannot keep the engine rooted.
        if (disposingBridge is not null)
        {
            try
            {
                disposingBridge.ThrowPendingException();
            }
            catch (Exception exception)
            {
                pending ??= exception;
            }

            try
            {
                disposingBridge.Dispose();
            }
            catch (Exception exception)
            {
                pending ??= exception;
            }
        }

        GC.SuppressFinalize(this);

        if (pending is not null)
        {
            throw pending;
        }
    }

    internal void RegisterDependant(IDisposable dependant)
    {
        lock (_dependantsLock)
        {
            if (_isDisposing || _handle.IsClosed)
            {
                throw new ObjectDisposedException(nameof(AudioEngine));
            }

            _dependants.RemoveAll(reference => !reference.TryGetTarget(out _));
            _dependants.Add(new WeakReference<IDisposable>(dependant));
        }
    }

    /// <summary>Raised as this audioengine is disposed, matching real XNA. The subscription is
    /// taken on the first <c>+=</c> and released with this object -- see
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> for the shared reasoning.</summary>
    public event EventHandler<EventArgs>? Disposing
    {
        add
        {
            _disposingBridge ??= NativeEventBridge.Subscribe(
                () => _disposingHandler?.Invoke(this, EventArgs.Empty),
                (callback, context) =>
                {
                    CnaResult result = Native.cna_audio_engine_subscribe_disposing_ext(
                        NativeHandle, callback, context, out CnaHandle registration);
                    GC.KeepAlive(this);
                    CnaException.ThrowIfFailed(result, nameof(Disposing));
                    return registration;
                },
                registration => Native.cna_audio_unsubscribe_ext(registration));

            _disposingHandler += value;
        }
        remove => _disposingHandler -= value;
    }

    private NativeEventBridge? _disposingBridge;
    private EventHandler<EventArgs>? _disposingHandler;
}
