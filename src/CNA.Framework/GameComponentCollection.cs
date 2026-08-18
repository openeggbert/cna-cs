using System.Collections;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GameComponentCollection</c>: the components a <see cref="Game"/> updates
/// and draws each frame.
///
/// A view over the *native* collection, not a managed list. The native game owns the collection
/// and iterates it itself, so a managed list would be a second, ignored registry -- every mutation
/// here goes straight through to <c>cna_game_components_add</c>/<c>_remove</c>/<c>_clear</c>.
///
/// The one thing kept managed-side is the mapping from a native component handle back to its
/// managed <see cref="GameComponent"/>: native reports handles, and there is no way to recover the
/// wrapper from one (the same limitation <see cref="Graphics.TextureCollection"/> documents). So
/// the indexer answers from a handle-keyed dictionary of components this collection has seen,
/// which is exact for anything added through it.
/// </summary>
public class GameComponentCollection : ICollection<GameComponent>
{
    private readonly Game _game;
    private readonly Dictionary<ulong, GameComponent> _known = [];

    internal GameComponentCollection(Game game)
    {
        _game = game;
    }

    private CnaHandle GameHandle => new(_game.NativeHandle);

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_game_components_get_count(GameHandle, out ulong count);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public bool IsReadOnly => false;

    public GameComponent this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            CnaResult result = Native.cna_game_components_get_at(GameHandle, (ulong)index, out CnaHandle component);
            CnaException.ThrowIfFailed(result, nameof(GameComponentCollection));

            return _known.TryGetValue(component.Value, out GameComponent? known)
                ? known
                : throw new InvalidOperationException(
                    "The native game holds a component at this index that was not added through this collection, " +
                    "so its managed GameComponent cannot be recovered.");
        }
    }

    public void Add(GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CnaResult result = Native.cna_game_components_add(GameHandle, item.NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Add));
        _known[item.NativeHandle.Value] = item;
        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    public bool Remove(GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CnaResult result = Native.cna_game_components_remove(GameHandle, item.NativeHandle, out byte removed);
        CnaException.ThrowIfFailed(result, nameof(Remove));

        if (removed != 0)
        {
            _known.Remove(item.NativeHandle.Value);
            ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));
        }

        return removed != 0;
    }

    public void Clear()
    {
        CnaResult result = Native.cna_game_components_clear(GameHandle);
        CnaException.ThrowIfFailed(result, nameof(Clear));
        _known.Clear();
    }

    public bool Contains(GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CnaResult result = Native.cna_game_components_contains(GameHandle, item.NativeHandle, out byte contains);
        CnaException.ThrowIfFailed(result, nameof(Contains));
        return contains != 0;
    }

    public int IndexOf(GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CnaResult result = Native.cna_game_components_index_of(GameHandle, item.NativeHandle, out int index);
        CnaException.ThrowIfFailed(result, nameof(IndexOf));
        return index;
    }

    public void CopyTo(GameComponent[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        int count = Count;
        for (int i = 0; i < count; i++)
        {
            array[arrayIndex + i] = this[i];
        }
    }

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentAdded;

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentRemoved;

    public IEnumerator<GameComponent> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Matches real XNA's <c>GameComponentCollectionEventArgs</c>.</summary>
public class GameComponentCollectionEventArgs : EventArgs
{
    public GameComponentCollectionEventArgs(IGameComponent gameComponent)
    {
        GameComponent = gameComponent;
    }

    public IGameComponent GameComponent { get; }
}
