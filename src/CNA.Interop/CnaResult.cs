namespace CNA.Interop;

/// <summary>
/// Mirrors the native <c>CNA_Result</c> enum from the CNA stable C ABI.
/// See ../../cnabinding/analysis_binding.md §8, §14.
/// </summary>
internal enum CnaResult : int
{
    Ok = 0,
    ErrorInvalidArgument = 1,
    ErrorInvalidHandle = 2,
    ErrorOutOfMemory = 3,
    ErrorIo = 4,
    ErrorInternal = 5,
    ErrorNotSupported = 6,
    ErrorAbiMismatch = 7,
}

internal static class CnaResultExtensions
{
    public static bool IsSuccess(this CnaResult result) => result == CnaResult.Ok;
    public static bool IsFailure(this CnaResult result) => result != CnaResult.Ok;
}
