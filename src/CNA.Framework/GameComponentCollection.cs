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

    /// <summary>Handle-to-wrapper map. Deliberately *not* a membership set: the C API accepts the
    /// same component twice ("The canonical collection accepts the same component twice, and this
    /// route does too"), so an entry must survive one removal, and entries are kept after
    /// <see cref="Remove"/>/<see cref="Clear"/> so a re-added component is still resolvable. An
    /// earlier revision deleted on removal and made the indexer claim a twice-added component "was
    /// not added through this collection" -- found by a code-review pass. Native remains the sole
    /// authority on membership; this only answers "which managed object is this handle".</summary>
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

        // Registered BEFORE the native call: the C API initializes a component as it is added once
        // the game is running, so the initialize callback can run inside cna_game_components_add
        // and a component that inspects Game.Components from its own Initialize would otherwise
        // find itself unresolvable. Rolled back if the add fails.
        _known[item.NativeHandle.Value] = item;

        CnaResult result = Native.cna_game_components_add(GameHandle, item.NativeHandle);
        if (result.IsFailure())
        {
            _known.Remove(item.NativeHandle.Value);
            CnaException.ThrowIfFailed(result, nameof(Add));
        }

        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    /// <summary>
    /// Inserts at a position rather than appending. Real XNA's collection is a
    /// <c>Collection&lt;IGameComponent&gt;</c>, so it has this through <see cref="System.Collections.IList"/>.
    ///
    /// Registers before the native call for the reason <see cref="Add"/> documents at length: the
    /// component's own <c>Initialize</c> can run inside the insert, and a component that inspects
    /// <c>Game.Components</c> from there would otherwise not find itself.
    /// </summary>
    public void Insert(int index, GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _known[item.NativeHandle.Value] = item;

        // The C index is a uint64_t. This passed a signed 32-bit value until B2's generated
        // prototype probe compared the two: the argument register's upper half was never written,
        // which happens to work on x86-64 because a 32-bit move zero-extends, and is not something
        // to rely on. The negative case is refused above, so the widening is total.
        CnaResult result = Native.cna_game_components_insert(GameHandle, (ulong)index, item.NativeHandle);
        if (result.IsFailure())
        {
            _known.Remove(item.NativeHandle.Value);
            CnaException.ThrowIfFailed(result, nameof(Insert));
        }

        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    /// <summary>Removes the component at a position. Real XNA's collection is a
    /// <c>Collection&lt;IGameComponent&gt;</c>, so it has this through
    /// <see cref="System.Collections.IList"/>. Resolves the component first so
    /// <see cref="ComponentRemoved"/> can name it, which is what a handler needs.</summary>
    public void RemoveAt(int index)
    {
        GameComponent component = this[index];
        Remove(component);
    }

    public bool Remove(GameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CnaResult result = Native.cna_game_components_remove(GameHandle, item.NativeHandle, out byte removed);
        CnaException.ThrowIfFailed(result, nameof(Remove));

        if (removed != 0)
        {
            // _known deliberately keeps its entry -- see that field's doc comment.
            ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));
        }

        return removed != 0;
    }

    /// <summary>Raises <see cref="ComponentRemoved"/> once per component, matching XNA's
    /// <c>ClearItems</c> and the C API ("Every removal raises the component-removed event, one per
    /// component"). An earlier revision raised none, so per-component teardown wired to that event
    /// never ran on <see cref="Clear"/>.</summary>
    public void Clear()
    {
        GameComponent[] removed = ComponentRemoved is null ? [] : this.ToArray();

        CnaResult result = Native.cna_game_components_clear(GameHandle);
        CnaException.ThrowIfFailed(result, nameof(Clear));

        foreach (GameComponent component in removed)
        {
            ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(component));
        }
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

    /// <summary>Validates before copying anything -- <c>ICollection&lt;T&gt;.CopyTo</c> requires
    /// the exception to be thrown before any element is written, and an earlier revision copied
    /// what fitted and then threw, leaving the caller's array half-populated.</summary>
    public void CopyTo(GameComponent[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        int count = Count;
        if (arrayIndex > array.Length - count)
        {
            throw new ArgumentException(
                "The destination array is too small for the components in this collection.", nameof(array));
        }

        for (int i = 0; i < count; i++)
        {
            array[arrayIndex + i] = this[i];
        }
    }

    /// <summary>
    /// Disposes every component this collection has ever been given, releasing its native handle
    /// and its GC root.
    ///
    /// Called by <see cref="Game.Dispose(bool)"/> because the C API requires it: "Every component
    /// must be released before its game is destroyed." Nothing else can do it -- a component holds
    /// a strong <see cref="System.Runtime.InteropServices.GCHandle"/> to itself for the native
    /// callback context, so it is permanently reachable and no finalizer can ever run. Without
    /// this, the standard XNA pattern <c>Components.Add(new MyComponent(this))</c> followed by
    /// <c>game.Dispose()</c> violated that precondition and leaked both the native component and
    /// the managed object for the process lifetime. Found by a code-review pass.
    /// </summary>
    internal void DisposeAllKnownComponents()
    {
        foreach (GameComponent component in _known.Values.ToArray())
        {
            component.Dispose();
        }

        _known.Clear();
    }

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentAdded;

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentRemoved;

    /// <summary>
    /// Snapshots the collection before yielding. The native collection is the authority and can be
    /// mutated during enumeration -- including by a component's own callbacks -- and an earlier
    /// revision indexed it live against a count read once, so removing during a <c>foreach</c>
    /// silently skipped the next component and then failed with a native out-of-range error
    /// instead of the <see cref="InvalidOperationException"/> a <c>foreach</c> caller expects.
    ///
    /// A snapshot rather than a version counter, and the reason needed correcting: this used to say
    /// "native owns the list and does not report modifications", which
    /// <c>cna_game_components_subscribe_added</c>/<c>_removed</c> (<c>runtime_components.h:546</c>,
    /// <c>:561</c>) contradict. Those exist, and a header audit found them. A version counter built
    /// on them would still be the wrong shape here: a component's own callback can mutate the
    /// collection mid-enumeration, and turning that into an <see cref="InvalidOperationException"/>
    /// would make a legitimate pattern throw. Enumerating a copy leaves it simply unobserved by the
    /// loop, which is what a caller can actually work with.
    /// </summary>
    public IEnumerator<GameComponent> GetEnumerator()
    {
        int count = Count;
        var snapshot = new GameComponent[count];
        for (int i = 0; i < count; i++)
        {
            snapshot[i] = this[i];
        }

        return ((IEnumerable<GameComponent>)snapshot).GetEnumerator();
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
