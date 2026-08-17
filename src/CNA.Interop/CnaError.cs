using System.Text;

namespace CNA.Interop;

/// <summary>
/// Retrieves the calling thread's most recent native CNA C API error message, matching the real,
/// shipped openeggbert/cna C API's own two-call size-then-copy pattern exactly
/// (<c>cna_error_get_last_message_size</c> + <c>cna_error_copy_last_message</c>,
/// <c>core.h:156-178</c>). A prior version of this type called a guessed, single-call shape
/// (<c>cna_get_last_error_message_length</c>/<c>cna_copy_last_error_message</c>, both returning a
/// byte count directly) that has no real equivalent -- both real functions instead return
/// <see cref="CnaResult"/>, which this method treats as best-effort: a failure here must never
/// itself throw, since <c>CnaException.ThrowIfFailed</c> calls this while already reporting a
/// different, primary failure.
/// </summary>
internal static class CnaError
{
    public static unsafe string GetLastErrorMessage()
    {
        CnaResult sizeResult = Native.cna_error_get_last_message_size(out ulong length);
        if (sizeResult.IsFailure() || length == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[length];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = Native.cna_error_copy_last_message(bufferPtr, length, out ulong written);
            if (copyResult.IsFailure())
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
