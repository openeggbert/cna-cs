using System.Runtime.InteropServices;
using CNA.Interop;
using Xunit;

namespace CNA.Framework.Tests;

public sealed class NativeEventBridgeTests
{
    [Fact]
    public void HandlerException_IsContainedUntilManagedBoundary()
    {
        nint callbackPointer = 0;
        nint context = 0;
        var expected = new InvalidOperationException("handler");
        using NativeEventBridge bridge = NativeEventBridge.Subscribe(
            () => throw expected,
            (callback, callbackContext) =>
            {
                callbackPointer = callback;
                context = callbackContext;
                return new CnaHandle(1);
            },
            _ => { });

        NativeCallback callback = Marshal.GetDelegateForFunctionPointer<NativeCallback>(callbackPointer);
        callback(context);

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(bridge.ThrowPendingException));
        bridge.ThrowPendingException();
    }

    [Fact]
    public void UnsubscribeFailure_RetainsCallbackRootAndRetriesSafely()
    {
        nint callbackPointer = 0;
        nint context = 0;
        int handlerCalls = 0;
        int unsubscribeCalls = 0;
        NativeEventBridge bridge = NativeEventBridge.Subscribe(
            () => handlerCalls++,
            (callback, callbackContext) =>
            {
                callbackPointer = callback;
                context = callbackContext;
                return new CnaHandle(1);
            },
            _ =>
            {
                if (++unsubscribeCalls == 1)
                {
                    throw new InvalidOperationException("unsubscribe");
                }
            });

        Assert.Throws<InvalidOperationException>(bridge.Dispose);

        // Native still owns the registration, so its context must remain a valid GCHandle. The
        // disposed bridge intentionally suppresses delivery until unsubscription can be retried.
        NativeCallback callback = Marshal.GetDelegateForFunctionPointer<NativeCallback>(callbackPointer);
        callback(context);
        Assert.Equal(0, handlerCalls);

        bridge.Dispose();
        Assert.Equal(2, unsubscribeCalls);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeCallback(nint context);
}
