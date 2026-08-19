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
    /// (<c>CNA_ABI_VERSION_MAJOR</c>/<c>_MINOR</c>/<c>_PATCH</c> = 0.3.0).
    ///
    /// 0.1.0 -> 0.2.0 was the content-reader registration, SpriteFont and launch-parameter routes.
    /// 0.3.0 -> 0.4.0 added the <c>.cnj</c> loader registration, and 0.4.0 -> 0.5.0 the native-window
    /// accessor, both additively.
    /// 0.2.0 -> 0.3.0 is <em>not</em> additive: every route taking a <c>CNA_Bool</c> now refuses a
    /// byte outside {0, 1} with <c>INVALID_ARGUMENT</c>. Sixty-six of ninety-four used to accept
    /// one and then disagree about what it meant -- read as <c>!= CNA_FALSE</c> in some places and
    /// <c>== CNA_TRUE</c> in others, so 9 was true in one route and false in another.
    ///
    /// This binding is unaffected, and that was checked rather than assumed: every Bool it emits
    /// comes from <c>value ? (byte)1 : (byte)0</c> or a literal, and every Bool it reads is
    /// compared <c>!= 0</c>. Only the major component gates compatibility, so none of this ever
    /// blocked anything -- the constant is kept accurate so the integration test's
    /// "native ABI x, binding expects y" line stays worth reading.
    ///
    /// Not to be confused with the ELF symbol version the library exports, which is
    /// <c>CNA_C_API_0.1</c> and deliberately does <em>not</em> track this. Moving a version node on
    /// a minor bump would break every already-linked consumer.
    /// </summary>
    public const uint ExpectedVersion = (0u << 16) | (5u << 8) | 0u;

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

        // A same-major library that is *older* than this binding is missing routes this binding
        // binds, and the symptom is an EntryPointNotFoundException from whichever call happens to
        // reach one first -- naming a symbol rather than the mismatch that caused it. The check is
        // a floor rather than an equality for the reason ABI_VERSIONING.md gives and the installed
        // CMake package enforces with SameMajorVersion: a *newer* minor is additive and fine.
        if (nativeMinor < expectedMinor || (nativeMinor == expectedMinor && nativePatch < expectedPatch))
        {
            throw new CnaException(
                $"The loaded cna-native library implements C ABI {nativeMajor}.{nativeMinor}.{nativePatch}, " +
                $"older than the {expectedMajor}.{expectedMinor}.{expectedPatch} this build of CNA.NET was " +
                "written against. A newer minor is additive and fine; an older one is missing routes this " +
                "binding calls, which would otherwise surface later as a missing entry point.");
        }

        _checked = true;
    }
}
