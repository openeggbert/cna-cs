namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>XNA 4.0-compatible <c>TouchPanelCapabilities</c>.</summary>
public readonly struct TouchPanelCapabilities
{
    internal TouchPanelCapabilities(bool isConnected, int maximumTouchCount)
    {
        IsConnected = isConnected;
        MaximumTouchCount = maximumTouchCount;
    }

    public bool IsConnected { get; }

    public int MaximumTouchCount { get; }

    internal static TouchPanelCapabilities FromFramework(CNA.Input.Touch.TouchPanelCapabilities source) =>
        new(source.IsConnected, source.MaximumTouchCount);
}
