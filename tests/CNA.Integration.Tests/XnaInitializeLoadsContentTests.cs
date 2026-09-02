using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// XNA's <c>Game.Initialize</c> loads content, so a game's own <c>Initialize</c> can use what
/// <c>LoadContent</c> created as soon as it has called <c>base.Initialize()</c>.
///
/// This is not a corner of the API. It is how the XNA sample collection is written, and until
/// 2026-09-02 this binding did not do it: <c>Initialize</c> was empty and content arrived only
/// through the separate native <c>load_content</c> callback, which is delivered afterwards. Every
/// game following the documented pattern read a null and died inside the initialize callback with
/// nothing but "Object reference not set to an instance of an object" to go on.
///
/// `cna-cs-samples` CSSAMPLE-019 -- the unmodified original RectangleCollision -- is the case in
/// point: its <c>Initialize</c> calls <c>base.Initialize()</c> and then reads
/// <c>personTexture.Width</c>.
///
/// FNA is the authority (`src/Game.cs:623`): its <c>Initialize</c> initializes the components and
/// then calls <c>LoadContent()</c> when a graphics device exists.
/// </summary>
[Collection(global::CNA.Integration.Tests.OwnGameCollection.Name)]
public class XnaInitializeLoadsContentTests(ITestOutputHelper output)
{
    /// <summary>A game shaped exactly like the sample that found this: content created in
    /// <c>LoadContent</c>, then used by <c>Initialize</c> after <c>base.Initialize()</c>.</summary>
    // Fully qualified: this file's namespace is nested inside CNA, so a bare `Game` binds
    // to CNA.Game through the enclosing namespace before any using directive is considered.
    private sealed class SampleShapedGame : Microsoft.Xna.Framework.Game
    {
        private readonly Microsoft.Xna.Framework.GraphicsDeviceManager _graphics;

        public SampleShapedGame()
        {
            _graphics = new Microsoft.Xna.Framework.GraphicsDeviceManager(this);
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D? Texture { get; private set; }

        public int LoadContentCalls { get; private set; }

        public bool TextureWasVisibleToInitialize { get; private set; }

        public List<string> Order { get; } = [];

        protected override void LoadContent()
        {
            Order.Add("LoadContent");
            LoadContentCalls++;
            Texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, 2, 2);
        }

        protected override void Initialize()
        {
            Order.Add("Initialize.before-base");
            base.Initialize();
            Order.Add("Initialize.after-base");

            // The whole point: this is the line the samples write.
            TextureWasVisibleToInitialize = Texture is not null && Texture.Width == 2;
        }

        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Exit();
            base.Update(gameTime);
        }
    }

    /// <summary>A game that does NOT call base.Initialize(), which is legal. Content must still
    /// load, through the native callback, exactly as it did before the fix.</summary>
    private sealed class NoBaseCallGame : Microsoft.Xna.Framework.Game
    {
        private readonly Microsoft.Xna.Framework.GraphicsDeviceManager _graphics;

        public NoBaseCallGame()
        {
            _graphics = new Microsoft.Xna.Framework.GraphicsDeviceManager(this);
        }

        public int LoadContentCalls { get; private set; }

        protected override void LoadContent() => LoadContentCalls++;

        protected override void Initialize()
        {
            // deliberately no base.Initialize()
        }

        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Exit();
            base.Update(gameTime);
        }
    }

    [NativeFact]
    public void BaseInitialize_LoadsContent_AndLoadsItExactlyOnce()
    {
        using var game = new SampleShapedGame();
        game.Run();

        output.WriteLine(string.Join(" -> ", game.Order));

        Assert.True(
            game.TextureWasVisibleToInitialize,
            "Initialize could not see the texture LoadContent creates. XNA's Game.Initialize ends " +
            "by calling LoadContent, and the samples are written on that promise.");

        Assert.Equal(1, game.LoadContentCalls);

        // Order, not just the count: content must be loaded by the base call, not before it and
        // not after Initialize has finished.
        Assert.Equal(
            ["Initialize.before-base", "LoadContent", "Initialize.after-base"],
            game.Order);
    }

    [NativeFact]
    public void AGameThatSkipsBaseInitialize_StillLoadsContentOnce()
    {
        using var game = new NoBaseCallGame();
        game.Run();

        // The native load_content callback is the second path into the same guard. Skipping
        // base.Initialize() is legal XNA, and it must not cost the game its content -- nor load it
        // twice for the game that does call base.
        Assert.Equal(1, game.LoadContentCalls);
    }
}
