using CNA.Interop;

namespace CNA.Input.Touch;

/// <summary>Matches real XNA's <c>TouchPanelCapabilities</c>. Sourced from
/// <c>cna_touch_capabilities_init</c>, which -- unlike most <c>_init</c> functions in this ABI --
/// reports the *live* device state rather than a preset, so this is a real query and not a
/// constant.</summary>
public readonly struct TouchPanelCapabilities
{
    internal TouchPanelCapabilities(bool isConnected, int maximumTouchCount)
    {
        IsConnected = isConnected;
        MaximumTouchCount = maximumTouchCount;
    }

    public bool IsConnected { get; }

    /// <summary>XNA's documented maximum, four when a device is connected. Note this is lower than
    /// <c>CNA_TOUCH_MAX_TOUCHES</c> (eight), the capacity of the underlying snapshot -- see
    /// <c>CnaTouchState.MaxTouches</c>'s own doc comment.</summary>
    public int MaximumTouchCount { get; }

    internal static TouchPanelCapabilities FromNative(in CnaTouchCapabilities native) =>
        new(native.IsConnected != 0, (int)native.MaximumTouchCount);
}
