using System;
using System.IO;
using CNA.Content;
using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// Content asset names resolve case-insensitively, because XNA games were written against a
/// case-insensitive filesystem and rely on it.
///
/// The XNA sample collection does so casually. `cna-cs-samples` CSSAMPLE-022 Pathfinding ships
/// `Map1.xnb` through `Map4.xnb` and asks for "map1" — correct on Windows and Xbox 360, and on
/// this host it failed with "Could not open content asset 'map1'". CNA's own native content
/// manager already resolves this way; the C++ port of that sample loads the same files under the
/// same names, so this is the managed side matching the runtime it binds.
/// </summary>
public class XnaContentPathCaseTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "cna-content-case-" + Guid.NewGuid().ToString("N"));

    public XnaContentPathCaseTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
        GC.SuppressFinalize(this);
    }

    private string Write(string fileName)
    {
        string path = Path.Combine(_root, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [0]);
        return path;
    }

    [Fact]
    public void AnAssetNameThatDiffersOnlyInCaseResolvesToTheFileOnDisk()
    {
        string onDisk = Write("Map1.xnb");

        Assert.Equal(onDisk, XnaContentPath.ToFilePath(_root, "map1", ".xnb"));
        Assert.Equal(onDisk, XnaContentPath.ToFilePath(_root, "MAP1", ".xnb"));
        Assert.Equal(onDisk, XnaContentPath.ToFilePath(_root, "Map1", ".xnb"));
    }

    [Fact]
    public void AnExactMatchIsPreferredAndCostsNoScan()
    {
        // Both exist; the exactly-named one must win rather than whichever the scan met first.
        Write("Tile.xnb");
        string exact = Write("tile.xnb");

        Assert.Equal(exact, XnaContentPath.ToFilePath(_root, "tile", ".xnb"));
    }

    [Fact]
    public void ASubdirectoryInTheAssetNameIsHonoured()
    {
        string onDisk = Write(Path.Combine("Textures", "Rock.xnb"));

        // XNA spells asset names with a backslash; the resolver translates the separator.
        Assert.Equal(onDisk, XnaContentPath.ToFilePath(_root, @"Textures\rock", ".xnb"));
    }

    [Fact]
    public void AMissingAssetStillReturnsTheExactPathSoTheErrorNamesWhatWasAskedFor()
    {
        string expected = Path.Combine(_root, "absent.xnb");

        // Not an exception and not a guess: the caller checks File.Exists and reports the name the
        // game used, which is the message a developer can act on.
        Assert.Equal(expected, XnaContentPath.ToFilePath(_root, "absent", ".xnb"));
    }

    [Fact]
    public void AMissingDirectoryDoesNotThrow()
    {
        string expected = Path.Combine(_root, "NoSuchFolder", "asset.xnb");

        Assert.Equal(expected, XnaContentPath.ToFilePath(_root, @"NoSuchFolder\asset", ".xnb"));
    }
}
