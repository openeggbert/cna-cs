namespace Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

/// <summary>
/// The song and video readers.
///
/// <b>Why these matter out of proportion to their count.</b> Almost every XNA game with background
/// music calls <c>Content.Load&lt;Song&gt;</c>, and <c>SongReader</c> is the *root* reader of that
/// asset -- so without it the call failed outright, not in some nested corner. The survey of the
/// XNA 4.0 sample collection finds exactly one such asset because samples rarely ship music; a
/// shipped game is the opposite case.
///
/// <b>What a compiled song actually is.</b> Not audio. The <c>.xnb</c> holds a relative path to a
/// media file sitting beside it, plus the duration the pipeline measured. That is why the reader
/// resolves the path against the asset's own directory rather than the working directory, and why
/// the duration is passed through instead of being rediscovered by the decoder: a game that reads
/// <c>Song.Duration</c> before playback gets the value it was authored against.
/// </summary>
internal sealed class SongContentReader : ContentTypeReader<Song>
{
    /// <summary>
    /// Whether this asset writes its scalar fields as dispatched objects or as raw values.
    ///
    /// <b>Two wire forms exist and both are real.</b> XNA's own <c>SongReader</c> reads the media
    /// path with <c>ReadString</c> and the duration with <c>ReadObject&lt;int&gt;</c>, so an
    /// XNA-pipeline asset carries <c>Int32Reader</c> in its table alongside <c>SongReader</c>. CNA's
    /// runtime reader instead reads both fields raw, and content authored for it carries a
    /// one-entry table. Four assets in the XNA 4.0 sample collection are the second kind, and this
    /// reader -- correct for XNA -- read the duration's first byte as a type-reader index and
    /// failed them.
    ///
    /// <b>The rule is upstream's, not invented here.</b> CNA's <c>VideoReader</c> makes exactly this
    /// choice with <c>ReaderCount(input) &gt; 1</c>: a table holding only the asset's own reader
    /// cannot describe a dispatched field, because there is no reader to dispatch to. Applying it to
    /// <c>Song</c> keeps every XNA-written asset on XNA's path and adds the one CNA writes.
    ///
    /// Note that CNA's own <c>SongReader</c> reads only the raw form, so it cannot read an
    /// XNA-written Song; this reader now reads both.
    /// </summary>
    internal static bool UsesReaderReferences(ContentReader input) => input.TypeReaderCount > 1;

    /// <summary>One <c>int</c> field in whichever form this asset uses.</summary>
    internal static int ReadFieldInt32(ContentReader input) =>
        UsesReaderReferences(input) ? input.ReadObject<int>() : input.ReadInt32();

    protected internal override Song Read(ContentReader input, Song existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        string reference = input.ReadString();
        string path = ContentReferencePaths.Resolve(input, reference);
        int durationMilliseconds = ReadFieldInt32(input);

        return new Song(new CNA.Media.Song(path, input.AssetName, durationMilliseconds));
    }
}

/// <summary>See <see cref="SongContentReader"/>. A video records more of what the pipeline
/// measured: dimensions, frame rate and which soundtrack the container carries.</summary>
internal sealed class VideoContentReader : ContentTypeReader<Video>
{
    protected internal override Video Read(ContentReader input, Video existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        bool dispatched = SongContentReader.UsesReaderReferences(input);
        string reference = dispatched ? input.ReadObject<string>() : input.ReadString();
        string path = ContentReferencePaths.Resolve(input, reference);
        int durationMilliseconds = SongContentReader.ReadFieldInt32(input);
        int width = SongContentReader.ReadFieldInt32(input);
        int height = SongContentReader.ReadFieldInt32(input);
        float framesPerSecond = dispatched ? input.ReadObject<float>() : input.ReadSingle();
        var soundtrackType = (VideoSoundtrackType)SongContentReader.ReadFieldInt32(input);

        return new Video(
            GraphicsContentHelper.GraphicsDeviceFromContentReader(input),
            path,
            durationMilliseconds,
            width,
            height,
            framesPerSecond,
            soundtrackType);
    }
}

/// <summary>
/// Resolves a media file path recorded inside an asset.
///
/// XNA combines the reference with the content manager's root directory and the title location,
/// then cleans the result. The pieces here are the same, in the same order, and the cleaning is
/// deliberately not <see cref="Path.GetFullPath(string)"/>: that would splice the process working
/// directory into a path the game expects to be relative to its own content.
/// </summary>
internal static class ContentReferencePaths
{
    /// <summary>
    /// The file a media asset's embedded reference names, as a path this host can open.
    ///
    /// XNA's <c>GetAbsolutePathToReference</c> resolves the reference against the referring asset,
    /// combines it with the content root and the title location, and cleans the result. The first
    /// two steps are asset-name semantics and live in <see cref="CNA.Content.XnaContentPath"/>; the
    /// last is where a name becomes a path, and is the same boundary the XNB loader crosses.
    ///
    /// <b>This used to split on separators and rejoin.</b> That dropped the empty leading segment a
    /// POSIX absolute root produces, so a content root of <c>/rv/tmp/x</c> resolved to
    /// <c>rv/tmp/x</c> and the media file was reported missing -- with the cause hidden behind
    /// XNA's normalised "The XNB file is invalid". It was the third copy of that same
    /// split-and-rejoin in this repository; the other two are gone, and so is this one.
    /// </summary>
    internal static string Resolve(ContentReader input, string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' references a media file by an empty path.");
        }

        string assetName = CNA.Content.XnaContentPath.Resolve(input.AssetName, reference)
            ?? throw new ContentLoadException(
                $"Content asset '{input.AssetName}' references a media file by an empty path.");

        return CNA.Content.XnaContentPath.ToFilePath(
            input.ContentManager.RootDirectory, assetName, extension: string.Empty);
    }
}
