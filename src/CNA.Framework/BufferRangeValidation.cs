namespace CNA;

/// <summary>
/// Shared overflow-safe "does [startIndex, startIndex + elementCount) fit within a buffer of
/// this length" validation -- extracted after the same block was copy-pasted five times across
/// <c>SoundEffect</c>'s constructor and <c>VertexBuffer</c>/<c>IndexBuffer</c>'s <c>SetData</c>/
/// <c>GetData</c> methods. Checked as <c>startIndex &gt; length || elementCount &gt; length -
/// startIndex</c> rather than <c>startIndex + elementCount &gt; length</c> -- the addition form
/// can integer-overflow for adversarial inputs and wrap negative, silently passing validation it
/// should fail (see <c>SoundEffect.cs</c>'s own fix for this exact bug earlier in this project's
/// history). This form can't overflow: once <c>startIndex &lt;= length</c> is established,
/// <c>length - startIndex</c> is a safe, non-negative subtraction.
/// </summary>
internal static class BufferRangeValidation
{
    public static void ValidateRange(int length, int startIndex, int elementCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        if (startIndex > length || elementCount > length - startIndex)
        {
            throw new ArgumentException(
                $"{nameof(startIndex)} ({startIndex}) + {nameof(elementCount)} ({elementCount}) exceeds the buffer length ({length}).");
        }
    }
}
