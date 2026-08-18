using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>TextureCollection</c>: the indexer view over one of a device's two
/// texture-binding collections (<c>GraphicsDevice.Textures</c> for the pixel shader,
/// <c>VertexTextures</c> for the vertex shader).
///
/// Unlike <see cref="SamplerStateCollection"/> -- which is deliberately stateless and reads every
/// value back from native -- this one *does* keep the managed reference it was handed per slot,
/// and its getter returns that rather than re-reading native. That is not an inconsistency; the
/// two collections differ in what native can actually give back:
///
/// <list type="bullet">
/// <item>A sampler slot reads back as a complete <c>CNA_SamplerState</c> value struct, so the
/// managed object can be faithfully rebuilt from it.</item>
/// <item>A texture slot reads back as <c>CNA_TextureSlotInfo</c>, which carries only a bare
/// handle -- and the C API explicitly documents that it may report a slot as bound "with an
/// invalid handle, because no C resource owns that texture" (<c>graphics_device.h:602-608</c>).
/// There is no way to map a raw handle back to the managed <see cref="Texture"/> wrapper that owns
/// it, so re-reading could not return the object the caller set, and for an
/// engine-bound-but-unowned texture could not return anything at all.</item>
/// </list>
///
/// So the getter answers "what did I last bind here", which is what a caller can actually use.
/// Writes always go through to native immediately, so the binding itself is never stale.
/// </summary>
public class TextureCollection
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CnaShaderStage _stage;
    private readonly Texture?[] _bound = new Texture?[CnaTextureCollectionLimits.MaxTextures];

    protected internal TextureCollection(GraphicsDevice graphicsDevice, bool vertexStage)
    {
        _graphicsDevice = graphicsDevice;
        _stage = vertexStage ? CnaShaderStage.Vertex : CnaShaderStage.Pixel;
    }

    /// <summary>Matches <c>CNA_TEXTURE_COLLECTION_MAX_TEXTURES</c>
    /// (<c>graphics_device.h:584</c>). See <see cref="SamplerStateCollection.Count"/> for why this
    /// is exposed at all when real XNA's own collection does not.</summary>
    public int Count => CnaTextureCollectionLimits.MaxTextures;

    public Texture? this[int index]
    {
        get
        {
            ValidateSlot(index);
            return _bound[index];
        }
        set
        {
            ValidateSlot(index);
            CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
            CnaResult result = Native.cna_graphics_device_set_texture(
                _graphicsDevice.ResolveNativeDeviceHandle(), _stage, (uint)index, handle);
            CnaException.ThrowIfFailed(result, nameof(TextureCollection));
            _bound[index] = value;
        }
    }

    private static void ValidateSlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, CnaTextureCollectionLimits.MaxTextures);
    }
}
