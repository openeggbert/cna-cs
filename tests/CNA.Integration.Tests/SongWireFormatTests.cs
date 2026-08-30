using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// A <c>Song</c> asset exists in two wire forms and this binding reads both.
///
/// <b>Why two.</b> XNA's own <c>SongReader</c> reads the media path with <c>ReadString</c> and the
/// duration with <c>ReadObject&lt;int&gt;</c>, so an XNA-pipeline asset carries <c>Int32Reader</c>
/// in its table beside <c>SongReader</c>. CNA's own runtime reader reads both fields raw, and
/// content authored for it has a one-entry table. Four assets in the XNA 4.0 sample collection are
/// the second kind; this reader was XNA-exact and read their duration's first byte as a type-reader
/// index.
///
/// <b>The rule that tells them apart is upstream's.</b> CNA's <c>VideoReader</c> selects with
/// <c>ReaderCount &gt; 1</c>: a table holding only the asset's own reader cannot describe a
/// dispatched field, because there is nothing to dispatch to.
///
/// The two durations differ, so reading the wrong form does not merely fail -- it produces a
/// different answer, and a test using one duration for both would pass with the branch inverted.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class SongWireFormatTests(ITestOutputHelper output, NativeGameFixture fixture) : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("cna-song-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private const string Xna = ", Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553";
    private const string Corlib = ", mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

    [NativeFact]
    public void BothWireForms_ReadTheSameFieldsAndDifferentDurations()
    {
        // The media file the reference names. Its bytes are never decoded -- a Song records the
        // path and the pipeline's own duration -- but it must exist, because that is what makes a
        // dangling reference a load failure rather than a Song nobody can play.
        File.WriteAllBytes(Path.Combine(_root, "track.wma"), [0, 1, 2, 3]);

        // CNA's form: one reader in the table, both fields raw.
        WriteAsset(
            "raw",
            ["Microsoft.Xna.Framework.Content.SongReader" + Xna],
            writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("track.wma");
                writer.Write(119338);
            });

        // XNA's form: the duration is a dispatched object, so Int32Reader is in the table.
        WriteAsset(
            "dispatched",
            ["Microsoft.Xna.Framework.Content.SongReader" + Xna,
             "Microsoft.Xna.Framework.Content.Int32Reader" + Corlib],
            writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("track.wma");
                writer.Write7BitEncodedInt(2);
                writer.Write(42000);
            });

        fixture.InsideAFrame(game =>
        {
            using var content = new ContentManager(game.Services, _root);

            Song raw = content.Load<Song>("raw");
            Song dispatched = content.Load<Song>("dispatched");

            output.WriteLine($"raw={raw.Duration} dispatched={dispatched.Duration}");

            Assert.Equal(TimeSpan.FromMilliseconds(119338), raw.Duration);
            Assert.Equal(TimeSpan.FromMilliseconds(42000), dispatched.Duration);
        });
    }

    /// <summary>A reference that names no file is a load failure, not a Song with a path nobody can
    /// open. Worth pinning because the resolution this exercises is the one that used to drop a
    /// POSIX-absolute root's leading separator, and a missing file was the symptom.</summary>
    [NativeFact]
    public void MissingMediaFile_FailsTheLoad()
    {
        WriteAsset(
            "absent",
            ["Microsoft.Xna.Framework.Content.SongReader" + Xna],
            writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("nothing-here.wma");
                writer.Write(1000);
            });

        fixture.InsideAFrame(game =>
        {
            using var content = new ContentManager(game.Services, _root);

            ContentLoadException failure = Assert.Throws<ContentLoadException>(() => content.Load<Song>("absent"));
            output.WriteLine(failure.ToString());

            // The absolute root must survive into the path the loader reports, which is the whole
            // point: a resolution that dropped the leading separator reported a relative path and
            // was indistinguishable from a genuinely missing asset.
            Assert.Contains(_root, Flatten(failure), StringComparison.Ordinal);
        });
    }

    private static string Flatten(Exception failure)
    {
        var text = new StringBuilder();
        for (Exception? current = failure; current is not null; current = current.InnerException)
        {
            text.Append(current.Message).Append(' ');
        }

        return text.ToString();
    }

    private void WriteAsset(string assetName, string[] readerNames, Action<BinaryWriter> writeBody)
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write7BitEncodedInt(readerNames.Length);
            foreach (string name in readerNames)
            {
                writer.Write(name);
                writer.Write(0);
            }

            writer.Write7BitEncodedInt(0);
            writeBody(writer);
        }

        byte[] body = payload.ToArray();
        using var container = new MemoryStream();
        using (var writer = new BinaryWriter(container, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((byte)'X');
            writer.Write((byte)'N');
            writer.Write((byte)'B');
            writer.Write((byte)'w');
            writer.Write((byte)5);
            writer.Write((byte)0);
            writer.Write(10 + body.Length);
            writer.Write(body);
        }

        File.WriteAllBytes(Path.Combine(_root, assetName + ".xnb"), container.ToArray());
    }
}
