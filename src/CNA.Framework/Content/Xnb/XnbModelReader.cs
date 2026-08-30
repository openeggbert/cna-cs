namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>ModelReader</c> object graph into an <see cref="XnbModelData"/>,
/// matching the real openeggbert/cna C++ engine's own <c>ModelContentTypeReaders.cpp</c>
/// (<c>ModelReader::Read</c>) exactly -- confirmed field-by-field, and cross-checked byte-for-byte
/// against a real, uncompressed, MonoGame-compiled <c>Model</c> asset (2 bones, root bone
/// <c>"RootNode"</c>). Sequence, in order:
///
/// 1. Bone names + transforms (<see cref="XnbContentReader.ReadObject{T}"/> for each name --
///    dispatches to <c>StringReader</c> -- then a raw <see cref="XnbContentReader.ReadMatrix"/>).
/// 2. Bone hierarchy, in a *second* pass over the same bones, after every bone/transform has been
///    read: for each bone, one bone reference for its own parent is read but discarded (each
///    bone's *children* loop below is what actually establishes the hierarchy, so recording the
///    parent redundantly here would just be setting it twice from two different encodings), then a
///    child count and that many child bone references.
/// 3. Meshes: name, parent bone reference, bounding sphere, the mesh's <c>Tag</c>, then that many
///    mesh parts (vertex offset/count, start index, primitive count, the part's <c>Tag</c>, then
///    three shared-resource references in a fixed order -- VertexBuffer, IndexBuffer, Effect).
/// 4. One more bone reference, for the model's own root bone.
/// 5. The model's own <c>Tag</c>.
///
/// Deliberately returns <see cref="XnbModelData"/>, not a real <see cref="Graphics.Model"/> --
/// see that type's own doc comment for why (shared resources referenced here, like this mesh
/// part's <c>VertexBuffer</c>, are populated *after* this method returns, by the two-pass
/// mechanism <see cref="XnbContentReader"/> documents; a real <see cref="Graphics.VertexBuffer"/>
/// also needs a real, native-backed <see cref="Graphics.GraphicsDevice"/> this method has no
/// access to). <see cref="XnbModelBuilder"/> does that final assembly.
/// </summary>
internal static class XnbModelReader
{
    // A code-review finding: unlike the type-reader-table count (capped at 4096) and vertex
    // element count (capped at 1024) elsewhere in this feature, these three counts had no
    // plausibility bound at all -- a corrupt file could set e.g. boneCount to over a billion and
    // trigger a huge List<T> allocation/stall instead of a clean, immediate ContentLoadException.
    // Generous (real models can legitimately have thousands of bones/meshes/parts) but bounded.
    private const int MaxPlausibleCount = 1_000_000;

    internal static object Read(XnbContentReader reader)
    {
        uint boneCount = reader.ReadUInt32();
        if (boneCount > MaxPlausibleCount)
        {
            throw new ContentLoadException($"Corrupt .xnb file: implausible bone count {boneCount}.");
        }

        var bones = new List<XnbBoneData>((int)boneCount);
        for (uint i = 0; i < boneCount; i++)
        {
            string? name = reader.ReadObjectOrNull<string>();
            Matrix transform = reader.ReadMatrix();
            bones.Add(new XnbBoneData((int)i, name, transform));
        }

        for (int i = 0; i < bones.Count; i++)
        {
            _ = reader.ReadBoneReference(bones.Count); // parent -- see this type's own doc comment
            uint childCount = reader.ReadUInt32();
            for (uint c = 0; c < childCount; c++)
            {
                int childIndex = reader.ReadBoneReference(bones.Count);
                if (childIndex >= 0)
                {
                    bones[i].ChildIndices.Add(childIndex);
                }
            }
        }

        int meshCount = reader.ReadInt32();
        if (meshCount is < 0 or > MaxPlausibleCount)
        {
            throw new ContentLoadException($"Corrupt .xnb file: implausible mesh count {meshCount}.");
        }

        var meshes = new List<XnbMeshData>(meshCount);
        for (int m = 0; m < meshCount; m++)
        {
            string? meshName = reader.ReadObjectOrNull<string>();
            int parentBoneIndex = reader.ReadBoneReference(bones.Count);
            BoundingSphere boundingSphere = reader.ReadBoundingSphere();
            object? meshTag = reader.ReadTag();

            int partCount = reader.ReadInt32();
            if (partCount is < 0 or > MaxPlausibleCount)
            {
                throw new ContentLoadException($"Corrupt .xnb file: implausible mesh part count {partCount}.");
            }

            var parts = new List<XnbMeshPartData>(partCount);
            for (int p = 0; p < partCount; p++)
            {
                int vertexOffset = reader.ReadInt32();
                int numVertices = reader.ReadInt32();
                int startIndex = reader.ReadInt32();
                int primitiveCount = reader.ReadInt32();
                object? partTag = reader.ReadTag();

                var part = new XnbMeshPartData
                {
                    VertexOffset = vertexOffset,
                    NumVertices = numVertices,
                    StartIndex = startIndex,
                    PrimitiveCount = primitiveCount,
                    Tag = partTag,
                };

                // Fixed order, matching real XNA's own ModelReader exactly. A code-review finding:
                // each cast previously trusted the resolved shared resource was always the
                // expected type -- a corrupt file with a mismatched type-reader at one of these
                // slots would otherwise surface as an unhandled InvalidCastException instead of
                // the clear ContentLoadException every other corrupt-input case in this feature
                // produces.
                reader.ReadSharedResource(o => part.VertexBuffer = XnbContentReader.RequireType<XnbVertexBufferData>(o, "a mesh part's VertexBuffer"));
                reader.ReadSharedResource(o => part.IndexBuffer = XnbContentReader.RequireType<XnbIndexBufferData>(o, "a mesh part's IndexBuffer"));
                reader.ReadSharedResource(o => part.Effect = XnbContentReader.RequireType<XnbEffectData>(o, "a mesh part's Effect"));

                parts.Add(part);
            }

            meshes.Add(new XnbMeshData(meshName, parentBoneIndex, boundingSphere, parts, meshTag));
        }

        int rootBoneIndex = reader.ReadBoneReference(bones.Count);
        object? modelTag = reader.ReadTag();

        return new XnbModelData(bones, meshes, rootBoneIndex, modelTag);
    }
}
