using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Bridges a native <c>void(void* context)</c> event callback to a managed delegate.
///
/// Several unrelated parts of the C API share exactly this callback shape -- graphics-device
/// manager events, dynamic-sound buffer-needed, microphone buffer-ready -- and each of them needs
/// the same three things done right, which is why this exists once rather than three times:
///
/// <list type="number">
/// <item>A <see cref="GCHandle"/> rooting the managed target, because native holds only a raw
/// context pointer and nothing else keeps the delegate alive.</item>
/// <item>A total callback. These handlers return <c>void</c> -- there is no error channel at all --
/// so an exception escaping into native code is undefined behaviour. Everything is caught, and the
/// first failure is surfaced through <see cref="ThrowPendingException"/> rather than swallowed, the
/// same bargain <see cref="GameComponent"/> makes for the same reason.</item>
/// <item>Unsubscription paired with freeing the root. Leaving the registration alive after the
/// <see cref="GCHandle"/> is freed would leave native calling into a dangling context.</item>
/// </list>
/// </summary>
internal sealed class NativeEventBridge : IDisposable
{
    private readonly Action _handler;
    private readonly Action<CnaHandle> _unsubscribe;
    private GCHandle _selfHandle;
    private CnaHandle _registration;
    private bool _disposed;

    private NativeEventBridge(Action handler, Action<CnaHandle> unsubscribe)
    {
        _handler = handler;
        _unsubscribe = unsubscribe;
        _selfHandle = GCHandle.Alloc(this);
    }

    private Exception? _pendingException;
    private int _suppressedFailureCount;

    /// <summary>
    /// Rethrows and clears the first exception the managed handler threw, if any. Callers invoke
    /// this from their own managed-initiated members -- see <see cref="GameComponent"/>'s doc
    /// comment for why surfacing late is the best this callback shape allows.
    ///
    /// The ORIGINAL exception is rethrown rather than a wrapper, for the reason
    /// <see cref="GameComponent"/> records: this is reachable from inside a handler, so a wrapper
    /// would be re-captured and grow one <c>InnerException</c> layer per raise, without bound. The
    /// explanation goes in <see cref="Exception.Data"/>, which survives a rethrow.
    /// </summary>
    internal void ThrowPendingException()
    {
        if (_pendingException is null)
        {
            return;
        }

        Exception exception = _pendingException;
        _pendingException = null;
        int suppressed = _suppressedFailureCount;
        _suppressedFailureCount = 0;

        exception.Data["CnaNativeEvent"] =
            "Thrown from a native CNA event handler. That callback returns void, so there is no " +
            "error channel at the point of failure; the exception was captured and rethrown at the " +
            "next managed-initiated call on its owner." +
            (suppressed > 0 ? $" {suppressed} later failure(s) on this subscription were dropped." : string.Empty);
        throw exception;
    }

    /// <summary>
    /// Subscribes, wiring <paramref name="handler"/> to the native event.
    ///
    /// <paramref name="subscribe"/> receives the function pointer and context to register and
    /// returns the registration handle; <paramref name="unsubscribe"/> releases it. Passing both in
    /// keeps this type independent of which subsystem it is bridging -- the audio and game event
    /// families have separate subscribe/unsubscribe pairs despite the identical callback shape.
    /// </summary>
    internal static unsafe NativeEventBridge Subscribe(
        Action handler,
        Func<nint, nint, CnaHandle> subscribe,
        Action<CnaHandle> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var bridge = new NativeEventBridge(handler, unsubscribe);
        try
        {
            bridge._registration = subscribe(
                (nint)(delegate* unmanaged[Cdecl]<nint, void>)&OnNativeEvent,
                GCHandle.ToIntPtr(bridge._selfHandle));
        }
        catch
        {
            bridge.Dispose();
            throw;
        }

        return bridge;
    }

    /// <summary>
    /// The <c>void(sender, context)</c> variant. A few event families
    /// (<c>CNA_VertexBufferContentLostCallback</c>, <c>CNA_IndexBufferContentLostCallback</c>) pass
    /// the resource handle alongside the context; it is ignored here, because the managed handler
    /// is already bound to the one object that resource belongs to. Everything else -- the
    /// <see cref="GCHandle"/> root, the totality, the unsubscribe ordering -- is shared with
    /// <see cref="Subscribe"/>.
    /// </summary>
    internal static unsafe NativeEventBridge SubscribeWithSender(
        Action handler,
        Func<nint, nint, CnaHandle> subscribe,
        Action<CnaHandle> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var bridge = new NativeEventBridge(handler, unsubscribe);
        try
        {
            bridge._registration = subscribe(
                (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&OnNativeEventWithSender,
                GCHandle.ToIntPtr(bridge._selfHandle));
        }
        catch
        {
            bridge.Dispose();
            throw;
        }

        return bridge;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnNativeEventWithSender(nint sender, nint context) => Dispatch(context);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnNativeEvent(nint context) => Dispatch(context);

    /// <summary>The shared body of both entry points. Separate from them because a method carrying
    /// <see cref="UnmanagedCallersOnlyAttribute"/> cannot be called directly -- only through a
    /// function pointer -- so the two-argument shim needs something ordinary to forward
    /// to.</summary>
    private static void Dispatch(nint context)
    {
        NativeEventBridge? bridge = null;
        try
        {
            if (context == 0)
            {
                return;
            }

            GCHandle handle = GCHandle.FromIntPtr(context);
            if (!handle.IsAllocated || handle.Target is not NativeEventBridge resolved)
            {
                return;
            }

            bridge = resolved;
            bridge._handler();
        }
        catch (Exception ex)
        {
            // Nothing can be reported from here -- the signature returns void -- and letting this
            // escape into native code would kill the process. Keep the first failure for the owner
            // to surface; a freed or recycled GCHandle simply makes the callback a no-op.
            if (bridge is null)
            {
                return;
            }

            // Keeps the FIRST failure, not the latest: once a handler is broken the later throws
            // are usually consequences of the first. Every later one is counted so the rethrow can
            // say how many were dropped rather than implying there was only ever one.
            if (bridge._pendingException is null)
            {
                bridge._pendingException = ex;
            }
            else
            {
                bridge._suppressedFailureCount++;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe BEFORE freeing the root: the other order leaves native holding a context
        // pointer into a freed GCHandle for as long as the registration lives.
        if (_registration.Value != 0)
        {
            _unsubscribe(_registration);
            _registration = CnaHandle.Zero;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }
}
