using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// The single-draw-call form of <c>SpriteBatch</c>. Command buffering + a batched
/// <c>cna_sprite_batch_draw_many</c> call, per ../../cnabinding/analysis_binding.md §22, is
/// Phase 5 (plan.md) -- not implemented yet.
/// </summary>
public class SpriteBatch : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public SpriteBatch(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_sprite_batch_create(new CnaHandle(graphicsDevice.NativeHandleValue), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(SpriteBatch));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_sprite_batch_release(new CnaHandle(h)));
    }

    private nint NativeHandleValue => _handle.DangerousGetHandle();

    public void Begin()
    {
        CnaResult result = Native.cna_sprite_batch_begin(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Begin));
    }

    public void Draw(Texture2D texture, Vector2 position, Color color)
    {
        ArgumentNullException.ThrowIfNull(texture);

        CnaResult result = Native.cna_sprite_batch_draw(
            new CnaHandle(NativeHandleValue),
            new CnaHandle(texture.NativeHandleValue),
            position.ToNative(),
            color.ToNative());
        CnaException.ThrowIfFailed(result, nameof(Draw));
    }

    public void End()
    {
        CnaResult result = Native.cna_sprite_batch_end(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(End));
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
