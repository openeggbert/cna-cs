using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Interop;
using Xunit;

namespace CNA.Tests;

/// <summary>Compile-time/runtime layout evidence for the interop shapes exercised by the new
/// audio, XACT, media, video, and device-event paths. These values are ELF/Linux x64 evidence;
/// they are intentionally not presented as PE/x86 or Mach-O evidence.</summary>
public sealed class InteropLayoutTests
{
    [Fact]
    public void AudioCreateAndInstanceInfo_MatchNativeLayout()
    {
        Assert.Equal(24, Marshal.SizeOf<CnaSoundEffectCreateInfo>());
        Assert.Equal(16, Offset<CnaSoundEffectCreateInfo>(nameof(CnaSoundEffectCreateInfo.Reserved)));

        Assert.Equal(32, Marshal.SizeOf<CnaSoundEffectInstanceInfo>());
        Assert.Equal(8, Offset<CnaSoundEffectInstanceInfo>(nameof(CnaSoundEffectInstanceInfo.State)));
        Assert.Equal(12, Offset<CnaSoundEffectInstanceInfo>(nameof(CnaSoundEffectInstanceInfo.IsLooped)));
        Assert.Equal(16, Offset<CnaSoundEffectInstanceInfo>(nameof(CnaSoundEffectInstanceInfo.Volume)));
        Assert.Equal(28, Offset<CnaSoundEffectInstanceInfo>(nameof(CnaSoundEffectInstanceInfo.Reserved1)));
    }

    [Fact]
    public void ListenerEmitterAndCue_MatchNativeLayout()
    {
        Assert.Equal(56, Marshal.SizeOf<CnaAudioListener>());
        Assert.Equal(8, Offset<CnaAudioListener>(nameof(CnaAudioListener.Forward)));
        Assert.Equal(44, Offset<CnaAudioListener>(nameof(CnaAudioListener.Velocity)));

        Assert.Equal(60, Marshal.SizeOf<CnaAudioEmitter>());
        Assert.Equal(8, Offset<CnaAudioEmitter>(nameof(CnaAudioEmitter.DopplerScale)));
        Assert.Equal(48, Offset<CnaAudioEmitter>(nameof(CnaAudioEmitter.Velocity)));

        Assert.Equal(16, Marshal.SizeOf<CnaCueInfo>());
        Assert.Equal(8, Offset<CnaCueInfo>(nameof(CnaCueInfo.IsCreated)));
        Assert.Equal(15, Offset<CnaCueInfo>(nameof(CnaCueInfo.IsStopping)));
    }

    [Fact]
    public void VisualizationInlineBuffers_AreContiguous()
    {
        Assert.Equal(1024, Marshal.SizeOf<CnaFloatBuffer256>());
        Assert.Equal(2056, Marshal.SizeOf<CnaVisualizationData>());
        Assert.Equal(8, Offset<CnaVisualizationData>(nameof(CnaVisualizationData.Frequencies)));
        Assert.Equal(1032, Offset<CnaVisualizationData>(nameof(CnaVisualizationData.Samples)));
    }

    [Fact]
    public void PointerWidthHandlesStringViewsAndCallbacks_MatchElfX64()
    {
        Assert.Equal(8, IntPtr.Size);
        Assert.Equal(8, Marshal.SizeOf<CnaHandle>());
        Assert.Equal(16, Marshal.SizeOf<CnaStringView>());
        Assert.Equal(8, Offset<CnaStringView>(nameof(CnaStringView.ByteLength)));

        Assert.Equal(56, Marshal.SizeOf<CnaManagedGameCallbacks>());
        Assert.Equal(8, Offset<CnaManagedGameCallbacks>(nameof(CnaManagedGameCallbacks.LoadContent)));
        Assert.Equal(48, Offset<CnaManagedGameCallbacks>(nameof(CnaManagedGameCallbacks.Context)));
    }

    [Fact]
    public void InteropEnumsHaveNativeUint32Width()
    {
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(CnaResult)));
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(CnaGraphicsDeviceEvent)));
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(CnaGraphicsProfile)));
        // Marshal.SizeOf deliberately rejects enums even though their unmanaged width is defined
        // by the underlying type. Unsafe.SizeOf measures the actual in-memory representation.
        Assert.Equal(4, Unsafe.SizeOf<CnaResult>());
    }

    [Fact]
    public void GraphicsDeviceEventPrototype_CarriesSenderAndContext()
    {
        MethodInfo method = typeof(Native).GetMethod(
            "cna_graphics_device_subscribe_event",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal(typeof(CnaResult), method.ReturnType);
        Assert.Equal(5, parameters.Length);
        Assert.Equal(typeof(CnaHandle), parameters[0].ParameterType);
        Assert.Equal(typeof(uint), parameters[1].ParameterType);
        Assert.Equal(typeof(nint), parameters[2].ParameterType);
        Assert.Equal(typeof(nint), parameters[3].ParameterType);
        Assert.Equal(typeof(CnaHandle).MakeByRefType(), parameters[4].ParameterType);
        Assert.True(parameters[4].IsOut);
    }

    private static int Offset<T>(string field) where T : struct =>
        checked((int)Marshal.OffsetOf<T>(field));
}
