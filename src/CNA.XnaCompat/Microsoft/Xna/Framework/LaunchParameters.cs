namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>LaunchParameters</c>. A pure subclass of
/// <see cref="CNA.LaunchParameters"/>; both are ultimately
/// <see cref="Dictionary{TKey,TValue}"/> of <see cref="string"/>, with nothing to re-type.</summary>
public class LaunchParameters : CNA.LaunchParameters
{
    public LaunchParameters()
    {
    }

    public LaunchParameters(IEnumerable<string> arguments)
        : base(arguments)
    {
    }

    /// <summary>Re-wraps an already-materialised set, which is how the compat <c>Game</c> re-types
    /// the base's dictionary without asking native for it twice.</summary>
    public LaunchParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        : base(parameters)
    {
    }
}
