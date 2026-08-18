namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>IGraphicsDeviceManager</c>. Every member involves only
/// <see cref="bool"/> and <see langword="void"/>, so unlike
/// <see cref="Graphics.IGraphicsDeviceService"/> this could have been inherited -- it is declared
/// here anyway so the type name resolves in this namespace, and
/// <see cref="CNA.IGraphicsDeviceManager"/> is implemented alongside it.</summary>
public interface IGraphicsDeviceManager
{
    bool BeginDraw();

    void CreateDevice();

    void EndDraw();
}
