namespace Microsoft.Xna.Framework.Input;

/// <summary>
/// Reproduces XNA's private <c>Helpers.SmartGetHashCode</c> routine for its input value types.
/// XNA XORs the raw four-byte words and substitutes <see cref="int.MaxValue"/> for an all-zero
/// result. Keeping this internal avoids turning an implementation detail into facade metadata.
/// </summary>
internal static class XnaInputHash
{
    internal static int Smart(params int[] words)
    {
        int result = 0;
        foreach (int word in words)
        {
            result ^= word;
        }

        return result == 0 ? int.MaxValue : result;
    }

    internal static int FloatBits(float value) => BitConverter.SingleToInt32Bits(value);
}
