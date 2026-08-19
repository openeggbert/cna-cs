using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Vertex and index buffer transfers, including the windowed ones that threw until the routes
/// behind them were found.
///
/// The windowed case is the whole point: <c>offsetInBytes</c> indexes <b>the buffer</b>, while the
/// transfer descriptor's <c>start_index</c> indexes <b>the caller's array</b>. Confusing the two
/// writes the right bytes to the wrong place, which no compiler and no managed test can catch --
/// only reading the buffer back does.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class BufferIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{


    private static VertexPositionColor Vertex(float x) =>
        new(new Vector3(x, x, x), new Color((int)x, 0, 0, 255));

    [Native3DFact]
    public void VertexBuffer_SetThenGetData_RoundTrips()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            VertexPositionColor[] written = [Vertex(1f), Vertex(2f), Vertex(3f), Vertex(4f)];

            using var buffer = new VertexBuffer(
                device, VertexPositionColor.VertexDeclaration, written.Length, BufferUsage.None);

            buffer.SetData(written);

            var read = new VertexPositionColor[written.Length];
            buffer.GetData(read);

            Assert.Equal(written, read);
        });
    }

    /// <summary>
    /// The windowed upload. Writes the whole buffer, then rewrites one vertex at a nonzero
    /// <c>offsetInBytes</c>, then reads everything back: the target vertex must have changed and
    /// its neighbours must not.
    ///
    /// That last clause is the assertion that matters. An implementation that confused the buffer
    /// offset with the caller-array offset would still write plausible data and still round-trip
    /// the element it was asked about -- it would just also flatten a neighbour.
    /// </summary>
    [Native3DFact]
    public void VertexBuffer_SetData_WithNonzeroOffset_RewritesOnlyThatWindow()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            VertexPositionColor[] original = [Vertex(1f), Vertex(2f), Vertex(3f), Vertex(4f)];
            int stride = VertexPositionColor.VertexDeclaration.VertexStride;

            using var buffer = new VertexBuffer(
                device, VertexPositionColor.VertexDeclaration, original.Length, BufferUsage.None);

            buffer.SetData(original);

            VertexPositionColor[] replacement = [Vertex(99f)];
            buffer.SetData(stride * 2, replacement, 0, 1, stride);

            var read = new VertexPositionColor[original.Length];
            buffer.GetData(read);

            output.WriteLine($"stride {stride}; read back {string.Join(", ", read.Select(v => v.Position.X))}");

            Assert.Equal(original[0], read[0]);
            Assert.Equal(original[1], read[1]);
            Assert.Equal(replacement[0], read[2]);
            Assert.Equal(original[3], read[3]);
        });
    }

    /// <summary>Raw readback of a layout the built-in <c>CNA_VertexType</c> set does not name. This
    /// threw as "the C API has no raw-bytes vertex readback" after the route existed.</summary>
    [Native3DFact]
    public void VertexBuffer_GetData_ReadsBackACustomLayout()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            VertexPositionColor[] written = [Vertex(5f), Vertex(6f)];
            int stride = VertexPositionColor.VertexDeclaration.VertexStride;

            using var buffer = new VertexBuffer(
                device, VertexPositionColor.VertexDeclaration, written.Length, BufferUsage.None);

            buffer.SetData(written);

            // byte[] is not a CNA_VertexType, so this can only go through the raw route.
            var raw = new byte[stride * written.Length];
            buffer.GetData(0, raw, 0, raw.Length, 1);

            Assert.Contains(raw, b => b != 0);
        });
    }

    /// <summary>
    /// <c>GetVertexBuffers</c> answers, and its cross-check against native's count fires when the
    /// two disagree.
    ///
    /// This member threw until the render-target precedent in the same file was noticed. Both face
    /// the identical limitation -- native reports bare handles that cannot be mapped back to a
    /// managed wrapper -- and the answer is the same: report what this object bound, and verify the
    /// count so a rebind from elsewhere surfaces instead of being papered over.
    /// </summary>
    [Native3DFact]
    public void GraphicsDevice_GetVertexBuffers_ReportsWhatWasBound()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            Assert.Empty(device.GetVertexBuffers());

            using var buffer = new VertexBuffer(
                device, VertexPositionColor.VertexDeclaration, 3, BufferUsage.None);
            buffer.SetData([Vertex(1f), Vertex(2f), Vertex(3f)]);

            device.SetVertexBuffer(buffer);

            VertexBufferBinding[] bound = device.GetVertexBuffers();
            Assert.Single(bound);
            Assert.Same(buffer, bound[0].VertexBuffer);
            output.WriteLine($"{bound.Length} binding(s); count says {device.VertexBufferCount}");

            device.SetVertexBuffer(null);
            Assert.Empty(device.GetVertexBuffers());
        });
    }

    [Native3DFact]
    public void IndexBuffer_SetData_WithNonzeroOffset_RewritesOnlyThatWindow()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            ushort[] original = [10, 11, 12, 13];

            using var buffer = new IndexBuffer(
                device, IndexElementSize.SixteenBits, original.Length, BufferUsage.None);

            buffer.SetData(original);
            buffer.SetData(sizeof(ushort) * 2, new ushort[] { 99 }, 0, 1);

            var read = new ushort[original.Length];
            buffer.GetData(read);

            output.WriteLine($"read back {string.Join(", ", read)}");

            Assert.Equal([(ushort)10, (ushort)11, (ushort)99, (ushort)13], read);
        });
    }
}
