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
    protected internal override Song Read(ContentReader input, Song existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        string reference = input.ReadString();
        string path = ContentReferencePaths.Resolve(input, reference);
        int durationMilliseconds = input.ReadObject<int>();

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

        string reference = input.ReadObject<string>();
        string path = ContentReferencePaths.Resolve(input, reference);
        int durationMilliseconds = input.ReadObject<int>();
        int width = input.ReadObject<int>();
        int height = input.ReadObject<int>();
        float framesPerSecond = input.ReadObject<float>();
        var soundtrackType = (VideoSoundtrackType)input.ReadObject<int>();

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
    internal static string Resolve(ContentReader input, string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' references a media file by an empty path.");
        }

        int separator = input.AssetName.LastIndexOfAny(['\\', '/', Path.DirectorySeparatorChar]);
        string assetDirectory = separator < 0 ? string.Empty : input.AssetName[..separator];

        string relative = assetDirectory.Length == 0
            ? reference
            : Path.Combine(assetDirectory, reference);

        string root = input.ContentManager.RootDirectory;
        return Normalize(root.Length == 0 ? relative : Path.Combine(root, relative));
    }

    private static string Normalize(string path)
    {
        string[] segments = path.Replace('\\', '/').Split('/');
        var clean = new List<string>(segments.Length);
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            if (segment == ".." && clean.Count > 0 && clean[^1] != "..")
            {
                clean.RemoveAt(clean.Count - 1);
                continue;
            }

            clean.Add(segment);
        }

        return string.Join(Path.DirectorySeparatorChar, clean);
    }
}
