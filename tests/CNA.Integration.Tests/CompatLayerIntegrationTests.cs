using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CNA.XnaCompat.Extensions;
using Xunit;
using Xunit.Abstractions;
using XnaAudio = Microsoft.Xna.Framework.Audio;
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
    private struct ManagedReferenceVertex
    {
        public ManagedReferenceVertex(Vector3 position, string label)
        {
            Position = position;
            Label = label;
        }

        public Vector3 Position;
        public string? Label;
    }

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

    /// <summary>
    /// Exercises the complete Game/device/window facade construction order. In particular the
    /// manager is created while the outer XNA game is still in its constructor, whereas its CNA
    /// backend is private; this is the path that used to depend on public inheritance.
    /// </summary>
    private sealed class FacadeGroupProbe : XnaGame
    {
        public FacadeGroupProbe()
        {
            Manager = new GraphicsDeviceManager(this);
        }

        public GraphicsDeviceManager Manager { get; }

        public bool Updated { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            Updated = true;
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
            // See NativeGameFixture.InsideAFrame: a skip decided inside the body is a skip.
            if (failure is Xunit.Sdk.SkipException)
            {
                throw failure;
            }

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

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGameFacade_DeviceManagerAndWindowRemainFacadeTypedAcrossAFrame()
    {
        using var game = new FacadeGroupProbe();

        game.RunOneFrame();

        Assert.True(game.Updated);
        Assert.Same(
            game.Manager,
            game.Services.GetService(typeof(IGraphicsDeviceService)));
        Assert.Same(
            game.Manager,
            game.Services.GetService(typeof(IGraphicsDeviceManager)));
        Assert.IsType<Microsoft.Xna.Framework.Graphics.GraphicsDevice>(game.Manager.GraphicsDevice);
        Assert.IsAssignableFrom<Microsoft.Xna.Framework.GameWindow>(game.Window);
    }

    /// <summary>
    /// How the back buffer is fitted into the window -- a choice XNA does not offer.
    ///
    /// XNA 4.0 stretches the back buffer to the client area and gives a game no say, which is why
    /// a fixed-aspect XNA game draws its own letterbox bars. CNA does it in the presentation step,
    /// so this is one of the places where a ported game can delete code rather than port it.
    ///
    /// Every identity is round-tripped rather than just one: the enum is a straight numeric cast
    /// across the ABI, and the failure mode of a wrong cast is an off-by-one that a single-value
    /// test passes and every other value fails.
    /// </summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void GraphicsDeviceManager_PreferredPresentationMode_RoundTripsEveryIdentity()
    {
        using var game = new FacadeGroupProbe();

        game.RunOneFrame();

        CnaPresentationMode initial = game.Manager.GetCnaPreferredPresentationMode();
        output.WriteLine($"initial mode: {initial}");

        foreach (CnaPresentationMode mode in Enum.GetValues<CnaPresentationMode>())
        {
            game.Manager.SetCnaPreferredPresentationMode(mode);
            Assert.Equal(mode, game.Manager.GetCnaPreferredPresentationMode());
        }

        game.Manager.SetCnaPreferredPresentationMode(initial);
        Assert.Equal(initial, game.Manager.GetCnaPreferredPresentationMode());
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

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatContent_LoadModelAcceptsXnbRawByteBufferPayloads()
    {
        InsideACompatFrame(game =>
        {
            game.Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "xnb");
            Model model = game.Content.Load<Model>("BlenderDefaultCube");
            ModelMeshPart part = model.Meshes[0].MeshParts[0];

            Assert.NotNull(part.VertexBuffer);
            Assert.NotNull(part.IndexBuffer);
            Assert.NotNull(part.Effect);
            Assert.True(part.NumVertices > 0);
            Assert.True(part.PrimitiveCount > 0);

            // Model XNB readers supply byte[] blobs to generic SetData. This used to interpret
            // byte count as vertex/index count and fail in native before a model could load.
            game.Content.Unload();
            Assert.True(part.VertexBuffer.IsDisposed);
            Assert.True(part.IndexBuffer.IsDisposed);
            Assert.True(part.Effect.IsDisposed);
        });
    }

    /// <summary>Exercises the raw-byte Texture3D ABI route through XNA's generic overload. A
    /// uint is intentionally used for a four-byte Color texel so this cannot fall back to the
    /// managed Color conversion path.</summary>
    [global::CNA.Integration.Tests.NativeFactRequiring(global::CNA.Graphics.GraphicsCapability.Texture3D)]
    public void CompatTexture3D_SetDataAcceptsBlittableNonColorElements()
    {
        InsideACompatFrame(game =>
        {
            if (!global::CNA.XnaCompat.Extensions.CnaGraphicsDeviceExtensions.SupportsCnaCapability(
                    game.GraphicsDevice,
                    global::CNA.XnaCompat.Extensions.CnaGraphicsCapability.Texture3D))
            {
                output.WriteLine("NOT EXERCISED: the active renderer does not support Texture3D.");
                return;
            }

            using var texture = new Texture3D(game.GraphicsDevice, 1, 1, 1, false, SurfaceFormat.Color);
            texture.SetData<uint>([0xFFFFFFFFu]);

            Assert.Equal(1, texture.Depth);
        });
    }

    /// <summary>
    /// The listener-array overload must be one native request, and it must succeed.
    ///
    /// CNA carried the whole array across the ABI from 0.6.0, but the implementation behind it
    /// refused every count other than one until CABI-6, so this test used to assert
    /// <c>NotSupportedException</c>. The only admitted runtime now accepts any count of one or more
    /// and applies the dominant listener, which is what XNA does; asserting the refusal would now
    /// be asserting a limitation that no admitted library has. What the managed layer must still
    /// never do is loop over the array applying one listener at a time, which would silently retain
    /// only the last -- that is why this goes through the atomic route and then checks the instance
    /// is still usable through the canonical one.
    /// </summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatSoundEffectInstance_AppliesAMultipleListenerArrayAtomically()
    {
        InsideACompatFrame(_ =>
        {
            var pcm = new byte[44100 / 20 * 2];
            using var effect = new XnaAudio.SoundEffect(pcm, 44100, XnaAudio.AudioChannels.Mono);
            using XnaAudio.SoundEffectInstance instance = effect.CreateInstance();

            XnaAudio.AudioListener[] listeners = [new XnaAudio.AudioListener(), new XnaAudio.AudioListener()];
            listeners[1].Position = new Vector3(10f, 0f, 0f);

            instance.Apply3D(listeners, new XnaAudio.AudioEmitter());

            // Applying again with a single listener must remain legal afterwards: the array route
            // must not have left the instance in a state the canonical route cannot use.
            instance.Apply3D(listeners[0], new XnaAudio.AudioEmitter());
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

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_StateAndCollectionWrappersHaveXnaIdentity()
    {
        InsideACompatFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            Assert.Same(BlendState.Opaque, device.BlendState);
            Assert.Same(DepthStencilState.Default, device.DepthStencilState);
            Assert.Same(RasterizerState.CullCounterClockwise, device.RasterizerState);
            Assert.Same(device, device.BlendState.GraphicsDevice);
            Assert.Equal("BlendState.Opaque", BlendState.Opaque.Name);
            Assert.Equal("DepthStencilState.Default", DepthStencilState.Default.Name);
            Assert.Equal("RasterizerState.CullCounterClockwise", RasterizerState.CullCounterClockwise.Name);
            Assert.Equal("SamplerState.LinearWrap", SamplerState.LinearWrap.Name);
            Assert.Throws<InvalidOperationException>(() =>
                BlendState.Opaque.ColorSourceBlend = Blend.Zero);

            Assert.Same(device.SamplerStates, device.SamplerStates);
            Assert.Same(device.VertexSamplerStates, device.VertexSamplerStates);
            Assert.Same(device.Textures, device.Textures);
            Assert.Same(device.VertexTextures, device.VertexTextures);
            Assert.Same(SamplerState.LinearWrap, device.SamplerStates[0]);

            using var blend = new BlendState { ColorSourceBlend = Blend.SourceAlpha };
            device.BlendState = blend;
            Assert.Same(blend, device.BlendState);
            Assert.Same(device, blend.GraphicsDevice);
            Assert.Throws<InvalidOperationException>(() => blend.ColorSourceBlend = Blend.One);
            device.BlendState = BlendState.Opaque;

            using var sampler = new SamplerState { Filter = TextureFilter.Point };
            device.SamplerStates[0] = sampler;
            Assert.Same(sampler, device.SamplerStates[0]);
            Assert.Throws<InvalidOperationException>(() => sampler.Filter = TextureFilter.Linear);
            device.SamplerStates[0] = SamplerState.LinearWrap;

            using var texture = new Texture2D(device, 1, 1);
            TextureCollection first = device.Textures;
            first[0] = texture;
            Assert.Same(texture, device.Textures[0]);
            device.Textures[0] = null;
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsResource_DisposingSeesDisposedStateAndFiresOnceAfterHandlerFailure()
    {
        InsideACompatFrame(_ =>
        {
            var state = new BlendState
            {
                Name = "lifecycle-state",
                Tag = new object(),
            };
            object? tag = state.Tag;
            int calls = 0;

            state.Disposing += (_, _) =>
            {
                calls++;
                Assert.True(state.IsDisposed);
                throw new InvalidOperationException("handler failure");
            };

            Assert.Throws<InvalidOperationException>(state.Dispose);
            Assert.True(state.IsDisposed);
            Assert.Equal(1, calls);

            state.Dispose();
            Assert.Equal(1, calls);
            Assert.Equal("lifecycle-state", state.Name);
            Assert.Same(tag, state.Tag);
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_PresentOverloadRejectsUnrepresentableArguments()
    {
        InsideACompatFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            Assert.Throws<NotSupportedException>(() =>
                device.Present(new Rectangle(0, 0, 1, 1), null, IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() =>
                device.Present(null, new Rectangle(0, 0, 1, 1), IntPtr.Zero));
            Assert.Throws<NotSupportedException>(() =>
                device.Present(null, null, new IntPtr(1)));
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_ResourceEventSubscriptionIsSafeWhileAbiCannotReportWrappers()
    {
        InsideACompatFrame(game =>
        {
            int created = 0;
            int destroyed = 0;
            EventHandler<ResourceCreatedEventArgs> onCreated = (_, _) => created++;
            EventHandler<ResourceDestroyedEventArgs> onDestroyed = (_, _) => destroyed++;

            game.GraphicsDevice.ResourceCreated += onCreated;
            game.GraphicsDevice.ResourceDestroyed += onDestroyed;
            using (var texture = new Texture2D(game.GraphicsDevice, 1, 1))
            {
                texture.Name = "resource-event-safety";
            }

            game.GraphicsDevice.ResourceCreated -= onCreated;
            game.GraphicsDevice.ResourceDestroyed -= onDestroyed;

            // ABI 0.6 reports only presence/name bytes and cannot identify the actual facade
            // object required by XNA. The safe fallback is no event, never a null/fake resource or
            // the former callback-signature crash.
            Assert.Equal(0, created);
            Assert.Equal(0, destroyed);
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatTexture_DescriptionRemainsReadableAfterDispose()
    {
        InsideACompatFrame(game =>
        {
            var texture = new Texture2D(game.GraphicsDevice, 2, 3, false, SurfaceFormat.Color);
            texture.Dispose();

            Assert.Equal(2, texture.Width);
            Assert.Equal(3, texture.Height);
            Assert.Equal(1, texture.LevelCount);
            Assert.Equal(SurfaceFormat.Color, texture.Format);
            Assert.Equal(new Rectangle(0, 0, 2, 3), texture.Bounds);
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatSpriteBatch_InvalidOrderPreservesXnaStateMachine()
    {
        InsideACompatFrame(game =>
        {
            using var texture = new Texture2D(game.GraphicsDevice, 1, 1);
            texture.SetData([Color.White]);
            using var batch = new SpriteBatch(game.GraphicsDevice);

            Assert.Throws<InvalidOperationException>(batch.End);
            Assert.Throws<ArgumentNullException>(() =>
                batch.Draw(null!, Vector2.Zero, Color.White));
            Assert.Throws<InvalidOperationException>(() =>
                batch.Draw(texture, Vector2.Zero, Color.White));

            batch.Begin();
            Assert.Throws<InvalidOperationException>(batch.Begin);
            batch.End();
            Assert.Throws<InvalidOperationException>(() =>
                batch.Draw(texture, Vector2.Zero, Color.White));

            batch.Begin();
            batch.End();
        });
    }

    /// <summary>
    /// Where <c>SetDataOptions</c> reaches the ABI, and where it deliberately cannot.
    ///
    /// The vertex half used to assert <c>NotSupportedException</c> for a windowed optioned upload,
    /// because the option reached the ABI only through the built-in typed route. CNA 0.19.0 added
    /// <c>cna_vertex_buffer_set_data_raw_with_options</c> and its windowed twin, so the option is
    /// now forwarded and the bytes are asserted where XNA puts them.
    ///
    /// The index half still refuses, and that is upstream's stated position rather than a gap in
    /// the ABI shape: <c>cna_index_buffer_set_data_at</c> carries an options field and rejects
    /// anything but <c>None</c>, because "a windowed upload preserves the rest of the buffer, so it
    /// accepts no SetDataOptions other than None". XNA does accept them there, so this stays
    /// recorded as a behavioural difference in docs/native-behavior-blockers.md; what this test
    /// pins is that the difference surfaces as a refusal rather than a dropped hint.
    /// </summary>
    [global::CNA.Integration.Tests.NativeFact]
    public void CompatDynamicBuffers_ForwardSetDataOptionsWhereTheAbiCarriesThem()
    {
        InsideACompatFrame(game =>
        {
            if (!global::CNA.XnaCompat.Extensions.CnaGraphicsDeviceExtensions.SupportsCnaCapability(
                    game.GraphicsDevice,
                    global::CNA.XnaCompat.Extensions.CnaGraphicsCapability.ThreeD))
            {
                output.WriteLine("NOT EXERCISED: the active renderer does not support 3D buffers.");
                return;
            }

            var vertices = new[]
            {
                new VertexPositionColor(Vector3.Zero, Color.Red),
                new VertexPositionColor(Vector3.UnitX, Color.Green),
                new VertexPositionColor(Vector3.UnitY, Color.Blue),
            };
            using var vertexBuffer = new DynamicVertexBuffer(
                game.GraphicsDevice,
                VertexPositionColor.VertexDeclaration,
                vertices.Length,
                BufferUsage.None);

            vertexBuffer.SetData(vertices, 0, vertices.Length, SetDataOptions.Discard);
            vertexBuffer.SetData(vertices, 0, vertices.Length, SetDataOptions.NoOverwrite);
            var vertexReadback = new VertexPositionColor[vertices.Length];
            vertexBuffer.GetData(vertexReadback);
            Assert.Equal(vertices[1].Position, vertexReadback[1].Position);

            // The windowed optioned upload. Asserting the neighbours is the part that matters: a
            // route that confused the buffer offset with the caller-array offset would still write
            // plausible data and still round-trip the vertex it was asked about.
            int stride = VertexPositionColor.VertexDeclaration.VertexStride;
            var replacement = new VertexPositionColor(Vector3.UnitZ, Color.White);
            vertexBuffer.SetData(stride, new[] { replacement }, 0, 1, stride, SetDataOptions.NoOverwrite);
            vertexBuffer.GetData(vertexReadback);
            Assert.Equal(vertices[0].Position, vertexReadback[0].Position);
            Assert.Equal(replacement.Position, vertexReadback[1].Position);
            Assert.Equal(vertices[2].Position, vertexReadback[2].Position);

            using var indexBuffer = new DynamicIndexBuffer(
                game.GraphicsDevice,
                IndexElementSize.SixteenBits,
                3,
                BufferUsage.None);
            ushort[] indices = [0, 1, 2];
            indexBuffer.SetData(indices, 0, indices.Length, SetDataOptions.None);
            indexBuffer.SetData(indices, 0, indices.Length, SetDataOptions.Discard);
            indexBuffer.SetData(indices, 0, indices.Length, SetDataOptions.NoOverwrite);

            // A windowed optioned upload. This used to throw, which broke the commonest use of the
            // overload -- a batcher rewriting one slice per frame with NoOverwrite.
            indexBuffer.SetData(2, new ushort[] { 7 }, 0, 1, SetDataOptions.NoOverwrite);

            var indexReadback = new ushort[3];
            indexBuffer.GetData(indexReadback);
            Assert.Equal([(ushort)0, (ushort)7, (ushort)2], indexReadback);

            // And reading back from a nonzero offset, which had no route at all.
            var tail = new ushort[2];
            indexBuffer.GetData(2, tail, 0, 2);
            Assert.Equal([(ushort)7, (ushort)2], tail);
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatGraphicsDevice_DrawValidationMatchesXnaBeforeNativeDispatch()
    {
        InsideACompatFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            var vertices = new[]
            {
                new VertexPositionColor(Vector3.Zero, Color.Red),
                new VertexPositionColor(Vector3.UnitX, Color.Green),
                new VertexPositionColor(Vector3.UnitY, Color.Blue),
            };
            short[] indices = [0, 1, 2];

            Assert.Equal("vertexData", Assert.Throws<ArgumentNullException>(() =>
                device.DrawUserPrimitives(
                    PrimitiveType.TriangleList, (VertexPositionColor[])null!, 0, 1)).ParamName);
            Assert.Equal("vertexOffset", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawUserPrimitives(
                    PrimitiveType.TriangleList, new VertexPositionColor[0], 0, 1)).ParamName);
            Assert.Equal("primitiveCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawUserPrimitives(
                    PrimitiveType.TriangleList, vertices, 1, 1)).ParamName);

            Assert.Equal("vertexData", Assert.Throws<ArgumentNullException>(() =>
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, new VertexPositionColor[0], 0, 3,
                    indices, 0, 1)).ParamName);
            Assert.Equal("indexData", Assert.Throws<ArgumentNullException>(() =>
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vertices, 0, 3,
                    Array.Empty<short>(), 0, 1)).ParamName);
            Assert.Equal("numVertices", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vertices, 0, 0, indices, 0, 1)).ParamName);
            Assert.Equal("primitiveCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vertices, 0, 3, new short[2], 0, 1)).ParamName);
            Assert.Equal("vertexData", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vertices, 1, 3, indices, 0, 1)).ParamName);

            using var declaration = new VertexDeclaration(
                16,
                new VertexElement(
                    0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            var managedVertices = new[]
            {
                new ManagedReferenceVertex(Vector3.Zero, "a"),
                new ManagedReferenceVertex(Vector3.UnitX, "b"),
                new ManagedReferenceVertex(Vector3.UnitY, "c"),
            };
            ArgumentException managedReference = Assert.Throws<ArgumentException>(() =>
                device.DrawUserPrimitives(
                    PrimitiveType.TriangleList, managedVertices, 0, 1, declaration));
            Assert.Equal("vertexData", managedReference.ParamName);

            Assert.Equal("primitiveCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 0)).ParamName);
            Assert.Equal("numVertices", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, 0, 0, 1)).ParamName);
            Assert.Equal("instanceCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
                device.DrawInstancedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, 3, 0, 1, 0)).ParamName);

            using var effect = new BasicEffect(device);
            effect.CurrentTechnique.Passes[0].Apply();
            Assert.Throws<InvalidOperationException>(() =>
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 1));
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

            effect.CurrentTechnique.Passes[0].Apply();

            Assert.Equal(0.5f, effect.DiffuseColor.X, 1e-4f);
            output.WriteLine($"diffuse={effect.DiffuseColor} world={effect.World.M11}");

            using Effect clone = effect.Clone();
            Assert.IsAssignableFrom<BasicEffect>(clone);
        });
    }

    [global::CNA.Integration.Tests.NativeFact]
    public void CompatEffectReflection_ReturnsStableWrappersWithoutOwnedHandleChurn()
    {
        InsideACompatFrame(game =>
        {
            using var effect = new BasicEffect(game.GraphicsDevice);

            Assert.Same(effect.Parameters, effect.Parameters);
            Assert.Same(effect.Techniques, effect.Techniques);

            EffectTechnique technique = effect.CurrentTechnique;
            Assert.Same(technique, effect.CurrentTechnique);
            Assert.Same(technique, effect.Techniques[technique.Name]);
            Assert.Same(technique.Passes, technique.Passes);
            Assert.Same(technique.Annotations, technique.Annotations);
            Assert.Null(effect.Techniques[-1]);
            Assert.Null(effect.Techniques[(string)null!]);

            EffectPass pass = technique.Passes[0];
            Assert.Same(pass, technique.Passes[0]);
            Assert.Same(pass, technique.Passes[pass.Name]);
            Assert.Same(pass.Annotations, pass.Annotations);
            Assert.Null(technique.Passes[-1]);

            if (effect.Parameters.Count > 0)
            {
                EffectParameter parameter = effect.Parameters[0];
                Assert.Same(parameter, effect.Parameters[0]);
                Assert.Same(parameter, effect.Parameters[parameter.Name]);
                Assert.Same(parameter.Elements, parameter.Elements);
                Assert.Same(parameter.StructureMembers, parameter.StructureMembers);
                Assert.Same(parameter.Annotations, parameter.Annotations);
                Assert.Null(effect.Parameters[-1]);
                Assert.Null(effect.Parameters[(string)null!]);
            }

            for (int i = 0; i < 256; i++)
            {
                Assert.Same(technique, effect.CurrentTechnique);
                Assert.Same(pass, technique.Passes[0]);
            }
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
