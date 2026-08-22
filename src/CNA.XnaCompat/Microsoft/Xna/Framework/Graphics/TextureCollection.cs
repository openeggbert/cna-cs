namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>TextureCollection</c>. Holds no state of its own beyond what the
/// base class already tracks -- this only re-types the indexer so callers see this namespace's
/// <see cref="Texture"/>. The downcast is safe because the only way a value reaches the base
/// collection is through this indexer's own setter, which takes the compat type.</summary>
public class TextureCollection : CNA.Graphics.TextureCollection
{
    internal TextureCollection(GraphicsDevice graphicsDevice, bool vertexStage)
        : base(graphicsDevice.Framework, vertexStage)
    {
    }

    public new Texture? this[int index]
    {
        get => Texture.FromFramework(base[index]);
        set => base[index] = value?.FrameworkTexture;
    }
}
