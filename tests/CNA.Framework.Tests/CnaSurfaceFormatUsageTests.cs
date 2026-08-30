using CNA.Graphics;
using CNA.Interop;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// The public <see cref="CnaSurfaceFormatUsage"/> and the interop <c>CnaRendererFormatUsage</c> are
/// two spellings of one set of bits, and only the interop one is checked against the header.
///
/// That asymmetry is deliberate -- the gate derives its macro names from the interop enum's own name
/// -- but it leaves the public enum free to drift, and a public flags value that disagrees with the
/// bit CNA actually sets is silently wrong at every call site. So the two are compared here, by name
/// and by value, in both directions.
/// </summary>
public sealed class CnaSurfaceFormatUsageTests
{
    [Fact]
    public void PublicUsageFlags_MatchTheInteropEnumTheyProject()
    {
        Dictionary<string, ulong> publicFlags = Enum.GetNames<CnaSurfaceFormatUsage>()
            .ToDictionary(name => name, name => (ulong)Enum.Parse<CnaSurfaceFormatUsage>(name));

        Dictionary<string, ulong> interopFlags = Enum.GetNames<CnaRendererFormatUsage>()
            .ToDictionary(name => name, name => (ulong)Enum.Parse<CnaRendererFormatUsage>(name));

        // Both directions: a bit added to one and not the other is a difference whichever side
        // gained it, and a one-directional subset check would miss half of those.
        Assert.Equal(interopFlags.OrderBy(pair => pair.Key), publicFlags.OrderBy(pair => pair.Key));
    }

    /// <summary>
    /// The two masks answer different questions, and neither is the negation of the other.
    ///
    /// An unclassified usage is neither supported nor refused -- <c>graphics.h</c> forbids inferring
    /// support or rejection from an unclassified bit, and this is the property that encodes it.
    /// </summary>
    [Theory]
    [InlineData(CnaSurfaceFormatUsage.None, CnaSurfaceFormatUsage.None, false, false)]
    [InlineData(CnaSurfaceFormatUsage.Sampled, CnaSurfaceFormatUsage.Sampled, true, false)]
    [InlineData(CnaSurfaceFormatUsage.Sampled, CnaSurfaceFormatUsage.None, false, true)]
    public void UnclassifiedIsNeitherSupportedNorRefused(
        CnaSurfaceFormatUsage known, CnaSurfaceFormatUsage supported, bool expectSupported, bool expectRefused)
    {
        var support = new CnaSurfaceFormatSupport(known, supported);

        Assert.Equal(expectSupported, support.IsSupported(CnaSurfaceFormatUsage.Sampled));
        Assert.Equal(expectRefused, support.IsRefused(CnaSurfaceFormatUsage.Sampled));
    }
}
