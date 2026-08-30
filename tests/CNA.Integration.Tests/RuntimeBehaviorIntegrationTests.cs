using CNA.Audio;
using Xunit;

namespace CNA.Integration.Tests;

/// <summary>Focused regressions for framework work that only becomes observable at a native frame
/// boundary. These deliberately assert exact callback counts: merely seeing at least one callback
/// would not detect the managed/native double-pump regression.</summary>
[Collection(NativeGameCollection.Name)]
public sealed class RuntimeBehaviorIntegrationTests(NativeGameFixture fixture)
{
    /// <summary>
    /// The dispatcher must run exactly once per <c>Update</c>, not twice.
    ///
    /// CNA raises <c>BufferNeeded</c> `MINIMUM_BUFFER_CHECK(3) - PendingBufferCount` times per
    /// dispatcher pump, so one buffer queued and never consumed means two raises per pump. The
    /// assertion is therefore two per update rather than two outright: a fixed-timestep loop
    /// legitimately runs catch-up updates inside one frame when wall time has advanced, and an
    /// earlier flat `Assert.Equal(2, events)` was really asserting that the previous test in this
    /// assembly had finished quickly. It reported six on a Debug-built native library. A restored
    /// managed pump alongside the native one would double the ratio, which is the regression this
    /// exists to catch, and the pending-count assertion keeps the "two" honest instead of assuming
    /// the queue stayed exactly one deep.
    /// </summary>
    [NativeFact]
    public void GameUpdate_PumpsDynamicSoundExactlyOnce()
    {
        DynamicSoundEffectInstance? dynamic = null;
        int events = 0;
        var pendingAtEachRaise = new List<int>();

        fixture.InsideAFrame(_ =>
        {
            dynamic = new DynamicSoundEffectInstance(8000, AudioChannels.Mono);
            dynamic.SubmitBuffer(new byte[16_000]);
            dynamic.BufferNeeded += (_, _) =>
            {
                events++;
                pendingAtEachRaise.Add(dynamic!.PendingBufferCount);
            };
            dynamic.Play();
        });

        Assert.True(fixture.LastFrameUpdateCount >= 1, "The frame ran no update at all.");
        Assert.All(pendingAtEachRaise, pending => Assert.Equal(1, pending));
        Assert.Equal(2 * fixture.LastFrameUpdateCount, events);
        fixture.InsideAFrame(_ =>
        {
            dynamic!.Stop();
            dynamic.Dispose();
        });
    }

    [NativeFact]
    public void DynamicSoundHandlerException_DoesNotUnwindThroughNativeFrame()
    {
        DynamicSoundEffectInstance? dynamic = null;

        fixture.InsideAFrame(_ =>
        {
            dynamic = new DynamicSoundEffectInstance(8000, AudioChannels.Mono);
            dynamic.SubmitBuffer(new byte[16_000]);
            dynamic.BufferNeeded += (_, _) => throw new InvalidOperationException("handler-failure");
            dynamic.Play();
        });

        fixture.InsideAFrame(_ =>
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(dynamic!.Dispose);
            Assert.Equal("handler-failure", exception.Message);
            Assert.True(exception.Data.Contains("CnaNativeEvent"));
        });
    }

    [NativeFact]
    public void GraphicsDeviceReset_RaisesResettingThenResetWithCorrectSender()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            var order = new List<string>();
            bool sendersMatch = true;
            EventHandler<EventArgs> resetting = (sender, _) =>
            {
                sendersMatch &= ReferenceEquals(sender, device);
                order.Add("resetting");
            };
            EventHandler<EventArgs> reset = (sender, _) =>
            {
                sendersMatch &= ReferenceEquals(sender, device);
                order.Add("reset");
            };

            device.DeviceResetting += resetting;
            device.DeviceReset += reset;
            try
            {
                device.Reset();
            }
            finally
            {
                device.DeviceResetting -= resetting;
                device.DeviceReset -= reset;
            }

            Assert.True(sendersMatch);
            Assert.Equal(new[] { "resetting", "reset" }, order);
        });
    }
}
