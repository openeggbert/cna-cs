namespace Microsoft.Xna.Framework;

/// <summary>
/// The proposed device settings supplied to <see cref="GraphicsDeviceManager.PreparingDeviceSettings"/>.
/// This is an XNA-owned mutable proposal, not a subtype of CNA's proposal object.
/// </summary>
public class GraphicsDeviceInformation
{
    public Graphics.GraphicsProfile GraphicsProfile { get; set; }

    public Graphics.PresentationParameters PresentationParameters { get; set; } = new();

    public Graphics.GraphicsAdapter? Adapter { get; set; }

    public GraphicsDeviceInformation Clone() => new()
    {
        GraphicsProfile = GraphicsProfile,
        PresentationParameters = PresentationParameters.Clone(),
        Adapter = Adapter,
    };

    public override bool Equals(object? obj)
    {
        if (obj is not GraphicsDeviceInformation other ||
            GraphicsProfile != other.GraphicsProfile ||
            !Equals(Adapter, other.Adapter))
        {
            return false;
        }

        Graphics.PresentationParameters left = PresentationParameters;
        Graphics.PresentationParameters right = other.PresentationParameters;
        return left.BackBufferWidth == right.BackBufferWidth &&
            left.BackBufferHeight == right.BackBufferHeight &&
            left.BackBufferFormat == right.BackBufferFormat &&
            left.DepthStencilFormat == right.DepthStencilFormat &&
            left.MultiSampleCount == right.MultiSampleCount &&
            left.DisplayOrientation == right.DisplayOrientation &&
            left.PresentationInterval == right.PresentationInterval &&
            left.RenderTargetUsage == right.RenderTargetUsage &&
            left.DeviceWindowHandle == right.DeviceWindowHandle &&
            left.IsFullScreen == right.IsFullScreen;
    }

    public override int GetHashCode()
    {
        Graphics.PresentationParameters parameters = PresentationParameters;
        return GraphicsProfile.GetHashCode() ^ (Adapter?.GetHashCode() ?? 0) ^
            parameters.BackBufferWidth.GetHashCode() ^ parameters.BackBufferHeight.GetHashCode() ^
            parameters.BackBufferFormat.GetHashCode() ^ parameters.DepthStencilFormat.GetHashCode() ^
            parameters.MultiSampleCount.GetHashCode() ^ parameters.DisplayOrientation.GetHashCode() ^
            parameters.PresentationInterval.GetHashCode() ^ parameters.RenderTargetUsage.GetHashCode() ^
            parameters.DeviceWindowHandle.GetHashCode() ^ parameters.IsFullScreen.GetHashCode();
    }

    internal static GraphicsDeviceInformation FromFramework(CNA.GraphicsDeviceInformation information) => new()
    {
        GraphicsProfile = (Graphics.GraphicsProfile)(int)information.GraphicsProfile,
        PresentationParameters = new Graphics.PresentationParameters(information.PresentationParameters),
        Adapter = information.Adapter is null ? null : Graphics.GraphicsAdapter.FromFramework(information.Adapter),
    };

    internal void CopyTo(CNA.GraphicsDeviceInformation information)
    {
        information.GraphicsProfile = (CNA.Graphics.GraphicsProfile)(int)GraphicsProfile;
        information.PresentationParameters = PresentationParameters.Framework;
        information.Adapter = Adapter?.Framework;
    }
}

/// <summary>XNA event arguments carrying the mutable device proposal.</summary>
public class PreparingDeviceSettingsEventArgs : EventArgs
{
    public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation)
    {
        ArgumentNullException.ThrowIfNull(graphicsDeviceInformation);
        GraphicsDeviceInformation = graphicsDeviceInformation;
    }

    public GraphicsDeviceInformation GraphicsDeviceInformation { get; }
}
