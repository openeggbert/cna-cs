using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>ModelBoneCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Graphics.ModelBoneCollection</c> -- same reasoning as <c>SongCollection</c>'s
/// own doc comment in the media compat layer (extending directly would inherit an indexer typed to
/// <c>CNA.Graphics.ModelBone</c>, not this namespace's own).</summary>
public sealed class ModelBoneCollection : IEnumerable<ModelBone>
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
