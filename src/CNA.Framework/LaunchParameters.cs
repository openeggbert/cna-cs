namespace CNA;

/// <summary>Matches real XNA's <c>LaunchParameters</c>: the command-line arguments a game was
/// started with, parsed into key/value pairs. A plain <see cref="Dictionary{TKey,TValue}"/>
/// subclass, exactly as in real XNA -- there is nothing native about it.</summary>
public class LaunchParameters : Dictionary<string, string>
{
    public LaunchParameters()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>Parses <c>-key:value</c> / <c>/key:value</c> / <c>-flag</c> style arguments, the
    /// form real XNA's own launch-parameter parsing accepts. An argument with no <c>:</c> becomes
    /// a key with an empty value; one that matches nothing recognisable is
    /// skipped.</summary>
    public LaunchParameters(IEnumerable<string> arguments)
        : this()
    {
        ArgumentNullException.ThrowIfNull(arguments);

        foreach (string argument in arguments)
        {
            if (string.IsNullOrEmpty(argument) || (argument[0] != '-' && argument[0] != '/'))
            {
                continue;
            }

            string body = argument[1..];
            int separator = body.IndexOf(':', StringComparison.Ordinal);

            if (separator < 0)
            {
                this[body] = string.Empty;
            }
            else
            {
                this[body[..separator]] = body[(separator + 1)..];
            }
        }
    }
}
