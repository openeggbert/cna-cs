namespace CNA.Graphics;

/// <summary>
/// How a back buffer is fitted into the window it is presented into.
///
/// Not an XNA identity. XNA 4.0 stretches the back buffer to the client area and gives a game no
/// say in it, which is why a fixed-aspect XNA game letterboxes by hand. CNA's renderers do this in
/// the presentation step instead, so the choice is real here.
/// </summary>
public enum PresentationMode : uint
{
    /// <summary>Preserve the aspect ratio, filling the remainder with bars.</summary>
    Letterbox = 0,

    /// <summary>Preserve the aspect ratio, cropping whatever falls outside the window.</summary>
    Overscan = 1,

    /// <summary>Fill the window, changing the aspect ratio if it differs. XNA's own behaviour.</summary>
    Stretch = 2,

    /// <summary>Present at the back buffer's own size, without scaling.</summary>
    NativeBackBuffer = 3,

    /// <summary>Hold the height and let the width follow the window's aspect ratio.</summary>
    FixedHeightDynamicWidth = 4,
}
