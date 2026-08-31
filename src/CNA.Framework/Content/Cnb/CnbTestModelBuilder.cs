using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// Builds a <c>.cnb</c> model fixture through CNA's own model encoder.
///
/// <b>The same reason <see cref="CnbTestWriter"/> exists, and the reason it is not enough.</b> A
/// model container is a set of chunks with a defined relationship -- parts referenced by meshes,
/// bones referenced by both, a skeleton whose joint count has to agree with its matrix arrays --
/// and <c>cna_cnb_encode_model</c> is documented to answer <c>IO</c> when they disagree. Writing
/// those chunks by hand through the generic chunk API would encode this repository's reading of the
/// schema into the fixture, so a decoder that read it wrongly and a fixture that wrote it wrongly in
/// the same way would agree with each other forever. Every byte here comes from CNA.
///
/// Internal and fixture-shaped on purpose. The public CNB surface is a read path; the writer family
/// is a separate piece of work and this is not a preview of it.
/// </summary>
internal sealed class CnbTestModelBuilder : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public CnbTestModelBuilder()
    {
        CnaResult result = Native.cna_cnb_model_create(out CnaHandle model);
        CnaException.ThrowIfFailed(result, nameof(CnbTestModelBuilder));
        _handle = new NativeResourceHandle(
            model.AsNint,
            h => Native.cna_cnb_model_destroy(new CnaHandle(h)).IsSuccess());
    }

    public void SetFlags(bool appliesGltfLightingPolicy, bool hasBoneHierarchy)
    {
        CnaResult result = Native.cna_cnb_model_set_flags(
            Handle, (byte)(appliesGltfLightingPolicy ? 1 : 0), (byte)(hasBoneHierarchy ? 1 : 0));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetFlags));
    }

    /// <summary>Appends a bone. <paramref name="parent"/> is -1 for a root.</summary>
    public unsafe int AddBone(string name, int parent, Matrix transform)
    {
        float[] values =
        [
            transform.M11, transform.M12, transform.M13, transform.M14,
            transform.M21, transform.M22, transform.M23, transform.M24,
            transform.M31, transform.M32, transform.M33, transform.M34,
            transform.M41, transform.M42, transform.M43, transform.M44,
        ];

        ulong index = 0;
        CnaResult result;
        fixed (float* floats = values)
        {
            float* captured = floats;
            result = CnaStringMarshal.WithStringView(
                name, view => Native.cna_cnb_model_add_bone(Handle, view, parent, captured, out index));
        }

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(AddBone));
        return checked((int)index);
    }

    /// <summary>
    /// Appends a part with its geometry.
    ///
    /// The counts are derived from the byte arrays rather than passed separately, because the
    /// encoder rejects a model whose stride times count does not equal its vertex byte count -- and
    /// a fixture helper that let a caller state both is a helper that lets a test author a model
    /// CNA will refuse, for reasons that have nothing to do with what the test is about.
    /// </summary>
    public unsafe int AddPart(
        string name,
        ReadOnlySpan<byte> vertexBytes,
        int vertexStride,
        ReadOnlySpan<byte> indexBytes,
        int indexElementSize,
        CnbEffectKind effectKind = CnbEffectKind.Basic,
        string externalEffect = "",
        int primitiveTopology = 4)
    {
        int vertexCount = vertexStride == 0 ? 0 : vertexBytes.Length / vertexStride;
        int indexCount = indexElementSize == 0 ? 0 : indexBytes.Length / indexElementSize;

        var info = CnaCnbModelPartInfo.Versioned();
        info.VertexStride = (uint)vertexStride;
        info.VertexCount = (uint)vertexCount;
        info.IndexCount = (uint)indexCount;
        info.IndexElementSize = (uint)indexElementSize;
        info.PrimitiveTopology = (uint)primitiveTopology;
        info.PrimitiveCount = (uint)(indexCount / 3);
        info.EffectKind = (CnaCnbEffectKind)effectKind;

        ulong index = 0;
        CnaResult result = CnaStringMarshal.WithStringView(
            name,
            nameView => CnaStringMarshal.WithStringView(
                externalEffect,
                effectView => Native.cna_cnb_model_add_part(Handle, in info, nameView, effectView, out index)));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(AddPart));

        int partIndex = checked((int)index);
        SetBytes(partIndex, vertexBytes, Native.cna_cnb_model_set_part_vertex_bytes, nameof(AddPart));
        SetBytes(partIndex, indexBytes, Native.cna_cnb_model_set_part_index_bytes, nameof(AddPart));
        return partIndex;
    }

    private unsafe delegate CnaResult ByteSetter(CnaHandle model, ulong index, byte* bytes, ulong byteCount);

    private unsafe void SetBytes(int index, ReadOnlySpan<byte> bytes, ByteSetter setter, string context)
    {
        if (bytes.Length == 0)
        {
            return;
        }

        fixed (byte* data = bytes)
        {
            CnaResult result = setter(Handle, (ulong)index, data, (ulong)bytes.Length);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, context);
        }
    }

    /// <summary>Replaces one part's material scalars, leaving its texture slots alone.</summary>
    public void SetMaterial(
        int part,
        Vector4 baseColorFactor,
        float metallicFactor,
        float roughnessFactor,
        CnbAlphaMode alphaMode = CnbAlphaMode.Opaque,
        float alphaCutoff = 0.5f,
        bool doubleSided = false)
    {
        var info = CnaCnbMaterialInfo.Versioned();
        info.BaseColorFactor = new CnaVector4(
            baseColorFactor.X, baseColorFactor.Y, baseColorFactor.Z, baseColorFactor.W);
        info.MetallicFactor = metallicFactor;
        info.RoughnessFactor = roughnessFactor;
        info.AlphaMode = (uint)alphaMode;
        info.AlphaCutoff = alphaCutoff;
        info.DoubleSided = (byte)(doubleSided ? 1 : 0);

        CnaResult result = Native.cna_cnb_model_set_material(Handle, (ulong)part, in info);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetMaterial));
    }

    public void SetMaterialTexture(int part, CnbMaterialTextureSlot slot, string assetName)
    {
        CnaResult result = CnaStringMarshal.WithStringView(
            assetName,
            view => Native.cna_cnb_model_set_material_texture(
                Handle, (ulong)part, (CnaCnbMaterialTextureSlot)slot, view));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetMaterialTexture));
    }

    /// <summary>
    /// Sets one <b>importer</b> slot's texture coordinate set.
    ///
    /// Takes the importer index rather than a <see cref="CnbMaterialTextureSlot"/> on purpose: a
    /// fixture that wants to prove the two spaces are crossed correctly has to be able to address
    /// the importer space directly, or it would be asserting the map against itself.
    /// </summary>
    public void SetImporterCoordinateSet(int part, int importerSlot, byte coordinateSet)
    {
        CnaResult result = Native.cna_cnb_model_set_material_texture_coordinate_set(
            Handle, (ulong)part, (ulong)importerSlot, coordinateSet);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetImporterCoordinateSet));
    }

    /// <summary>Sets one <b>importer</b> slot's UV transform, for the same reason as
    /// <see cref="SetImporterCoordinateSet"/> -- and it is the one that can carry a distinct value
    /// per slot.</summary>
    public void SetImporterTransform(
        int part, int importerSlot, float offsetX, float offsetY, float scaleX, float scaleY, float rotation)
    {
        var transform = new CnaCnbTextureTransform
        {
            OffsetX = offsetX,
            OffsetY = offsetY,
            ScaleX = scaleX,
            ScaleY = scaleY,
            Rotation = rotation,
        };

        CnaResult result = Native.cna_cnb_model_set_material_texture_transform(
            Handle, (ulong)part, (ulong)importerSlot, in transform);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetImporterTransform));
    }

    /// <summary>Appends a mesh naming parts in draw order.</summary>
    public unsafe int AddMesh(string name, int parentBone, params int[] partIndices)
    {
        ArgumentNullException.ThrowIfNull(partIndices);

        var indices = new uint[partIndices.Length];
        for (int i = 0; i < partIndices.Length; i++)
        {
            indices[i] = (uint)partIndices[i];
        }

        ulong index = 0;
        CnaResult result;
        fixed (uint* data = indices)
        {
            uint* captured = indices.Length == 0 ? null : data;
            result = CnaStringMarshal.WithStringView(
                name,
                view => Native.cna_cnb_model_add_mesh(
                    Handle, view, parentBone, captured, (ulong)indices.Length, out index));
        }

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(AddMesh));
        return checked((int)index);
    }

    /// <summary>
    /// Gives the model a skinning skeleton.
    ///
    /// <paramref name="rootPrefix"/> may be empty, which is the encoder's way of saying the source
    /// carried none -- distinct from a prefix of identity matrices.
    /// </summary>
    public unsafe void SetSkeleton(
        int[] hierarchy, Matrix[] bindPose, Matrix[] inverseBindPose, Matrix[] rootPrefix)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(bindPose);
        ArgumentNullException.ThrowIfNull(inverseBindPose);
        ArgumentNullException.ThrowIfNull(rootPrefix);

        float[] bind = Flatten(bindPose);
        float[] inverse = Flatten(inverseBindPose);
        float[] prefix = Flatten(rootPrefix);

        fixed (int* hierarchyData = hierarchy)
        fixed (float* bindData = bind)
        fixed (float* inverseData = inverse)
        fixed (float* prefixData = prefix)
        {
            CnaResult result = Native.cna_cnb_model_set_skeleton(
                Handle,
                hierarchyData,
                (ulong)hierarchy.Length,
                bindData,
                inverseData,
                prefix.Length == 0 ? null : prefixData);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SetSkeleton));
        }
    }

    /// <summary>Encodes the model and writes it, so the test reads a real file.</summary>
    public unsafe void WriteToFile(string path, string contentName = "")
    {
        ArgumentNullException.ThrowIfNull(path);

        byte[]? encoded = null;
        CnaResult result = CnaStringMarshal.WithStringView(contentName, view =>
        {
            // BufferTooSmall is the documented answer to a zero-capacity size query, not a failure.
            CnaResult sizeResult = Native.cna_cnb_encode_model(Handle, view, null, 0, out ulong required);
            if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
            {
                return sizeResult;
            }

            var bytes = new byte[checked((int)required)];
            fixed (byte* destination = bytes)
            {
                CnaResult encodeResult = Native.cna_cnb_encode_model(
                    Handle, view, destination, (ulong)bytes.Length, out ulong written);
                if (!encodeResult.IsSuccess())
                {
                    return encodeResult;
                }

                if (written != (ulong)bytes.Length)
                {
                    throw new CnaException(
                        $"The CNB model encoder asked for {required} bytes and wrote {written}.");
                }
            }

            encoded = bytes;
            return CnaResult.Success;
        });

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(WriteToFile));
        File.WriteAllBytes(path, encoded!);
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    private static float[] Flatten(Matrix[] matrices)
    {
        var values = new float[matrices.Length * 16];
        for (int index = 0; index < matrices.Length; index++)
        {
            Matrix m = matrices[index];
            int at = index * 16;
            values[at + 0] = m.M11; values[at + 1] = m.M12; values[at + 2] = m.M13; values[at + 3] = m.M14;
            values[at + 4] = m.M21; values[at + 5] = m.M22; values[at + 6] = m.M23; values[at + 7] = m.M24;
            values[at + 8] = m.M31; values[at + 9] = m.M32; values[at + 10] = m.M33; values[at + 11] = m.M34;
            values[at + 12] = m.M41; values[at + 13] = m.M42; values[at + 14] = m.M43; values[at + 15] = m.M44;
        }

        return values;
    }
}
