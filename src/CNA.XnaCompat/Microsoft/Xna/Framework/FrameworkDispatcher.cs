namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>FrameworkDispatcher</c>. A thin forwarding facade -- static
/// classes cannot be subclassed, the same reason
/// <see cref="Microsoft.Xna.Framework.Input.Keyboard"/> forwards rather than inherits.</summary>
public static class FrameworkDispatcher
{
    public static void Update() => CNA.FrameworkDispatcher.Update();
}
