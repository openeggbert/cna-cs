using CNA.Graphics;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GraphicsDeviceInformation</c>: the settings a
/// <see cref="GraphicsDeviceManager"/> is about to create a device with, offered to game code for
/// inspection or adjustment before creation.
///
/// A plain mutable value holder, deliberately not native-backed even though
/// <c>cna_graphics_device_information_init</c>/<c>_clone</c> exist: the C API's own manager takes
/// its settings from the manager's properties, and this type's whole purpose in XNA is to be a
/// *proposal* that game code can edit before it is applied. Binding it would make the object a
/// live view of something already decided, which is the opposite of what it is for.
/// </summary>
public class GraphicsDeviceInformation
{
    public GraphicsProfile GraphicsProfile { get; set; }

    public PresentationParameters PresentationParameters { get; set; } = new();

    /// <summary>The adapter the device would be created on. Nullable because a proposal built
    /// before any device exists has no adapter to name yet -- real XNA populates it from the
    /// default adapter, which this project cannot enumerate without a device (see
    /// <see cref="GraphicsAdapter"/>'s own doc comment).</summary>
    public GraphicsAdapter? Adapter { get; set; }

    public GraphicsDeviceInformation Clone() => new()
    {
        GraphicsProfile = GraphicsProfile,
        PresentationParameters = PresentationParameters.Clone(),
        Adapter = Adapter,
    };
}

/// <summary>Matches real XNA's <c>PreparingDeviceSettingsEventArgs</c>: carries the
/// <see cref="GraphicsDeviceInformation"/> a game may edit in a
/// <c>GraphicsDeviceManager.PreparingDeviceSettings</c> handler.</summary>
public class PreparingDeviceSettingsEventArgs : EventArgs
{
    public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation)
    {
        ArgumentNullException.ThrowIfNull(graphicsDeviceInformation);
        GraphicsDeviceInformation = graphicsDeviceInformation;
    }

    public GraphicsDeviceInformation GraphicsDeviceInformation { get; }
}
