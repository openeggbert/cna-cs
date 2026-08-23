using System.Runtime.CompilerServices;
using System.Reflection;
using CNA.XnaCompat.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

namespace CnaOwnershipStress;

internal static class Program
{
    private const int DefaultCycles = 24;
    private const int DeepCycles = 1000;
    private static readonly string FinalizerFamily =
        Environment.GetEnvironmentVariable("CNA_OWNERSHIP_STRESS_FAMILY") ?? "all";

    private static int Main(string[] args)
    {
        int cycles = args.Length == 0
            ? Environment.GetEnvironmentVariable("CNA_OWNERSHIP_STRESS_DEEP") == "1"
                ? DeepCycles
                : DefaultCycles
            : int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
        if (cycles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "Cycle count must be positive.");
        }

        int explicitCycles = 0;
        int finalizerCycles = 0;
        int throwingDisposalCycles = 0;
        try
        {
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                bool abandon = (cycle & 1) != 0;
                bool exerciseThrowingDeviceHandler = cycle % 10 == 0;
                var game = new StressGame(abandon, exerciseThrowingDeviceHandler);
                try
                {
                    for (int frame = 0; frame < 4 && !game.Ran; frame++)
                    {
                        game.RunOneFrame();
                    }

                    if (!game.Ran)
                    {
                        throw new InvalidOperationException($"Cycle {cycle}: the resource frame never ran.");
                    }

                    if (game.Failure is not null)
                    {
                        throw new InvalidOperationException($"Cycle {cycle}: resource exercise failed.", game.Failure);
                    }

                    if (abandon)
                    {
                        ForceFinalizers();
                        if (game.AbandonedResources is null ||
                            game.AbandonedResources.Any(static reference => reference.IsAlive))
                        {
                            throw new InvalidOperationException(
                                $"Cycle {cycle}: an intentionally abandoned resource remained rooted.");
                        }

                        finalizerCycles++;
                    }
                    else
                    {
                        explicitCycles++;
                    }

                    bool observedExpectedException = false;
                    try
                    {
                        game.Dispose();
                    }
                    catch (ExpectedDisposingException)
                    {
                        observedExpectedException = true;
                    }

                    if (exerciseThrowingDeviceHandler &&
                        (!observedExpectedException || game.DeviceDisposingCount != 1))
                    {
                        throw new InvalidOperationException(
                            $"Cycle {cycle}: throwing device disposal handler did not run exactly once.");
                    }

                    if (!exerciseThrowingDeviceHandler && observedExpectedException)
                    {
                        throw new InvalidOperationException(
                            $"Cycle {cycle}: an unexpected device disposal exception escaped.");
                    }

                    if (exerciseThrowingDeviceHandler)
                    {
                        throwingDisposalCycles++;
                    }

                    game.Dispose();
                }
                finally
                {
                    game.Dispose();
                }

                ForceFinalizers();
            }

            Console.WriteLine(
                $"ownership-stress cycles={cycles} explicit={explicitCycles} finalizer={finalizerCycles} " +
                $"throwing-device-dispose={throwingDisposalCycles} family={FinalizerFamily} " +
                "game-recreate=ok");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void ForceFinalizers()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private sealed class ExpectedDisposingException : Exception
    {
    }

    private sealed class StressGame(bool abandon, bool throwFromDeviceDisposing) : Game
    {
        public bool Ran { get; private set; }

        public Exception? Failure { get; private set; }

        public WeakReference[]? AbandonedResources { get; private set; }

        public int DeviceDisposingCount { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            if (!Ran)
            {
                Ran = true;
                try
                {
                    if (throwFromDeviceDisposing)
                    {
                        GraphicsDevice device = GraphicsDevice;
                        device.Disposing += (sender, _) =>
                        {
                            DeviceDisposingCount++;
                            if (!ReferenceEquals(sender, device) || !device.IsDisposed)
                            {
                                throw new InvalidOperationException(
                                    "GraphicsDevice.Disposing exposed the wrong sender or state.");
                            }

                            throw new ExpectedDisposingException();
                        };
                    }

                    if (abandon)
                    {
                        AbandonedResources = AllocateAndAbandon(GraphicsDevice);
                    }
                    else
                    {
                        ExerciseExplicitDisposal(GraphicsDevice);
                    }
                }
                catch (Exception exception)
                {
                    Failure = exception;
                }
            }

            Exit();
            base.Update(gameTime);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseExplicitDisposal(GraphicsDevice device)
    {
        var disposables = new List<IDisposable>();
        Texture2D? texture = null;
        if (Enabled("texture") || Enabled("batch") || Enabled("font"))
        {
            texture = CreateTexture(device);
            disposables.Add(texture);
            device.Textures[0] = texture;
            device.Textures[0] = null;
        }

        if (Enabled("batch"))
        {
            disposables.Add(CreateAndUseBatch(device, texture!));
        }

        if (Enabled("font"))
        {
            _ = CreateAndUseSpriteFont(texture!);
        }

        if (Enabled("effect"))
        {
            disposables.Add(CreateAndUseEffect(device));
        }

        if (Enabled("sound"))
        {
            disposables.Add(CreateSoundEffect());
        }

        if (Enabled("media"))
        {
            (MediaLibrary library, SongCollection songs) = CreateMediaLibrary();
            disposables.Add(library);
            disposables.Add(songs);
        }

        if (Enabled("storage"))
        {
            (StorageDevice _, StorageContainer container) = CreateStorageObjects();
            disposables.Add(container);
        }

        if (Enabled("buffers"))
        {
            (DynamicVertexBuffer? vertices, DynamicIndexBuffer? indices) = CreateBuffers(device);
            if (vertices is not null)
            {
                disposables.Add(vertices);
            }

            if (indices is not null)
            {
                disposables.Add(indices);
            }
        }

        if (Enabled("adopted"))
        {
            disposables.Add(CreateAdoptedTexture(device));
        }

        if (Enabled("content"))
        {
            (ContentManager manager, Model model) = CreateContentAssets(device);
            _ = model.Meshes[0].MeshParts[0].Effect;
            disposables.Add(manager);
        }

        if (disposables.Count == 0)
        {
            throw new ArgumentException(
                $"Unknown CNA_OWNERSHIP_STRESS_FAMILY '{FinalizerFamily}'.", nameof(FinalizerFamily));
        }

        for (int index = disposables.Count - 1; index >= 0; index--)
        {
            disposables[index].Dispose();
            disposables[index].Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] AllocateAndAbandon(GraphicsDevice device)
    {
        var references = new List<WeakReference>();

        Texture2D? texture = null;
        if (Enabled("texture") || Enabled("batch") || Enabled("font"))
        {
            texture = CreateTexture(device);
            references.Add(new WeakReference(texture));
            device.Textures[0] = texture;
            device.Textures[0] = null;
        }

        if (Enabled("batch"))
        {
            references.Add(new WeakReference(CreateAndUseBatch(device, texture!)));
        }

        if (Enabled("font"))
        {
            references.Add(new WeakReference(CreateAndUseSpriteFont(texture!)));
        }

        if (Enabled("effect"))
        {
            references.Add(new WeakReference(CreateAndUseEffect(device)));
        }

        if (Enabled("sound"))
        {
            references.Add(new WeakReference(CreateSoundEffect()));
        }

        if (Enabled("media"))
        {
            (MediaLibrary library, SongCollection songs) = CreateMediaLibrary();
            references.Add(new WeakReference(library));
            references.Add(new WeakReference(songs));
        }

        if (Enabled("storage"))
        {
            (StorageDevice storageDevice, StorageContainer container) = CreateStorageObjects();
            references.Add(new WeakReference(storageDevice));
            references.Add(new WeakReference(container));
        }

        if (Enabled("buffers"))
        {
            (DynamicVertexBuffer? vertices, DynamicIndexBuffer? indices) = CreateBuffers(device);
            references.Add(new WeakReference(vertices));
            references.Add(new WeakReference(indices));
        }

        if (Enabled("adopted"))
        {
            references.Add(new WeakReference(CreateAdoptedTexture(device)));
        }

        if (Enabled("content"))
        {
            (ContentManager manager, Model model) = CreateContentAssets(device);
            references.Add(new WeakReference(manager));
            references.Add(new WeakReference(model));
            references.Add(new WeakReference(model.Bones[0]));
            references.Add(new WeakReference(model.Meshes[0]));
            references.Add(new WeakReference(model.Meshes[0].MeshParts[0]));
            references.Add(new WeakReference(model.Meshes[0].MeshParts[0].Effect));
        }

        if (references.Count == 0)
        {
            throw new ArgumentException(
                $"Unknown CNA_OWNERSHIP_STRESS_FAMILY '{FinalizerFamily}'.", nameof(FinalizerFamily));
        }

        return references.ToArray();
    }

    private static bool Enabled(string family) =>
        FinalizerFamily == "all" || FinalizerFamily == family;

    private static Texture2D CreateTexture(GraphicsDevice device)
    {
        var texture = new Texture2D(device, 2, 2, false, SurfaceFormat.Color);
        texture.SetData([Color.Red, Color.Green, Color.Blue, Color.White]);
        return texture;
    }

    private static SpriteBatch CreateAndUseBatch(GraphicsDevice device, Texture2D texture)
    {
        var batch = new SpriteBatch(device);
        batch.Begin();
        batch.Draw(texture, Vector2.Zero, Color.White);
        batch.End();
        return batch;
    }

    private static BasicEffect CreateAndUseEffect(GraphicsDevice device)
    {
        var effect = new BasicEffect(device);
        EffectTechnique technique = effect.CurrentTechnique;
        EffectPass pass = technique.Passes[0];
        _ = effect.Parameters.Count;
        _ = technique.Annotations.Count;
        _ = pass.Annotations.Count;
        pass.Apply();
        return effect;
    }

    private static SoundEffect CreateSoundEffect() =>
        new(new byte[512], 8000, AudioChannels.Mono);

    private static (MediaLibrary, SongCollection) CreateMediaLibrary()
    {
        var library = new MediaLibrary();
        SongCollection songs = library.Songs;
        _ = songs.Count;
        return (library, songs);
    }

    private static (StorageDevice, StorageContainer) CreateStorageObjects()
    {
        IAsyncResult selection = StorageDevice.BeginShowSelector(null, null);
        StorageDevice device = StorageDevice.EndShowSelector(selection);
        IAsyncResult opening = device.BeginOpenContainer(
            "cna-ownership-stress", null, null);
        StorageContainer container = device.EndOpenContainer(opening);
        _ = container.DisplayName;
        return (device, container);
    }

    private static (DynamicVertexBuffer?, DynamicIndexBuffer?) CreateBuffers(GraphicsDevice device)
    {
        if (!device.SupportsCnaCapability(CnaGraphicsCapability.ThreeD))
        {
            return (null, null);
        }

        var vertices = new DynamicVertexBuffer(
            device,
            VertexPositionColor.VertexDeclaration,
            3,
            BufferUsage.WriteOnly);
        vertices.SetData(
            [
                new VertexPositionColor(Vector3.Zero, Color.Red),
                new VertexPositionColor(Vector3.UnitX, Color.Green),
                new VertexPositionColor(Vector3.UnitY, Color.Blue),
            ],
            0,
            3,
            SetDataOptions.Discard);

        var indices = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, 3, BufferUsage.WriteOnly);
        indices.SetData<ushort>([0, 1, 2], 0, 3, SetDataOptions.Discard);
        return (vertices, indices);
    }

    private static Texture2D CreateAdoptedTexture(GraphicsDevice device)
    {
        byte[] png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        using var stream = new MemoryStream(png, writable: false);
        return Texture2D.FromStream(device, stream);
    }

    private static (ContentManager Manager, Model Model) CreateContentAssets(
        GraphicsDevice device)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "assets", "xnb");
        var manager = new ContentManager(new GraphicsDeviceService(device), root);
        Model model = manager.Load<Model>("BlenderDefaultCube");
        return (manager, model);
    }

    private static SpriteFont CreateAndUseSpriteFont(Texture2D texture)
    {
        ConstructorInfo constructor = typeof(SpriteFont).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [
                typeof(Texture2D),
                typeof(IReadOnlyList<Rectangle>),
                typeof(IReadOnlyList<Rectangle>),
                typeof(IReadOnlyList<char>),
                typeof(int),
                typeof(float),
                typeof(IReadOnlyList<Vector3>),
                typeof(char?),
            ],
            modifiers: null) ?? throw new MissingMethodException(typeof(SpriteFont).FullName, ".ctor");

        var font = (SpriteFont)constructor.Invoke(
        [
            texture,
            new Rectangle[] { new(0, 0, 1, 1) },
            new Rectangle[] { new(0, 0, 1, 1) },
            new char[] { 'A' },
            1,
            0f,
            new Vector3[] { new(0f, 1f, 1f) },
            null,
        ]);
        _ = font.MeasureString("A");
        return font;
    }

    private sealed class GraphicsDeviceService(GraphicsDevice graphicsDevice)
        : IServiceProvider, IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;

        public event EventHandler<EventArgs>? DeviceCreated
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs>? DeviceDisposing
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs>? DeviceReset
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs>? DeviceResetting
        {
            add { }
            remove { }
        }

        public object? GetService(Type serviceType) =>
            serviceType == typeof(IGraphicsDeviceService) ? this : null;
    }
}
