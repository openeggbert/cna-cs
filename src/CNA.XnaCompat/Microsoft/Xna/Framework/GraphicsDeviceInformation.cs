namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GraphicsDeviceInformation</c>. A pure subclass -- only the
/// members whose types differ per namespace are re-typed.</summary>
public class GraphicsDeviceInformation : CNA.GraphicsDeviceInformation
{
    public new Graphics.GraphicsProfile GraphicsProfile
    {
        get => (Graphics.GraphicsProfile)(int)base.GraphicsProfile;
        set => base.GraphicsProfile = (CNA.Graphics.GraphicsProfile)(int)value;
    }

    public new Graphics.PresentationParameters PresentationParameters
    {
        get => (Graphics.PresentationParameters)base.PresentationParameters;
        set => base.PresentationParameters = value;
    }

    public new Graphics.GraphicsAdapter? Adapter
    {
        get => base.Adapter as Graphics.GraphicsAdapter;
        set => base.Adapter = value;
    }
}

/// <summary>XNA 4.0-compatible <c>PreparingDeviceSettingsEventArgs</c>. A pure subclass; only
/// <see cref="GraphicsDeviceInformation"/> needs re-typing.</summary>
public class PreparingDeviceSettingsEventArgs : CNA.PreparingDeviceSettingsEventArgs
{
    public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation)
        : base(graphicsDeviceInformation)
    {
    }

    public new GraphicsDeviceInformation GraphicsDeviceInformation => (GraphicsDeviceInformation)base.GraphicsDeviceInformation;
}
