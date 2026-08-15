using System.Text;

namespace CNA.Interop;

/// <summary>
/// Retrieves the native thread-local last-error message set by a failing CNA C ABI call. See
/// ../../cnabinding/analysis_binding.md §10 and ../../cnabinding/analysis_binding_sharp_runtime.md §17.
/// </summary>
internal static class CnaError
{
    public static unsafe string GetLastErrorMessage()
    {
        nuint length = Native.cna_get_last_error_message_length();
        if (length == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[length];
        fixed (byte* bufferPtr = buffer)
        {
            nuint written = Native.cna_copy_last_error_message(bufferPtr, (nuint)buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
