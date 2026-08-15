using CNA.Interop;

namespace CNA.Framework;

/// <summary>
/// The managed exception every failing native CNA call is converted to. No <c>CnaResult</c> and
/// no native exception ever crosses out of <c>CNA.Interop</c> unconverted -- see
/// ../../cnabinding/analysis_binding.md §10 and plan.md invariant #2.
/// </summary>
public class CnaException : Exception
{
    public CnaException(string message)
        : base(message)
    {
    }

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
            : $"{operation} failed with native result {result}: {detail}");
    }
}
