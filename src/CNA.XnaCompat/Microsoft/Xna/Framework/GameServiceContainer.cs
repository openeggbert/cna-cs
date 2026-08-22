namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA's service registry. This is deliberately a managed facade rather than a subtype of the
/// CNA registry: an XNA <see cref="Game"/> must not acquire CNA's public inheritance graph merely
/// because its implementation needs a native game.
/// </summary>
public class GameServiceContainer : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = [];

    public void AddService(Type type, object provider)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(provider);

        if (!type.IsInstanceOfType(provider))
        {
            throw new ArgumentException(
                $"The service provider is not an instance of {type.FullName}.",
                nameof(provider));
        }

        _services.Add(type, provider);
    }

    public object? GetService(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _services.GetValueOrDefault(type);
    }

    public void RemoveService(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _services.Remove(type);
    }
}
