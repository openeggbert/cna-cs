using CNA.Content.Cnb;
using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D2's second vertical slice: a <c>.cnb</c> texture container decoded and uploaded as a real
/// <see cref="Texture2D"/>.
///
/// Fixtures are written by <c>cna_cnb_encode_texture2d</c> at test time, for the reason
/// <see cref="CnbDocumentTests"/> records: a container assembled here from a reading of the schema
/// would test this repository's understanding against itself.
///
/// <b>Every assertion here is about payload, not shape.</b> A decoder that returned correctly sized
/// zeros would satisfy every dimension and byte-count check in this file and fail only the pixel
/// comparisons, which is why the pixel comparisons are the point and the rest is scaffolding.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbTextureTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>A 4x2 image whose every texel is distinct in all four channels, so a decoder that
    /// transposed rows and columns, dropped a channel, or reordered RGBA fails rather than
    /// matching.</summary>
    private static byte[] Rgba(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            pixels[(i * 4) + 0] = (byte)(10 + i);
            pixels[(i * 4) + 1] = (byte)(70 + i);
            pixels[(i * 4) + 2] = (byte)(130 + i);
            pixels[(i * 4) + 3] = (byte)(200 + i);
        }

        return pixels;
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"cna-cnbtex-{Guid.NewGuid():N}.cnb");

    /// <summary>
    /// Whether a format stores 4x4 blocks, which is what decides that a level's byte count is not
    /// width * height * bytes-per-texel.
    ///
    /// Asked of CNA rather than derived from the name: <c>Bc1</c> looks block-compressed and
    /// <c>HdrBlendable</c> does not, but a reader that guessed from spelling would be guessing.
    /// Both answers are asserted, so a route that returned a constant fails.
    /// </summary>
    [NativeFact]
    public void IsBlockCompressed_AnswersPerFormat()
    {
        // One Fact rather than a Theory: NativeFact carries the "no native library" skip and xUnit
        // permits only one Fact-or-Theory attribute per method, so a theory here would run without
        // the library and fail with a DllNotFoundException instead of skipping.
        (CnbTextureFormat Format, bool Expected)[] cases =
        [
            (CnbTextureFormat.Rgba8, false),
            (CnbTextureFormat.Rgba32Float, false),
            (CnbTextureFormat.Bc1, true),
            (CnbTextureFormat.Bc3Srgb, true),
            (CnbTextureFormat.Bc7, true),
        ];

        foreach ((CnbTextureFormat format, bool expected) in cases)
        {
            Assert.Equal(expected, format.IsBlockCompressed());
        }
    }

    [NativeFact]
    public void DecodedTexture_ReportsItsShapeAndReturnsItsExactBytes()
    {
        const int Width = 4;
        const int Height = 2;
        string path = TempPath();
        byte[] written = Rgba(Width, Height);

        try
        {
            CnbTestWriter.WriteRgba8Texture2D(path, Width, Height, written, "diffuse");

            using var document = CnbDocument.Open(path);
            using CnbTexture texture = CnbTexture.DecodeTexture2D(document);

            output.WriteLine(
                $"{texture.Width}x{texture.Height}x{texture.Depth} faces={texture.FaceCount} " +
                $"mips={texture.MipCount} representations={texture.RepresentationCount}");

            Assert.Equal(Width, texture.Width);
            Assert.Equal(Height, texture.Height);
            Assert.Equal(1, texture.Depth);
            Assert.Equal(1, texture.FaceCount);
            Assert.Equal(1, texture.MipCount);
            Assert.Equal(1, texture.RepresentationCount);
            Assert.Equal(CnbTextureFormat.Rgba8, texture.GetRepresentationFormat(0));
            Assert.Equal(1, texture.GetLevelCount(0));
            Assert.Equal((Width, Height, 1), texture.GetLevelDimensions(0));

            // The bytes, exactly. This is the assertion the rest of the file exists to support.
            Assert.Equal(written, texture.CopyLevel(0, texture.LevelIndex(face: 0, mipLevel: 0)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The selection callback: CNA calls it once per representation, in order, and the answer
    /// decides which one is chosen.
    ///
    /// A predicate that refuses everything must answer -1 rather than defaulting to zero. A loader
    /// that fell back to the first representation would upload bytes in a format the device said it
    /// could not hold, which produces a texture that looks like noise -- and that failure is much
    /// harder to trace than a refusal.
    /// </summary>
    [NativeFact]
    public void SelectRepresentation_AsksThePredicateAndReportsNoMatch()
    {
        string path = TempPath();

        try
        {
            CnbTestWriter.WriteRgba8Texture2D(path, 2, 2, Rgba(2, 2));

            using var document = CnbDocument.Open(path);
            using CnbTexture texture = CnbTexture.DecodeTexture2D(document);

            var offered = new List<CnbTextureFormat>();
            int chosen = texture.SelectRepresentation(format =>
            {
                offered.Add(format);
                return format == CnbTextureFormat.Rgba8;
            });

            Assert.Equal(0, chosen);
            Assert.Equal([CnbTextureFormat.Rgba8], offered);

            Assert.Equal(-1, texture.SelectRepresentation(_ => false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A managed exception thrown by the predicate must not unwind into C.
    ///
    /// It is captured on the way out, the remaining representations are answered "no", and it is
    /// rethrown once the native frame has returned -- so the caller sees their own exception and the
    /// runtime never tears a stack through unmanaged frames. The identity of the exception is
    /// asserted, not just its type, because catching and rewrapping would also produce "an
    /// exception".
    /// </summary>
    [NativeFact]
    public void SelectRepresentation_PredicateThatThrows_SurfacesTheSameExceptionAfterTheNativeFrame()
    {
        string path = TempPath();

        try
        {
            CnbTestWriter.WriteRgba8Texture2D(path, 2, 2, Rgba(2, 2));

            using var document = CnbDocument.Open(path);
            using CnbTexture texture = CnbTexture.DecodeTexture2D(document);

            var planted = new InvalidOperationException("from the predicate");
            InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
                () => texture.SelectRepresentation(_ => throw planted));

            Assert.Same(planted, thrown);

            // The description survived the unwind and still answers, which is what proves the
            // native side completed rather than being abandoned mid-call.
            Assert.Equal(CnbTextureFormat.Rgba8, texture.GetRepresentationFormat(0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// End to end: file on disk to a real texture on the device, read back and compared texel for
    /// texel.
    ///
    /// The read-back is what makes this more than a decode test -- it is the only thing that shows
    /// the bytes reached the GPU in the right order and the right format. On a renderer that cannot
    /// read a texture back the comparison is skipped and the texture's shape is still asserted,
    /// because a wrong size is visible without a readback.
    /// </summary>
    [NativeFact]
    public void LoadTexture2D_UploadsTheDecodedPixelsToTheDevice()
    {
        const int Width = 4;
        const int Height = 2;
        string path = TempPath();
        byte[] written = Rgba(Width, Height);

        try
        {
            CnbTestWriter.WriteRgba8Texture2D(path, Width, Height, written, "diffuse");

            fixture.InsideAFrameWithDevice(device =>
            {
                using Texture2D uploaded = CnbTextureLoader.LoadTexture2D(device, path);

                output.WriteLine($"uploaded {uploaded.Width}x{uploaded.Height} {uploaded.Format}");

                Assert.Equal(Width, uploaded.Width);
                Assert.Equal(Height, uploaded.Height);
                Assert.Equal(SurfaceFormat.Color, uploaded.Format);

                var read = new Color[Width * Height];
                uploaded.GetData(read);

                for (int i = 0; i < read.Length; i++)
                {
                    Assert.Equal(written[(i * 4) + 0], read[i].R);
                    Assert.Equal(written[(i * 4) + 1], read[i].G);
                    Assert.Equal(written[(i * 4) + 2], read[i].B);
                    Assert.Equal(written[(i * 4) + 3], read[i].A);
                }
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The device's own answer about a surface format, in the two masks CNA reports.
    ///
    /// <c>Color</c> must support <c>TextureStorage</c>, because a renderer that could not store the
    /// one format CNA's encoder writes could load no CNB texture at all -- a real claim, not a
    /// tautology.
    ///
    /// <c>Sampled</c> is deliberately *not* asserted supported, and that is the finding this test
    /// records. Measured on OPENGLES3, every one of the twenty-four formats reports the identical
    /// known mask -- <c>TextureStorage | RenderTarget | ColorTransfer</c> -- and sampling is not in
    /// it. So sampling is unclassified rather than refused, which is exactly the distinction the two
    /// masks exist to carry, and a projection that collapsed them would have to answer either "the
    /// renderer cannot sample any format" or "it can sample all of them", both of which are claims
    /// CNA did not make.
    /// </summary>
    [NativeFact]
    public void SurfaceFormatSupport_ReportsKnownAndSupportedSeparately()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            CnaSurfaceFormatSupport color = device.GetCnaSurfaceFormatSupport(SurfaceFormat.Color);
            output.WriteLine($"Color known={color.Known} supported={color.Supported}");

            Assert.True(color.IsSupported(CnaSurfaceFormatUsage.TextureStorage));
            Assert.False(color.IsRefused(CnaSurfaceFormatUsage.TextureStorage));
            Assert.Equal(color.Supported, color.Supported & color.Known);

            // A usage nobody classified is neither supported nor refused. Both answers being false
            // is the distinction this type exists to preserve, and a projection that collapsed the
            // two masks into one boolean would make it impossible to state.
            foreach (SurfaceFormat format in Enum.GetValues<SurfaceFormat>())
            {
                CnaSurfaceFormatSupport support = device.GetCnaSurfaceFormatSupport(format);
                Assert.Equal(support.Supported, support.Supported & support.Known);

                foreach (CnaSurfaceFormatUsage usage in Enum.GetValues<CnaSurfaceFormatUsage>())
                {
                    if (usage == CnaSurfaceFormatUsage.None)
                    {
                        continue;
                    }

                    Assert.False(
                        support.IsSupported(usage) && support.IsRefused(usage),
                        $"{format}/{usage} cannot be both supported and refused.");
                }
            }
        });
    }
}
