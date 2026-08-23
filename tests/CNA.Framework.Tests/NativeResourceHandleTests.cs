using System.Runtime.CompilerServices;
using Xunit;

namespace CNA.Framework.Tests;

public sealed class NativeResourceHandleTests
{
    [Fact]
    public void CriticalFinalizer_DefersReleaseToCreationThread()
    {
        var counter = new Counter();
        WeakReference reference = AllocateAndAbandon(counter);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        Assert.False(reference.IsAlive);
        Assert.Equal(0, counter.Value);

        NativeResourceHandle.DrainPendingReleasesForCurrentThread();
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public void CrossThreadDispose_DefersReleaseToCreationThread()
    {
        int releases = 0;
        using var handle = new NativeResourceHandle(new IntPtr(1), _ =>
        {
            releases++;
            return true;
        });

        var thread = new Thread(handle.Dispose);
        thread.Start();
        thread.Join();

        Assert.Equal(0, releases);
        NativeResourceHandle.DrainPendingReleasesForCurrentThread();
        Assert.Equal(1, releases);
    }

    [Fact]
    public void Drain_RetriesFailedParentAfterChildRelease()
    {
        bool childReleased = false;
        int parentAttempts = 0;
        int childAttempts = 0;
        using var parent = new NativeResourceHandle(new IntPtr(1), _ =>
        {
            parentAttempts++;
            return childReleased;
        });
        using var child = new NativeResourceHandle(new IntPtr(2), _ =>
        {
            childAttempts++;
            childReleased = true;
            return true;
        });

        var thread = new Thread(() =>
        {
            parent.Dispose();
            child.Dispose();
        });
        thread.Start();
        thread.Join();

        NativeResourceHandle.DrainPendingReleasesForCurrentThread();

        Assert.Equal(2, parentAttempts);
        Assert.Equal(1, childAttempts);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AllocateAndAbandon(Counter counter)
    {
        var handle = new NativeResourceHandle(new IntPtr(1), _ =>
        {
            counter.Value++;
            return true;
        });
        return new WeakReference(handle);
    }

    private sealed class Counter
    {
        public int Value { get; set; }
    }
}
