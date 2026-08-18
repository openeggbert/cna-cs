namespace CNA.Storage;

/// <summary>
/// The <see cref="IAsyncResult"/> the <c>BeginXxx</c> methods on <see cref="StorageDevice"/> and
/// <see cref="StorageContainer"/> return.
///
/// Already complete when constructed, and deliberately so: the C API states outright that it
/// collapses XNA's <c>BeginXxx</c>/<c>EndXxx</c> pairs into one synchronous call because CNA
/// "completes synchronously; no operation handle is invented for work that never pends"
/// (<c>storage.h:125-127</c>). Faking a thread pool here would add latency and failure modes for
/// work that has already finished by the time this object exists. <see cref="CompletedSynchronously"/>
/// is <see langword="true"/>, which is exactly how a caller is supposed to detect this.
/// </summary>
internal sealed class CompletedAsyncResult<T> : IAsyncResult, IDisposable
{
    private readonly ManualResetEvent _waitHandle = new(initialState: true);

    internal CompletedAsyncResult(T result, object? asyncState)
    {
        Result = result;
        AsyncState = asyncState;
    }

    internal T Result { get; }

    public object? AsyncState { get; }

    public WaitHandle AsyncWaitHandle => _waitHandle;

    public bool CompletedSynchronously => true;

    public bool IsCompleted => true;

    public void Dispose() => _waitHandle.Dispose();
}
