using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xunit;
using Xunit.Abstractions;
using XnaGame = Microsoft.Xna.Framework.Game;

// NOT under CNA. That is load-bearing, and it is the second thing this file established.
//
// A namespace nested under CNA puts CNA's own root types in scope, and an enclosing namespace's
// members shadow a using directive's imports in C#. So inside `namespace CNA.Integration.Tests`,
// `GameTime` resolves to CNA.GameTime even with `using Microsoft.Xna.Framework;` at the top, and
// overriding the compat Game's Update fails with "cannot override inherited member because it is
// sealed" -- an error naming neither the namespace nor the shadowing.
//
// cna-cs-template hit the same thing and works around it in its csproj (`<RootNamespace>
// CnaCsTemplate</RootNamespace>`, with a comment). It is a real constraint on consumers: a ported
// XNA game cannot live under a CNA namespace. Recorded in docs rather than only in a csproj comment
// now, because the symptom points nowhere near the cause.
namespace CnaCs.Integration.Tests.Compat;

/// <summary>
/// The compat layer, against the real library. <b>Nothing here had ever run.</b>
///
/// Every integration test written before this one used <c>CNA.*</c> types. A ported XNA game uses
/// <c>Microsoft.Xna.Framework.*</c>, which is 238 re-typing members -- <c>new</c> properties that
/// convert, overrides that cast, value types that duplicate with implicit conversions -- and each
/// is a place a conversion can be wrong in a way the CNA layer running correctly says nothing
/// about.
///
/// It went unnoticed because the coverage measurement could not see it: both layers name their
/// types identically, so <c>Texture2D</c> counted as covered while only
/// <c>CNA.Graphics.Texture2D</c> had ever executed. A metric matching bare identifiers cannot
/// distinguish two namespaces, and this one did not.
///
/// In the own-game collection: a compat game is a different <c>Game</c> subclass, so it builds its
/// own rather than borrowing the shared fixture's.
/// </summary>
[Collection(global::CNA.Integration.Tests.OwnGameCollection.Name)]
public class CompatLayerIntegrationTests(ITestOutputHelper output)
{
    /// <summary>Runs one frame of a real compat game and surfaces what the body threw.</summary>
    private sealed class CompatProbe(Action<CompatProbe> body) : XnaGame
    {
        public bool Ran { get; private set; }

        public Exception? Failure { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            if (!Ran)
            {
                Ran = true;
                try
                {
                    body(this);
                }
                catch (Exception ex)
                {
                    Failure = ex;
                }
            }

            Exit();
            base.Update(gameTime);
        }
    }

    private static void InsideACompatFrame(Action<CompatProbe> body)
    {
        using var game = new CompatProbe(body);

        for (int i = 0; i < 4 && !game.Ran; i++)
        {
            game.RunOneFrame();
        }

        if (game.Failure is { } failure)
        {
            throw new Xunit.Sdk.XunitException($"The body threw inside the compat frame: {failure}");
        }

        Assert.True(game.Ran, "The frame never ran, so nothing was exercised.");
    }

    /// <summary>
    /// The covariant-return factory hooks: a compat game's Content, GraphicsDevice and Window must
    /// be the compat types, not the CNA ones they derive from.
    ///
    /// This is the load-bearing part of the whole layer. If any of those three hands back a base
    /// type, ported source stops compiling -- or worse, compiles and casts at run time.
    /// </summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGame_ExposesCompatTypedMembers()
    {
        InsideACompatFrame(game =>
        {
            Assert.IsAssignableFrom<Microsoft.Xna.Framework.Content.ContentManager>(game.Content);
            Assert.IsAssignableFrom<Microsoft.Xna.Framework.Graphics.GraphicsDevice>(game.GraphicsDevice);
            Assert.IsAssignableFrom<Microsoft.Xna.Framework.GameWindow>(game.Window);

            output.WriteLine(
                $"content={game.Content.GetType().FullName}, device={game.GraphicsDevice.GetType().FullName}");
        });
    }

    /// <summary>A texture created and uploaded through the compat types, with the compat
    /// <see cref="Color"/> -- a duplicated value type, not the CNA one.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatTexture2D_SetDataWithCompatColor()
    {
        InsideACompatFrame(game =>
        {
            using var texture = new Texture2D(game.GraphicsDevice, 2, 2);

            texture.SetData(
            [
                Color.Red, Color.Green,
                Color.Blue, Color.White,
            ]);

            Assert.Equal(2, texture.Width);
            Assert.Equal(new Rectangle(0, 0, 2, 2), texture.Bounds);

            var read = new Color[4];
            texture.GetData(read);
            output.WriteLine($"read back {string.Join(", ", read)}");
            Assert.Equal(Color.Red, read[0]);
        });
    }

    /// <summary>A full compat SpriteBatch pass with the compat Vector2 and Color.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatSpriteBatch_DrawsWithCompatValueTypes()
    {
        InsideACompatFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            using var texture = new Texture2D(device, 1, 1);
            texture.SetData([Color.White]);

            using var batch = new SpriteBatch(device);

            device.Clear(Color.CornflowerBlue);
            batch.Begin();
            batch.Draw(texture, new Vector2(3f, 4f), Color.White);
            batch.End();
        });
    }

    /// <summary>The compat viewport and its re-typed Rectangle, read from a live device.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_ViewportIsCompatTyped()
    {
        InsideACompatFrame(game =>
        {
            Viewport viewport = game.GraphicsDevice.Viewport;
            Rectangle bounds = viewport.Bounds;

            output.WriteLine($"viewport {viewport.Width}x{viewport.Height}, bounds {bounds}");

            Assert.Equal(viewport.Width, bounds.Width);
            Assert.True(viewport.Width > 0);
        });
    }

    /// <summary>The compat state objects, which are separate types per namespace and convert on
    /// the way down.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_StateObjectsRoundTrip()
    {
        InsideACompatFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            BlendState blend = device.BlendState;
            DepthStencilState depth = device.DepthStencilState;
            RasterizerState rasterizer = device.RasterizerState;

            output.WriteLine($"blend={blend.ColorSourceBlend} depth={depth.DepthBufferEnable} cull={rasterizer.CullMode}");

            device.BlendState = BlendState.AlphaBlend;
            Assert.Equal(BlendState.AlphaBlend.ColorSourceBlend, device.BlendState.ColorSourceBlend);
        });
    }

    /// <summary>Compat input, whose Keys and Buttons enums are duplicated per namespace and must
    /// stay numerically identical to the CNA ones for any of this to work.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatInput_ReportsState()
    {
        InsideACompatFrame(_ =>
        {
            KeyboardState keyboard = Keyboard.GetState();
            Assert.False(keyboard.IsKeyDown(Keys.Escape));

            MouseState mouse = Mouse.GetState();
            GamePadState pad = GamePad.GetState(PlayerIndex.One);

            output.WriteLine($"mouse ({mouse.X},{mouse.Y}) pad connected={pad.IsConnected}");
        });
    }

    /// <summary>A compat effect: the family that composes its CNA counterpart rather than deriving
    /// from it, so every forwarding member is a place the two halves can drift apart.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatBasicEffect_AppliesAndReTypesItsMatrices()
    {
        InsideACompatFrame(game =>
        {
            using var effect = new BasicEffect(game.GraphicsDevice)
            {
                World = Matrix.Identity,
                View = Matrix.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.Up),
                DiffuseColor = new Vector3(0.5f, 0.25f, 0.125f),
            };

            effect.Apply();

            Assert.Equal(0.5f, effect.DiffuseColor.X, 1e-4f);
            output.WriteLine($"diffuse={effect.DiffuseColor} world={effect.World.M11}");

            using Effect clone = effect.Clone();
            Assert.IsAssignableFrom<BasicEffect>(clone);
        });
    }

    /// <summary>Compat render targets, which derive from the compat Texture2D rather than from
    /// CNA's RenderTarget2D -- the divergence that needed GetRenderTargetProperties.</summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatRenderTarget2D_ReportsItsProperties()
    {
        InsideACompatFrame(game =>
        {
            using var target = new RenderTarget2D(game.GraphicsDevice, 32, 16);

            output.WriteLine(
                $"{target.Width}x{target.Height} depth={target.DepthStencilFormat} " +
                $"usage={target.RenderTargetUsage} lost={target.IsContentLost}");

            Assert.Equal(32, target.Width);

            // A compat render target must be usable where a compat Texture2D is expected -- that is
            // the whole reason it derives from this namespace's Texture2D.
            Texture2D asTexture = target;
            Assert.Equal(32, asTexture.Width);
        });
    }
}
