using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CNA.Graphics;

/// <summary>
/// Real XNA's <c>ModelBoneCollection</c>: indexable by position and by name (name lookup throws
/// if not found, matching real XNA). Shape confirmed against the real openeggbert/cna C++ engine's
/// own <c>ModelBoneCollection</c> (int/string indexers, <see cref="TryGetValue"/>,
/// <see cref="Contains"/>) -- not invented. Wraps the same <see cref="List{T}"/> reference its
/// owner (<see cref="Model"/> or <see cref="ModelBone"/>) already holds rather than copying it, so
/// <see cref="ModelBone.AddChild"/> appending to a bone's children list is immediately visible
/// through that bone's own <see cref="ModelBone.Children"/> collection.
/// </summary>
public class ModelBoneCollection : IEnumerable<ModelBone>
{
    private readonly List<ModelBone> _bones;

    internal ModelBoneCollection(List<ModelBone> bones)
    {
        _bones = bones;
    }

    public ModelBone this[int index] => _bones[index];

    public ModelBone this[string name]
    {
        get
        {
            if (TryGetValue(name, out ModelBone? bone))
            {
                return bone;
            }

            throw new KeyNotFoundException($"A bone named '{name}' was not found in this collection.");
        }
    }

    public int Count => _bones.Count;

    public bool TryGetValue(string boneName, [NotNullWhen(true)] out ModelBone? value)
    {
        ArgumentNullException.ThrowIfNull(boneName);

        foreach (ModelBone bone in _bones)
        {
            if (bone.Name == boneName)
            {
                value = bone;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool Contains(ModelBone item) => _bones.Contains(item);

    public List<ModelBone>.Enumerator GetEnumerator() => _bones.GetEnumerator();

    IEnumerator<ModelBone> IEnumerable<ModelBone>.GetEnumerator() => _bones.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _bones.GetEnumerator();
}
