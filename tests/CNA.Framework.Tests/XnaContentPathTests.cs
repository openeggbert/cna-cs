using CNA.Content;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// Locks down the asset-identity rules an external reference resolves through.
///
/// Every expectation here was derived by reading XNA 4.0's own decompiled
/// <c>TitleContainer.GetCleanPath</c> and <c>ContentReader.GetPathToReference</c> and executing
/// them on paper, not by asking this implementation what it does. Three of them contradict the
/// segment-splitting approximation that used to stand in for <c>GetCleanPath</c> in
/// <c>CNA.XnaCompat</c>, which is the reason they are written out individually rather than as one
/// round-trip.
/// </summary>
public sealed class XnaContentPathTests
{
    [Theory]
    // The ordinary cases.
    [InlineData("Models/Ship", "Models\\Ship")]
    [InlineData("Models\\Ship", "Models\\Ship")]
    [InlineData("Models\\.\\Ship", "Models\\Ship")]
    [InlineData(".\\Models\\Ship", "Models\\Ship")]
    [InlineData("Models\\Sub\\..\\Ship", "Models\\Ship")]
    [InlineData("Models\\Sub\\Deep\\..\\..\\Ship", "Models\\Ship")]
    [InlineData("Models\\Ship\\.", "Models\\Ship")]
    // Surprising and correct: a trailing ".." that cancels a *nested* segment leaves the separator
    // behind, because XNA removes from just after the preceding separator rather than from it.
    // Cancelling the *first* segment leaves nothing, because there is no preceding separator to
    // start after. This asymmetry is XNA's, measured by executing the decompiled method by hand,
    // and it is transcribed rather than tidied -- a cleaner answer here would be a different asset
    // name from the one XNA looks up.
    [InlineData("Models\\Ship\\..", "Models\\")]
    [InlineData("Models\\..", "")]
    [InlineData(".", "")]
    [InlineData("", "")]
    // XNA keeps a repeated separator. The splitting approximation dropped the empty segment and
    // answered "a\b", which is a different asset name and therefore a different cache key.
    [InlineData("a\\\\b", "a\\\\b")]
    // XNA keeps a leading "..", because its collapse scan starts at index 1. Its own
    // IsCleanPathAbsolute then rejects exactly this residue as "escaped the title".
    [InlineData("..\\secret", "..\\secret")]
    [InlineData("Models\\..\\..\\secret", "..\\secret")]
    // A dot that is part of a name is not a directory-relative segment.
    [InlineData("Models\\.hidden", "Models\\.hidden")]
    [InlineData("Models\\Ship.v2\\hull", "Models\\Ship.v2\\hull")]
    public void GetCleanPath_MatchesXna(string input, string expected) =>
        Assert.Equal(expected, TitleContainer.GetCleanPath(input));

    [Theory]
    // A reference is relative to the referring asset's own directory.
    [InlineData("Models\\Ship", "hull", "Models\\hull")]
    [InlineData("Models/Ship", "hull", "Models\\hull")]
    [InlineData("Ship", "hull", "hull")]
    [InlineData("Models\\Sub\\Ship", "..\\Textures\\hull", "Models\\Textures\\hull")]
    [InlineData("Models\\Ship", "..\\..\\Textures\\hull", "..\\Textures\\hull")]
    [InlineData("Models\\Ship", "Sub\\.\\hull", "Models\\Sub\\hull")]
    // Windows treats a leading separator as rooted, so XNA drops the referring directory. On this
    // host Path.Combine does not, which is the bug this type exists to prevent.
    [InlineData("Models\\Ship", "\\Absolute\\hull", "\\Absolute\\hull")]
    [InlineData("Models\\Ship", "C:\\Absolute\\hull", "C:\\Absolute\\hull")]
    public void Resolve_MatchesXna(string assetName, string reference, string expected) =>
        Assert.Equal(expected, XnaContentPath.Resolve(assetName, reference));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_EmptyReference_IsNoReferenceRatherThanAnAssetNamedAfterTheDirectory(string? reference) =>
        Assert.Null(XnaContentPath.Resolve("Models\\Sub\\Ship", reference));

    /// <summary>The host must not be able to change the answer. Both spellings of the same
    /// reference have to produce the one asset name, or the same content compiles to two different
    /// cache entries depending on which machine wrote the file.</summary>
    [Fact]
    public void Resolve_IsIndependentOfTheHostSeparator()
    {
        string fromWindowsSpelling = XnaContentPath.Resolve("Models\\Sub\\Ship", "..\\Textures\\hull")!;
        string fromPosixSpelling = XnaContentPath.Resolve("Models/Sub/Ship", "../Textures/hull")!;

        Assert.Equal("Models\\Textures\\hull", fromWindowsSpelling);
        Assert.Equal(fromWindowsSpelling, fromPosixSpelling);
        Assert.DoesNotContain('/', fromWindowsSpelling);
    }

    /// <summary>The working directory must never appear. <c>Path.GetFullPath</c> would splice it
    /// in, and the resulting string would still look like a plausible asset name.</summary>
    [Fact]
    public void Resolve_DoesNotIntroduceTheProcessWorkingDirectory()
    {
        string resolved = XnaContentPath.Resolve("Models\\Ship", "hull")!;

        Assert.Equal("Models\\hull", resolved);
        Assert.False(Path.IsPathRooted(resolved));
        Assert.DoesNotContain(Directory.GetCurrentDirectory(), resolved, StringComparison.Ordinal);
    }
}
