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
/// 3. Meshes: name, parent bone reference, bounding sphere, a rejected <c>Tag</c>, then that many
///    mesh parts (vertex offset/count, start index, primitive count, a rejected <c>Tag</c>, then
///    three shared-resource references in a fixed order -- VertexBuffer, IndexBuffer, Effect).
/// 4. One more bone reference, for the model's own root bone.
/// 5. A rejected <c>Tag</c> for the model itself.
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
    internal static object Read(XnbContentReader reader)
    {
        uint boneCount = reader.ReadUInt32();
        var bones = new List<XnbBoneData>((int)boneCount);
        for (uint i = 0; i < boneCount; i++)
        {
            string name = reader.ReadObject<string>();
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
        var meshes = new List<XnbMeshData>(meshCount);
        for (int m = 0; m < meshCount; m++)
        {
            string meshName = reader.ReadObject<string>();
            int parentBoneIndex = reader.ReadBoneReference(bones.Count);
            BoundingSphere boundingSphere = reader.ReadBoundingSphere();
            reader.RejectNonNullTag($"Mesh '{meshName}'");

            int partCount = reader.ReadInt32();
            var parts = new List<XnbMeshPartData>(partCount);
            for (int p = 0; p < partCount; p++)
            {
                int vertexOffset = reader.ReadInt32();
                int numVertices = reader.ReadInt32();
                int startIndex = reader.ReadInt32();
                int primitiveCount = reader.ReadInt32();
                reader.RejectNonNullTag($"Mesh '{meshName}' part {p}");

                var part = new XnbMeshPartData
                {
                    VertexOffset = vertexOffset,
                    NumVertices = numVertices,
                    StartIndex = startIndex,
                    PrimitiveCount = primitiveCount,
                };

                // Fixed order, matching real XNA's own ModelReader exactly.
                reader.ReadSharedResource(o => part.VertexBuffer = (XnbVertexBufferData)o);
                reader.ReadSharedResource(o => part.IndexBuffer = (XnbIndexBufferData)o);
                reader.ReadSharedResource(o => part.Effect = (XnbBasicEffectData)o);

                parts.Add(part);
            }

            meshes.Add(new XnbMeshData(meshName, parentBoneIndex, boundingSphere, parts));
        }

        int rootBoneIndex = reader.ReadBoneReference(bones.Count);
        reader.RejectNonNullTag("Model");

        return new XnbModelData(bones, meshes, rootBoneIndex);
    }
}
