using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// What is left of this file after the media-library rebinding, and why so little is left.
///
/// It used to hold 17 tests over <see cref="MediaLibrary"/> plus 30 more across
/// <c>MediaLibraryTypesTests</c> and <c>PictureLibraryTests</c>, all of which passed with no native
/// library present. That was possible because the whole media family was a managed object model
/// that never called native at all -- its own doc comment said every music collection was
/// permanently empty by design. The C API turned out to ship the entire scan
/// (<c>media_library.h</c>, 148 functions), so the model was replaced by real bindings, and those
/// tests were testing a fabrication rather than the library. Deleting them is the point, not
/// collateral damage: a test that passes only because the code under it does nothing is worse than
/// no test.
///
/// What survives is what still runs without a native library: the argument validation that happens
/// before the first native call. Everything else in this area now needs <c>cna-native</c> loaded,
/// which this suite does not have.
/// </summary>
public class MediaLibraryTests
{
    [Fact]
    public void Constructor_NullMediaSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MediaLibrary(null!));
    }

    /// <summary>Real XNA only supports a local device, and this check is kept managed-side so the
    /// failure is a <see cref="NotSupportedException"/> rather than a native result code -- see
    /// <see cref="MediaLibrary"/>'s own doc comment. It runs before the library is opened, which is
    /// what makes it testable here at all.</summary>
    [Fact]
    public void Constructor_NonLocalDeviceMediaSource_ThrowsNotSupportedException()
    {
        var source = new MediaSource(MediaSourceType.WindowsMediaConnect, "Remote");

        Assert.Throws<NotSupportedException>(() => new MediaLibrary(source));
    }
}
