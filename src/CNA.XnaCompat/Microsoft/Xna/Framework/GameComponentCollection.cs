namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GameComponentCollection</c>. A thin re-typing wrapper rather
/// than a subclass, because <see cref="CNA.GameComponentCollection"/>'s only constructor is
/// internal (it is a view the game creates over its own native collection). Every member forwards;
/// the element type is the base <see cref="CNA.GameComponent"/>, which the compat components
/// derive from, so both namespaces' components go in.</summary>
public class GameComponentCollection : System.Collections.Generic.ICollection<CNA.GameComponent>
{
    private readonly CNA.GameComponentCollection _components;

    internal GameComponentCollection(CNA.GameComponentCollection components)
    {
        _components = components;
    }

    public CNA.GameComponent this[int index] => _components[index];

    public int Count => _components.Count;

    public bool IsReadOnly => _components.IsReadOnly;

    public void Add(CNA.GameComponent item) => _components.Add(item);

    public bool Remove(CNA.GameComponent item) => _components.Remove(item);

    public void Clear() => _components.Clear();

    public bool Contains(CNA.GameComponent item) => _components.Contains(item);

    public int IndexOf(CNA.GameComponent item) => _components.IndexOf(item);

    public void CopyTo(CNA.GameComponent[] array, int arrayIndex) => _components.CopyTo(array, arrayIndex);

    public System.Collections.Generic.IEnumerator<CNA.GameComponent> GetEnumerator() => _components.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
