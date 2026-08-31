using CNA.Content;
using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// What <see cref="ContentManager"/> promises about loading the same asset twice.
///
/// <b>The question these tests exist to settle.</b> This class documented a cache before it had
/// one: <see cref="ContentManager.Unload"/>'s comment said it "releases every asset this manager
/// loaded" and <see cref="ContentManager.LoadForeign{T}"/>'s said results are "cached by asset name
/// exactly as every other load is" -- while no load cached anything. Measured before the fix, two
/// <c>Load&lt;SpriteFont&gt;</c> calls for one name produced two fonts over two GPU textures and
/// <c>Unload</c> disposed neither, so a game calling <c>Load</c> in a frame accumulated an atlas per
/// call that nothing would free.
///
/// So the divergence from XNA and from <c>CNA.XnaCompat</c> was a missing implementation, not a
/// CNA-native design decision -- the two false doc comments are what settle it -- and the semantics
/// below are now the same on both layers. A game porting between them does not find its content
/// identity changing underneath it.
///
/// Every test here uses its own manager. The suite shares one game, and a test that mutated the
/// shared <c>RootDirectory</c> would change what every later test loads.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class ContentManagerCacheTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private const string Font = "FontCalibri14";

    private static string LzxRoot => Path.Combine(AppContext.BaseDirectory, "assets", "xnb", "lzx");

    /// <summary>
    /// The core claim: one name, one object.
    ///
    /// Asserted on the atlas as well as the font, because that is where it costs something. Two
    /// fonts sharing one texture would be cheap and wrong; two fonts with two textures is the defect
    /// this fixes, and only the second assertion sees the difference.
    /// </summary>
    [NativeFact]
    public void Load_Twice_ReturnsTheSameInstanceAndTheSameAtlas()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using ContentManager content = ContentManager.CreateOwned(device, LzxRoot);

            SpriteFont first = content.Load<SpriteFont>(Font);
            SpriteFont second = content.Load<SpriteFont>(Font);

            Assert.Same(first, second);
            Assert.Same(first.Texture, second.Texture);
            output.WriteLine("one name, one font, one atlas");
        });
    }

    /// <summary>
    /// The cache key is case-insensitive over backslash-normalised names -- XNA's rule, and the one
    /// <c>CNA.XnaCompat</c> already used.
    ///
    /// Both halves matter and neither implies the other: a key that normalised separators but
    /// compared ordinally would fail the case row, and a case-insensitive key over the raw string
    /// would fail the separator row.
    /// </summary>
    [NativeFact]
    public void Load_TreatsCaseAndSeparatorsAsOneAsset()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            string root = Path.Combine(AppContext.BaseDirectory, "assets", "xnb");
            using ContentManager content = ContentManager.CreateOwned(device, root);

            SpriteFont canonical = content.Load<SpriteFont>("lzx/" + Font);
            SpriteFont backslashed = content.Load<SpriteFont>("lzx\\" + Font);
            SpriteFont lowercased = content.Load<SpriteFont>("lzx/" + Font.ToLowerInvariant());

            Assert.Same(canonical, backslashed);
            Assert.Same(canonical, lowercased);
        });
    }

    /// <summary>
    /// What is <em>not</em> normalised, stated as deliberately as what is.
    ///
    /// <c>./name</c> and <c>name</c> stay separate assets. XNA does not collapse them either, and a
    /// manager that decided they were one would be inventing an identity the content format does not
    /// have -- which is a worse failure than the duplicate, because it is silent and unfixable from
    /// the caller's side.
    /// </summary>
    [NativeFact]
    public void Load_DoesNotCollapseRelativePathSegments()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using ContentManager content = ContentManager.CreateOwned(device, LzxRoot);

            SpriteFont plain = content.Load<SpriteFont>(Font);
            SpriteFont dotted = content.Load<SpriteFont>("./" + Font);

            Assert.NotSame(plain, dotted);
            output.WriteLine("'./name' and 'name' are two assets, as they are in XNA");
        });
    }

    /// <summary>
    /// Asking for a loaded name as the wrong type is a content error naming the asset, not an
    /// <see cref="InvalidCastException"/> somewhere further out.
    /// </summary>
    [NativeFact]
    public void Load_AsADifferentType_SaysWhichAssetAndWhatItAlreadyIs()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using ContentManager content = ContentManager.CreateOwned(device, LzxRoot);

            _ = content.Load<SpriteFont>(Font);

            ContentLoadException failure =
                Assert.Throws<ContentLoadException>(() => content.Load<Texture2D>(Font));

            Assert.Contains(Font, failure.Message, StringComparison.Ordinal);
            output.WriteLine(failure.Message);
        });
    }

    /// <summary>
    /// <see cref="ContentManager.Unload"/> disposes what the manager loaded -- <b>including a
    /// resource the asset owns but is not</b>.
    ///
    /// <c>SpriteFont</c> is deliberately the asset here. It is not <see cref="IDisposable"/>, in
    /// this binding as in XNA, while the atlas behind it is; a manager that recorded only the object
    /// it returned would leave that atlas alive and pass every other test in this file. It did:
    /// measured before the fix, the atlas survived <c>Unload</c>.
    /// </summary>
    [NativeFact]
    public void Unload_DisposesTheAtlasAFontOwnsButIsNot()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using ContentManager content = ContentManager.CreateOwned(device, LzxRoot);

            SpriteFont font = content.Load<SpriteFont>(Font);
            Texture atlas = font.Texture;
            Assert.False(atlas.IsDisposed, "The atlas must be alive before Unload, or this proves nothing.");

            content.Unload();

            Assert.True(atlas.IsDisposed);
        });
    }

    /// <summary>After <c>Unload</c> the manager is usable and the cache is empty, so the next load
    /// produces a genuinely new asset rather than a disposed one.</summary>
    [NativeFact]
    public void Unload_ThenLoad_ProducesAFreshAssetRatherThanADisposedOne()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using ContentManager content = ContentManager.CreateOwned(device, LzxRoot);

            SpriteFont before = content.Load<SpriteFont>(Font);
            content.Unload();
            SpriteFont after = content.Load<SpriteFont>(Font);

            Assert.NotSame(before, after);
            Assert.False(after.Texture.IsDisposed);
        });
    }

    /// <summary>
    /// A failed load caches nothing, so a name that becomes loadable later loads.
    ///
    /// Proven by making it happen rather than by asserting that two failures look alike: the asset
    /// is copied into the root only after the first attempt has failed. A manager that cached the
    /// failure -- or cached a null -- would still refuse the second call, and no weaker test
    /// separates the two.
    /// </summary>
    [NativeFact]
    public void FailedLoad_IsNotCached_SoTheNameLoadsOnceTheAssetExists()
    {
        string root = Path.Combine(Path.GetTempPath(), $"cna-content-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            fixture.InsideAFrameWithDevice(device =>
            {
                using ContentManager content = ContentManager.CreateOwned(device, root);

                Assert.Throws<ContentLoadException>(() => content.Load<SpriteFont>(Font));

                File.Copy(Path.Combine(LzxRoot, Font + ".xnb"), Path.Combine(root, Font + ".xnb"));

                SpriteFont loaded = content.Load<SpriteFont>(Font);
                Assert.False(loaded.Texture.IsDisposed);
                output.WriteLine("the name failed, the asset appeared, and the same manager loaded it");
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>A disposed manager refuses to load, rather than answering from a cache it has
    /// already emptied or reaching a native handle it has already released.</summary>
    [NativeFact]
    public void Dispose_ThenLoad_Throws()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            ContentManager content = ContentManager.CreateOwned(device, LzxRoot);
            _ = content.Load<SpriteFont>(Font);
            content.Dispose();

            Assert.Throws<ObjectDisposedException>(() => content.Load<SpriteFont>(Font));
        });
    }

    /// <summary>Disposing the manager disposes what it loaded, by the same route as
    /// <see cref="ContentManager.Unload"/>.</summary>
    [NativeFact]
    public void Dispose_DisposesWhatItLoaded()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            ContentManager content = ContentManager.CreateOwned(device, LzxRoot);
            Texture atlas = content.Load<SpriteFont>(Font).Texture;

            content.Dispose();

            Assert.True(atlas.IsDisposed);
        });
    }
}
