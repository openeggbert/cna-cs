using CNA.Interop;

namespace CNA;

/// <summary>
/// The managed exception every failing native CNA call is converted to. No <c>CnaResult</c> and
/// no native exception ever crosses out of <c>CNA.Interop</c> unconverted -- see
/// openeggbert/cna's analysis_binding.md §10 and plan.md invariant #2.
/// </summary>
public class CnaException : Exception
{
    public CnaException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The name of the native result that caused this, or null when it did not come from one.
    ///
    /// A caller frequently needs to tell "this renderer cannot do that" from "this went wrong",
    /// and until now the only way was to read the message. The *name* rather than the value,
    /// because <c>CnaResult</c> is a CNA.Interop type and no CNA.Interop type may appear in a
    /// public signature -- the invariant this class exists to serve in the first place.
    /// </summary>
    public string? NativeResult { get; private init; }

    public CnaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    internal static void ThrowIfFailed(CnaResult result, string operation)
    {
        if (result.IsSuccess())
        {
            return;
        }

        string detail = CnaError.GetLastErrorMessage();
        throw new CnaException(string.IsNullOrEmpty(detail)
            ? $"{operation} failed with native result {result}."
            : $"{operation} failed with native result {result}: {detail}")
        {
            NativeResult = result.ToString(),
        };
    }
}
