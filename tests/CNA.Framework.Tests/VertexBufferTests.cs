using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// VertexBuffer/IndexBuffer's constructors call into native code immediately (unlike
/// SoundEffect's, which validates everything before the native call) -- there is no way to reach
/// even a successfully-constructed instance without a real cna-native, so SetData/GetData cannot
/// be tested here at all, and only argument-validation failures that throw *before* the native
/// call are testable. Uses the same "GraphicsDevice wrapping an invalid (zero) native handle"
/// trick SpriteFontTests uses for a dummy Texture2D, so these tests never touch native code.
/// </summary>
public class VertexBufferTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeGameHandleValue: 0);

    [Fact]
    public void Constructor_NullGraphicsDevice_Throws()
    {
        var declaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));

        Assert.Throws<ArgumentNullException>(() => new VertexBuffer(null!, declaration, 4, BufferUsage.None));
    }

    [Fact]
    public void Constructor_NullVertexDeclaration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VertexBuffer(CreateDummyDevice(), (VertexDeclaration)null!, 4, BufferUsage.None));
    }

    [Fact]
    public void Constructor_InvalidVertexType_Throws()
    {
        // VertexDeclaration.FromType(vertexType) runs in the constructor initializer, before the
        // chained constructor's own body (and thus before any native call), so this is testable
        // the same way the null-declaration case above is.
        Assert.Throws<ArgumentException>(() => new VertexBuffer(CreateDummyDevice(), typeof(int), 4, BufferUsage.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveVertexCount_Throws(int vertexCount)
    {
        var declaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => new VertexBuffer(CreateDummyDevice(), declaration, vertexCount, BufferUsage.None));
    }
}
