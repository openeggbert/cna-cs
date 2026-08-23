using System.Globalization;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace XnaCompatibilityCompileProbe;

/// <summary>
/// Device-independent XNA audio observations. Constructor success paths are intentionally kept in
/// the native probe; every constructor case here is guaranteed to fail during managed validation,
/// before any implementation can try to open an audio device.
/// </summary>
public static class AudioBehaviorCorpus
{
    public static IReadOnlyList<string> Capture()
    {
        var observations = new List<string>
        {
            $"audio.enum.channels={(int)AudioChannels.Mono},{(int)AudioChannels.Stereo}",
            $"audio.enum.state={(int)SoundState.Playing},{(int)SoundState.Paused},{(int)SoundState.Stopped}",
            $"audio.enum.stop={(int)AudioStopOptions.AsAuthored},{(int)AudioStopOptions.Immediate}",
            $"audio.enum.microphone_state={(int)MicrophoneState.Started},{(int)MicrophoneState.Stopped}",
        };

        // MonoGame DesktopGL does not ship XACT's RendererDetail at all. Resolve it dynamically so
        // that absence becomes a deterministic comparator observation instead of preventing the
        // rest of this same-source corpus from compiling there.
        Type? rendererType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Microsoft.Xna.Framework.Audio.RendererDetail", throwOnError: false))
            .FirstOrDefault(type => type is not null);
        if (rendererType is null)
        {
            observations.Add("audio.renderer.default=unsupported");
            observations.Add("audio.renderer.equals=unsupported");
        }
        else
        {
            object renderer = Activator.CreateInstance(rendererType)!;
            string? friendlyName = (string?)rendererType.GetProperty("FriendlyName")!.GetValue(renderer);
            string? rendererId = (string?)rendererType.GetProperty("RendererId")!.GetValue(renderer);
            observations.Add($"audio.renderer.default={friendlyName ?? "<null>"},{rendererId ?? "<null>"}");

            MethodInfo equality = rendererType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static)!;
            MethodInfo inequality = rendererType.GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static)!;
            observations.Add("audio.renderer.equals=" + ValueOrException(() =>
                $"{Flag((bool)equality.Invoke(null, [renderer, renderer])!)}," +
                $"{Flag((bool)inequality.Invoke(null, [renderer, renderer])!)}"));
        }

        var listener = new AudioListener();
        observations.Add("audio.listener.position=" + Vector(listener.Position));
        observations.Add("audio.listener.velocity=" + Vector(listener.Velocity));
        observations.Add("audio.listener.forward=" + Vector(listener.Forward));
        observations.Add("audio.listener.up=" + Vector(listener.Up));

        var emitter = new AudioEmitter();
        observations.Add("audio.emitter.position=" + Vector(emitter.Position));
        observations.Add("audio.emitter.velocity=" + Vector(emitter.Velocity));
        observations.Add("audio.emitter.forward=" + Vector(emitter.Forward));
        observations.Add("audio.emitter.up=" + Vector(emitter.Up));
        observations.Add("audio.emitter.doppler.default=" + Bits(emitter.DopplerScale));
        observations.Add("audio.emitter.doppler.negative=" + ExceptionName(() => emitter.DopplerScale = -1f));
        observations.Add("audio.emitter.doppler.nan=" + ExceptionName(() => emitter.DopplerScale = float.NaN));

        observations.Add("audio.duration.zero=" + SoundEffect.GetSampleDuration(0, 8000, AudioChannels.Mono).Ticks);
        observations.Add("audio.duration.mono=" + SoundEffect.GetSampleDuration(88200, 44100, AudioChannels.Mono).Ticks);
        observations.Add("audio.duration.stereo=" + SoundEffect.GetSampleDuration(88200, 44100, AudioChannels.Stereo).Ticks);
        observations.Add("audio.duration.partial=" + SoundEffect.GetSampleDuration(3, 44100, AudioChannels.Mono).Ticks);
        observations.Add("audio.duration.rounding=" + SoundEffect.GetSampleDuration(10, 8000, AudioChannels.Mono).Ticks);
        observations.Add("audio.duration.negative=" + ExceptionName(
            () => SoundEffect.GetSampleDuration(-1, 44100, AudioChannels.Mono)));
        observations.Add("audio.duration.rate.low=" + ExceptionName(
            () => SoundEffect.GetSampleDuration(2, 7999, AudioChannels.Mono)));
        observations.Add("audio.duration.rate.high=" + ExceptionName(
            () => SoundEffect.GetSampleDuration(2, 48001, AudioChannels.Mono)));
        observations.Add("audio.duration.rate.order=" + ExceptionName(
            () => SoundEffect.GetSampleDuration(-1, 7999, (AudioChannels)0)));
        observations.Add("audio.duration.channels=" + ExceptionName(
            () => SoundEffect.GetSampleDuration(2, 44100, (AudioChannels)0)));

        observations.Add("audio.size.zero=" + SoundEffect.GetSampleSizeInBytes(TimeSpan.Zero, 8000, AudioChannels.Mono));
        observations.Add("audio.size.mono=" + SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(1), 44100, AudioChannels.Mono));
        observations.Add("audio.size.stereo=" + SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(1), 44100, AudioChannels.Stereo));
        observations.Add("audio.size.rounding=" + SoundEffect.GetSampleSizeInBytes(TimeSpan.FromTicks(10_000), 44100, AudioChannels.Stereo));
        observations.Add("audio.size.negative=" + ExceptionName(
            () => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromTicks(-1), 44100, AudioChannels.Mono)));
        observations.Add("audio.size.overflow=" + ExceptionName(
            () => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(int.MaxValue), 48000, AudioChannels.Stereo)));
        observations.Add("audio.size.rate.order=" + ExceptionName(
            () => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromTicks(-1), 7999, (AudioChannels)0)));

        byte[] mono = new byte[16];
        observations.Add("audio.ctor.basic.null=" + ExceptionName(
            () => _ = new SoundEffect(null!, 44100, AudioChannels.Mono)));
        observations.Add("audio.ctor.basic.empty=" + ExceptionName(
            () => _ = new SoundEffect(Array.Empty<byte>(), 44100, AudioChannels.Mono)));
        observations.Add("audio.ctor.rate_before_buffer=" + ExceptionName(
            () => _ = new SoundEffect(null!, 0, 0, 7999, (AudioChannels)0, 0, 0)));
        observations.Add("audio.ctor.channels_before_buffer=" + ExceptionName(
            () => _ = new SoundEffect(null!, 0, 0, 44100, (AudioChannels)0, 0, 0)));
        observations.Add("audio.ctor.unaligned_buffer=" + ExceptionName(
            () => _ = new SoundEffect(new byte[3], 0, 2, 44100, AudioChannels.Mono, 0, 0)));
        observations.Add("audio.ctor.offset=" + ExceptionName(
            () => _ = new SoundEffect(mono, 1, 2, 44100, AudioChannels.Mono, 0, 0)));
        observations.Add("audio.ctor.count=" + ExceptionName(
            () => _ = new SoundEffect(mono, 0, 3, 44100, AudioChannels.Mono, 0, 0)));
        observations.Add("audio.ctor.range_overflow=" + ExceptionName(
            () => _ = new SoundEffect(mono, int.MaxValue - 5, 20, 44100, AudioChannels.Mono, 0, 0)));
        observations.Add("audio.ctor.loop_negative=" + ExceptionName(
            () => _ = new SoundEffect(mono, 0, mono.Length, 44100, AudioChannels.Mono, -1, 0)));
        observations.Add("audio.ctor.loop_past_end=" + ExceptionName(
            () => _ = new SoundEffect(mono, 0, mono.Length, 44100, AudioChannels.Mono, 7, 2)));
        observations.Add("audio.ctor.loop_overflow=" + ExceptionName(
            () => _ = new SoundEffect(mono, 0, mono.Length, 44100, AudioChannels.Mono, int.MaxValue, 1)));

        observations.Add("audio.dynamic.rate=" + ExceptionName(
            () => _ = new DynamicSoundEffectInstance(7999, AudioChannels.Mono)));
        observations.Add("audio.dynamic.channels=" + ExceptionName(
            () => _ = new DynamicSoundEffectInstance(44100, (AudioChannels)0)));

        return observations;
    }

    private static string Vector(Vector3 value) =>
        $"{Bits(value.X)},{Bits(value.Y)},{Bits(value.Z)}";

    private static int Flag(bool value) => value ? 1 : 0;

    private static string Bits(float value) =>
        unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0))
            .ToString("X8", CultureInfo.InvariantCulture);

    private static string ExceptionName(Action action)
    {
        try
        {
            action();
            return "none";
        }
        catch (Exception exception)
        {
            string parameter = exception is ArgumentException argument && argument.ParamName is not null
                ? ":" + argument.ParamName
                : string.Empty;
            return exception.GetType().Name + parameter;
        }
    }

    private static string ValueOrException(Func<string> action)
    {
        try
        {
            return action();
        }
        catch (Exception exception)
        {
            return exception is TargetInvocationException { InnerException: Exception inner }
                ? inner.GetType().Name
                : exception.GetType().Name;
        }
    }
}
