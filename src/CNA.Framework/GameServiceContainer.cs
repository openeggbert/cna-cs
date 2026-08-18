namespace CNA;

/// <summary>
/// Matches real XNA's <c>GameServiceContainer</c>: the <see cref="IServiceProvider"/> a
/// <see cref="Game"/> exposes so components can find shared services (classically
/// <c>IGraphicsDeviceService</c>) without depending on each other directly.
///
/// Pure managed logic with no native counterpart -- the C API's own
/// <c>cna_game_services_contains_ext</c>/<c>_remove_ext</c> are <c>_ext</c> test hooks over the
/// native game's own service table, not a general container, and real XNA's semantics (keyed by
/// <see cref="Type"/>, one instance per type, no inheritance lookup) are entirely expressible
/// here. Being managed also makes it directly testable, unlike the native-backed component types.
/// </summary>
public class GameServiceContainer : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = [];

    /// <summary>Throws when <paramref name="provider"/> is not actually an instance of
    /// <paramref name="type"/>, matching real XNA -- registering a mismatched pair would otherwise
    /// only fail much later, at the cast in whatever consumed
    /// <see cref="GetService(Type)"/>.</summary>
    public void AddService(Type type, object provider)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(provider);

        if (!type.IsInstanceOfType(provider))
        {
            throw new ArgumentException($"The service provider is not an instance of {type.FullName}.", nameof(provider));
        }

        _services.Add(type, provider);
    }

    public void AddService<T>(T provider) where T : notnull => AddService(typeof(T), provider);

    /// <summary>Exact-type lookup only, matching real XNA: a service registered as a concrete
    /// class is not found by asking for an interface it happens to implement. Returns
    /// <see langword="null"/> rather than throwing when nothing is registered, which is the
    /// <see cref="IServiceProvider"/> contract.</summary>
    public object? GetService(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _services.GetValueOrDefault(type);
    }

    public T? GetService<T>() where T : class => GetService(typeof(T)) as T;

    public void RemoveService(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _services.Remove(type);
    }
}
