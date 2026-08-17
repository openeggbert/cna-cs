namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Result</c> exactly
/// (<c>typedef uint32_t CNA_Result;</c> plus its <c>CNA_RESULT_*</c> value macros, <c>core.h:47-92</c>)
/// -- values and names both confirmed against real header text, not guessed from analysis docs (the
/// prior version of this enum, guessed before any real ABI existed, had a different, incompatible
/// value set entirely: only 8 values including a nonexistent <c>ErrorAbiMismatch</c>; the real ABI
/// has 15 -- see <c>NEXT.md</c>'s native-ABI-migration entry). <c>uint</c> underlying type to match
/// <c>CNA_Result</c>'s own <c>uint32_t</c> exactly, even though every currently-defined value also
/// fits an <c>int</c> without loss.
/// </summary>
internal enum CnaResult : uint
{
    Success = 0,
    InvalidArgument = 1,
    InvalidHandle = 2,
    InvalidState = 3,
    OutOfMemory = 4,
    Io = 5,
    NotSupported = 6,
    Platform = 7,
    Thread = 8,
    Callback = 9,
    Overflow = 10,
    Encoding = 11,
    Internal = 12,
    ShuttingDown = 13,
    BufferTooSmall = 14,
}

internal static class CnaResultExtensions
{
    public static bool IsSuccess(this CnaResult result) => result == CnaResult.Success;
    public static bool IsFailure(this CnaResult result) => result != CnaResult.Success;
}
