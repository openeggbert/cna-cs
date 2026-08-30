// SPDX-License-Identifier: MIT
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace CnaCs.ContentSurvey;

/// <summary>
/// Loads the assets the resolution survey says are readable, and reports what actually happened.
///
/// The resolution survey answers "does this binding have a reader for everything the file names",
/// which is necessary and not sufficient: it never decompresses a payload, never materialises an
/// object, and never touches a graphics device. An LZX-compressed texture whose reader table
/// resolves perfectly can still fail to decompress, and the resolution survey would call it
/// readable.
///
/// This mode builds a real game, a real graphics device and a real ContentManager, and calls
/// <c>Load</c>. Every outcome is reported under its own name, because "did not load" covers several
/// very different situations and collapsing them would hide the only interesting one.
/// </summary>
internal sealed class LoadingSurvey : Game
{
    private readonly string _root;
    private readonly IReadOnlyList<(string Relative, string RootReader)> _assets;

    public LoadingSurvey(string root, IReadOnlyList<(string Relative, string RootReader)> assets)
    {
        _root = root;
        _assets = assets;
        _ = new GraphicsDeviceManager(this);
        Content.RootDirectory = root;
    }

    public SortedDictionary<string, string> Loaded { get; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, string> NativeNotSupported { get; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, string> RuntimeFailures { get; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, string> NoManagedType { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Assets that failed because they name a type only the game's own assembly defines.
    ///
    /// Counting these as runtime failures overstates the gap: a <c>ReflectiveReader</c> over a
    /// game's settings class is *supposed* to be unreadable here, and no reader this binding could
    /// add would change it. They are separated so the failure count means "this binding could not
    /// read content it should have been able to read".
    /// </summary>
    public SortedDictionary<string, string> ExternalGameTypes { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The type to ask for, from the asset's own root reader.
    ///
    /// <c>Load&lt;object&gt;</c> is not a substitute: the strict facade routes several built-ins to
    /// CNA's own loader by the requested type, so asking for the wrong one exercises a different
    /// path from the one a game would take.
    /// </summary>
    private static Type? ManagedType(string rootReader) => rootReader switch
    {
        "Microsoft.Xna.Framework.Content.Texture2DReader" => typeof(Texture2D),
        "Microsoft.Xna.Framework.Content.TextureCubeReader" => typeof(TextureCube),
        "Microsoft.Xna.Framework.Content.Texture3DReader" => typeof(Texture3D),
        "Microsoft.Xna.Framework.Content.SpriteFontReader" => typeof(SpriteFont),
        "Microsoft.Xna.Framework.Content.SoundEffectReader" => typeof(SoundEffect),
        "Microsoft.Xna.Framework.Content.SongReader" => typeof(Song),
        "Microsoft.Xna.Framework.Content.VideoReader" => typeof(Video),
        "Microsoft.Xna.Framework.Content.ModelReader" => typeof(Model),
        "Microsoft.Xna.Framework.Content.EffectReader" => typeof(Effect),
        "Microsoft.Xna.Framework.Content.BasicEffectReader" => typeof(Effect),
        _ => null,
    };

    protected override void Update(GameTime gameTime)
    {
        foreach ((string relative, string rootReader) in _assets)
        {
            string assetName = relative[..^".xnb".Length].Replace('\\', '/');

            if (ManagedType(rootReader) is not { } type)
            {
                NoManagedType[relative] = rootReader;
                continue;
            }

            try
            {
                object? loaded = typeof(ContentManager)
                    .GetMethod(nameof(ContentManager.Load))!
                    .MakeGenericMethod(type)
                    .Invoke(Content, [assetName]);

                Loaded[relative] = Describe(loaded);
            }
            catch (Exception exception)
            {
                Exception actual = exception is System.Reflection.TargetInvocationException { InnerException: { } inner }
                    ? inner
                    : exception;

                // A renderer or backend that cannot represent an asset is a different fact from a
                // binding that cannot read one, and only the second is a defect here.
                if (actual is CNA.CnaException { NativeResult: "NotSupported" })
                {
                    NativeNotSupported[relative] = actual.Message;
                }
                else if (NamesAGameType(actual.Message))
                {
                    ExternalGameTypes[relative] = actual.Message;
                }
                else
                {
                    RuntimeFailures[relative] = $"{actual.GetType().Name}: {actual.Message}";
                }
            }
        }

        Exit();
        base.Update(gameTime);
    }

    /// <summary>
    /// Whether a failure names a reader over a type the game supplies rather than a built-in.
    ///
    /// <c>ReflectiveReader</c> is XNA's reader for a plain class in the game's own assembly, so a
    /// file naming one can only be read inside that game. A reader name with no
    /// <c>Microsoft.Xna.Framework</c> prefix is the same situation spelled differently.
    /// </summary>
    private static bool NamesAGameType(string message) =>
        message.Contains("ReflectiveReader", StringComparison.Ordinal) ||
        (message.Contains("content type reader '", StringComparison.Ordinal) &&
         !message.Contains("reader 'Microsoft.Xna.Framework", StringComparison.Ordinal));

    /// <summary>A property or two that only a materialised object can answer, so the report shows
    /// the asset was really built and not merely returned.</summary>
    private static string Describe(object? loaded) => loaded switch
    {
        Texture2D texture => $"Texture2D {texture.Width}x{texture.Height} {texture.Format}",
        TextureCube cube => $"TextureCube size={cube.Size} {cube.Format}",
        Texture3D volume => $"Texture3D {volume.Width}x{volume.Height}x{volume.Depth}",
        SpriteFont font => $"SpriteFont lineSpacing={font.LineSpacing} glyphs={font.Characters.Count}",
        SoundEffect sound => $"SoundEffect {sound.Duration}",
        Model model => $"Model meshes={model.Meshes.Count} bones={model.Bones.Count}",
        Effect effect => $"Effect techniques={effect.Techniques.Count}",
        Song song => $"Song {song.Duration}",
        Video video => $"Video {video.Width}x{video.Height}",
        null => "null",
        _ => loaded.GetType().Name,
    };
}
