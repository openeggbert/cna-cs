namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>IGraphicsDeviceService</c>: how a component finds the device without
/// depending on <c>GraphicsDeviceManager</c>'s concrete type.
///
/// A distinct interface from <see cref="CNA.IGraphicsDeviceService"/> rather than an alias, because
/// <see cref="GraphicsDevice"/> differs per namespace. <c>GraphicsDeviceManager</c> implements
/// both, so a component looking up either contract in <c>Game.Services</c> finds it.
///
/// It lived in <c>Microsoft.Xna.Framework</c> until the WP16 re-audit. Real XNA puts it here, in
/// <c>Microsoft.Xna.Framework.Graphics</c> -- unlike <c>IGraphicsDeviceManager</c> and
/// <c>GraphicsDeviceInformation</c>, which really are in the root namespace. Ported source with a
/// <c>using Microsoft.Xna.Framework.Graphics;</c> would not have resolved it.
/// </summary>
public interface IGraphicsDeviceService
{
    GraphicsDevice GraphicsDevice { get; }

    event EventHandler<EventArgs>? DeviceCreated;

    event EventHandler<EventArgs>? DeviceDisposing;

    event EventHandler<EventArgs>? DeviceReset;

    event EventHandler<EventArgs>? DeviceResetting;
}
