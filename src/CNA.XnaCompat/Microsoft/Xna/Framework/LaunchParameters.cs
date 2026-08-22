namespace Microsoft.Xna.Framework;

public class LaunchParameters : Dictionary<string, string>
{
    public LaunchParameters()
        : this(new CNA.LaunchParameters())
    {
    }

    internal LaunchParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        : base(parameters)
    {
    }
}
