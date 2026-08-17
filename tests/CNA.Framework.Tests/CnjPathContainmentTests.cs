using CNA.Content.Cnj;
using Xunit;

namespace CNA.Tests;

public class CnjPathContainmentTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "assets", "cnj");

    [Fact]
    public void TryResolve_SimpleRelativePath_ResolvesUnderRoot()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "quad_verts.bin", out string resolved);

        Assert.True(ok);
        Assert.Equal(Path.Combine(Root, "quad_verts.bin"), resolved);
    }

    [Fact]
    public void TryResolve_NestedRelativePath_ResolvesUnderRoot()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "sub/dir/file.bin", out string resolved);

        Assert.True(ok);
        Assert.Equal(Path.Combine(Root, "sub", "dir", "file.bin"), resolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryResolve_EmptyOrNullInput_ReturnsFalse(string? input)
    {
        bool ok = CnjPathContainment.TryResolve(Root, input!, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_PosixAbsolutePath_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "/etc/passwd", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_WindowsDriveAbsolutePath_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "C:/Windows/system.ini", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_UncPath_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "//server/share/file.bin", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_ParentTraversal_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "../outside.bin", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_DeepParentTraversal_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "sub/../../outside.bin", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_BackslashSeparatedTraversal_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, "..\\..\\outside.bin", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_RootItself_ReturnsFalse()
    {
        bool ok = CnjPathContainment.TryResolve(Root, ".", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryResolve_SiblingDirectoryWithSharedPrefix_ReturnsFalse()
    {
        // Regression case this spec calls out explicitly: component-wise containment must reject a
        // sibling directory like "cnj-evil" even though it string-prefix-matches "cnj".
        bool ok = CnjPathContainment.TryResolve(Root, "../cnj-evil/file.bin", out _);

        Assert.False(ok);
    }
}
