using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The two things a game actually draws with: text through a <see cref="SpriteFont"/>, and geometry
/// through a <see cref="Model"/>.
///
/// Most of them are built by hand here rather than loaded, because the assets cannot be produced in
/// this repository -- a compiled <c>.xnb</c> font needs the XNA content pipeline, and a model needs
/// a mesh. That is a real limit on what those prove and it is worth being exact about: the *load*
/// paths are not exercised, the *draw* paths are. Hand-building is enough for the second, since the
/// glyph table and the buffers cross the same ABI either way.
///
/// The exception is <see cref="SpriteFont_LoadsAnAuthoredCompressedAtlasThroughContent"/>, which
/// loads the one authored content-pipeline font this repository does have.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class DrawingIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>A three-glyph font over a 3x1 atlas: 'A', 'B' and a space, each one texel wide.</summary>
    private static SpriteFont BuildFont(GraphicsDevice device)
    {
        var atlas = new Texture2D(device, 3, 1);
        atlas.SetData([Color.White, Color.Gray, Color.Black]);

        return new SpriteFont(
            atlas,
            glyphBounds: [new Rectangle(0, 0, 1, 1), new Rectangle(1, 0, 1, 1), new Rectangle(2, 0, 1, 1)],
            cropping: [new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1)],
            characters: [' ', 'A', 'B'],
            lineSpacing: 2,
            spacing: 0f,
            kerning: [new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f)],
            defaultCharacter: ' ');
    }

    /// <summary>
    /// Measuring is pure managed arithmetic over the glyph table, so it is exact and worth
    /// asserting exactly: three one-texel glyphs with no spacing measure three wide, and the height
    /// is the line spacing.
    /// </summary>
    [NativeFact]
    public void SpriteFont_MeasuresFromItsGlyphTable()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            SpriteFont font = BuildFont(device);

            Vector2 measured = font.MeasureString("AB");
            output.WriteLine($"'AB' measures {measured}");

            Assert.Equal(2f, measured.X, 1e-4f);
            Assert.Equal(2f, measured.Y, 1e-4f);

            Assert.Equal(Vector2.Zero.X, font.MeasureString(string.Empty).X, 1e-4f);
        });
    }

    /// <summary>
    /// <c>DrawString</c> end to end, inside a real Begin/End pass.
    ///
    /// This is the member that made per-glyph readback worth asking upstream for: measuring sizes a
    /// whole string, and placing a glyph needs its atlas rectangle, cropping offset and three
    /// kerning values. A font that can be measured and not drawn is the failure this guards.
    /// </summary>
    [NativeFact]
    public void SpriteBatch_DrawString_Succeeds()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            SpriteFont font = BuildFont(device);
            using var batch = new SpriteBatch(device);

            device.Clear(Color.Black);
            batch.Begin();
            batch.DrawString(font, "AB", new Vector2(4f, 8f), Color.White);
            batch.End();
        });
    }

    /// <summary>An unmapped character must fall back to the default rather than throwing or drawing
    /// nothing -- XNA's rule, and the reason a font carries a default character at all.</summary>
    [NativeFact]
    public void SpriteFont_UnmappedCharacter_FallsBackToTheDefault()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            SpriteFont font = BuildFont(device);

            Vector2 measured = font.MeasureString("AZB");
            output.WriteLine($"'AZB' (Z unmapped) measures {measured}");

            // Three glyphs wide: the unmapped Z becomes the default space, which is also one wide.
            Assert.Equal(3f, measured.X, 1e-4f);
        });
    }

    /// <summary>
    /// A hand-built model drawn through its own <c>Draw</c>, which walks bones, applies each part's
    /// effect and submits indexed geometry -- three ABI surfaces at once.
    /// </summary>
    [Native3DFact]
    public void Model_DrawsItsMeshes()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!CnaNativeProbe.HasCapabilityOrRefuses(
                    device,
                    GraphicsCapability.ThreeD,
                    "creating a VertexBuffer",
                    () => new VertexBuffer(
                        device, VertexPositionColor.VertexDeclaration, 4, BufferUsage.None).Dispose(),
                    output))
            {
                return;
            }

            var vertices = new[]
            {
                new VertexPositionColor(new Vector3(0f, 0f, 0f), Color.Red),
                new VertexPositionColor(new Vector3(1f, 0f, 0f), Color.Green),
                new VertexPositionColor(new Vector3(0f, 1f, 0f), Color.Blue),
            };

            using var vertexBuffer = new VertexBuffer(
                device, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.None);
            vertexBuffer.SetData(vertices);

            using var indexBuffer = new IndexBuffer(
                device, IndexElementSize.SixteenBits, 3, BufferUsage.None);
            indexBuffer.SetData<ushort>([0, 1, 2]);

            var part = new ModelMeshPart(vertexBuffer, indexBuffer, vertices.Length, 1, 0, 0)
            {
                Effect = new BasicEffect(device) { VertexColorEnabled = true },
            };

            var mesh = new ModelMesh(device, "triangle", [part]);
            var bone = new ModelBone(0, "root") { Transform = Matrix.Identity };
            var model = new Model(device, [bone], [mesh]);

            output.WriteLine($"{model.Bones.Count} bone(s), {model.Meshes.Count} mesh(es), {mesh.MeshParts.Count} part(s)");

            device.Clear(Color.Black);
            model.Draw(Matrix.Identity, Matrix.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.Up),
                Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.6f, 0.1f, 100f));

            Assert.Single(model.Meshes);
            Assert.Single(model.Bones);
        });
    }

    /// <summary>Bone transforms copied out, which is how a game animates a model. A wrong stride
    /// here fills the array with plausible matrices belonging to the wrong bones.</summary>
    [Native3DFact]
    public void Model_CopiesAbsoluteBoneTransforms()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!CnaNativeProbe.HasCapabilityOrRefuses(
                    device,
                    GraphicsCapability.ThreeD,
                    "creating a VertexBuffer",
                    () => new VertexBuffer(
                        device, VertexPositionColor.VertexDeclaration, 4, BufferUsage.None).Dispose(),
                    output))
            {
                return;
            }

            var bones = new[]
            {
                new ModelBone(0, "root") { Transform = Matrix.CreateTranslation(1f, 0f, 0f) },
                new ModelBone(1, "child") { Transform = Matrix.CreateTranslation(0f, 2f, 0f) },
            };

            var model = new Model(device, bones, []);

            var absolute = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(absolute);

            output.WriteLine($"root translation {absolute[0].Translation}, child {absolute[1].Translation}");

            Assert.Equal(1f, absolute[0].Translation.X, 1e-4f);
        });
    }

    /// <summary>
    /// A real content-pipeline <c>SpriteFont</c>, loaded end to end: LZX-decompressed XNB, DXT3
    /// atlas, glyph table, measured and drawn.
    ///
    /// This is the first test in the repository to push an authored compressed atlas at a renderer.
    /// The managed XNB and LZX tests have parsed this exact file for a long time, but the selected
    /// backend rejected compressed uploads, so the fixture was recorded as a backend limitation and
    /// excluded from the native stress cycle. The renderer now reports Dxt1/Dxt3/Dxt5 support and
    /// this loads.
    ///
    /// It is worth having as a whole-pipeline test rather than only as a format check: a font is
    /// the one common asset that exercises content loading, a compressed texture upload, the glyph
    /// table and SpriteBatch text drawing in a single call graph.
    /// </summary>
    [NativeFact]
    public void SpriteFont_LoadsAnAuthoredCompressedAtlasThroughContent()
    {
        fixture.InsideAFrame(game =>
        {
            game.Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "xnb", "lzx");

            SpriteFont font = game.Content.Load<SpriteFont>("FontCalibri14");

            output.WriteLine(
                $"{font.Characters.Count} glyph(s), line spacing {font.LineSpacing}, " +
                $"atlas format {font.Texture.Format}");

            Assert.NotEmpty(font.Characters);
            Assert.True(font.LineSpacing > 0);

            Vector2 measured = font.MeasureString("Hello");
            Assert.True(measured.X > 0f && measured.Y > 0f, $"measured {measured}");

            using var batch = new SpriteBatch(game.GraphicsDevice);
            game.GraphicsDevice.Clear(Color.Black);
            batch.Begin();
            batch.DrawString(font, "Hello", Vector2.Zero, Color.White);
            batch.End();
        });
    }
}
