using CNA.Interop;

namespace CNA;

/// <summary>
/// The native library's ABI version, and the check that the loaded one is usable.
///
/// <c>cna_get_abi_version</c> (<c>abi.h:114</c>) exists precisely so a binding can refuse a
/// mismatched library, and it was bound but never called -- so a native library from a different
/// ABI generation would have been used anyway, and failed later as a garbled struct or a wrong
/// handle rather than as a clear message. A sweep of every declaration against the headers turned
/// it up alongside two fabricated ones.
/// </summary>
public static class CnaAbi
{
    /// <summary>
    /// The ABI this binding was written against
    /// (<c>CNA_ABI_VERSION_MAJOR</c>/<c>_MINOR</c>/<c>_PATCH</c> = 0.2.0).
    ///
    /// Bumped from 0.1.0 when upstream added the content-reader registration, SpriteFont and
    /// launch-parameter routes. Only the major component gates compatibility, so this was never a
    /// blocker -- it is kept accurate so the integration test's log line
    /// ("native ABI x, binding expects y") stays worth reading.
    ///
    /// Not to be confused with the ELF symbol version the library exports, which is
    /// <c>CNA_C_API_0.1</c> and deliberately does <em>not</em> track this. Moving a version node on
    /// a minor bump would break every already-linked consumer.
    /// </summary>
    public const uint ExpectedVersion = (0u << 16) | (2u << 8) | 0u;

    private static bool _checked;

    /// <summary>The encoded version the loaded native library reports.</summary>
    public static uint NativeVersion => Native.cna_get_abi_version();

    public static (int Major, int Minor, int Patch) Decode(uint encoded) =>
        ((int)((encoded >> 16) & 0xFFFF), (int)((encoded >> 8) & 0xFF), (int)(encoded & 0xFF));

    /// <summary>
    /// Throws unless the loaded library's ABI is compatible with what this binding expects.
    ///
    /// Compatible means the same major version. Minor and patch may differ: the encoding reserves
    /// 16 bits for major and 8 each for the rest, which is the usual "major breaks, minor adds"
    /// split, and refusing on a patch bump would make the binding brittle for no safety gain.
    ///
    /// Runs once. Every subsequent call is free, so <see cref="Game"/> can call it on construction
    /// without paying for it per game.
    /// </summary>
    public static void EnsureCompatible()
    {
        if (_checked)
        {
            return;
        }

        uint native = NativeVersion;
        (int expectedMajor, int expectedMinor, int expectedPatch) = Decode(ExpectedVersion);
        (int nativeMajor, int nativeMinor, int nativePatch) = Decode(native);

        if (nativeMajor != expectedMajor)
        {
            throw new CnaException(
                $"The loaded cna-native library implements C ABI {nativeMajor}.{nativeMinor}.{nativePatch}, " +
                $"but this build of CNA.NET was written against {expectedMajor}.{expectedMinor}.{expectedPatch}. " +
                "Major versions differ, so the two are not interoperable -- every struct layout and handle " +
                "convention in this binding assumes the version it was built against.");
        }

        _checked = true;
    }
}
