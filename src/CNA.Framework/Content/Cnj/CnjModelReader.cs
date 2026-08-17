using System.Text.Json;
using CNA.Content.Xnb;
using CNA.Graphics;

namespace CNA.Content.Cnj;

/// <summary>
/// Reads a real, minimal-scope <c>.cnj</c> <c>Model</c> document into a <see cref="CnjModelData"/>,
/// matching the real openeggbert/cna C++ engine's own <c>ModelTypeReader::Read</c>
/// (<c>modules/content/src/Xna/ContentManager.cpp</c>) for the subset this reader supports: a JSON
/// envelope plus a flat <c>"meshes"</c> array, an optional real <c>"bones"</c> scene-graph hierarchy
/// (cnjVersion 2), <c>BasicEffect</c> only, vertex sidecar strides 16/20/24/32 only. Deliberately,
/// explicitly out of scope (rejected with a clear <see cref="ContentLoadException"/>, never silently
/// ignored or mis-loaded -- matching this project's own <c>.xnb</c>-side "detect and throw a clear,
/// documented exception" precedent for LZX/LZ4 compression): <c>"skeleton"</c>/<c>"animations"</c>
/// (skinning/runtime animation playback -- a wholly separate subsystem from the rigid <c>"bones"</c>
/// hierarchy this reader *does* support, confirmed architecturally independent in the real C++
/// source; also not renderable in any meaningful way today, since this project has no
/// <c>SkinnedEffect</c> anywhere to ever consume the skinning data), per-mesh <c>"morphTargets"</c>,
/// every non-<c>BasicEffect</c> effect type, and vertex strides 48/52/56/68 (the tangent/skinned
/// shapes, which pair with <c>"skeleton"</c>-consuming effects for the identical reason). <c>"lights"</c>
/// is the one field this reader silently ignores rather than rejects -- a pure lighting
/// *enhancement*, not a structural feature, so omitting it changes a loaded model's lit appearance,
/// not its structural correctness (the same tier this project's own <c>XnbBasicEffectReader</c>
/// already accepted for stubbing full material fidelity).
///
/// Unlike the real C++ reader (which locates <c>"meshes"</c> via a hand-rolled brace-matching scan
/// over the raw JSON string, a historical artifact of its own JSON library's limitations), this
/// reader uses <see cref="JsonDocument"/> uniformly for the whole document, matching this project's
/// own design invariant of using the real BCL for non-CNA-specific concepts -- only the *fields and
/// their read order/semantics* are ported faithfully, not the C++ scanning mechanism that produces
/// them.
///
/// No <see cref="Graphics.GraphicsDevice"/> dependency at all -- fully unit-testable without a real
/// <c>cna-native</c>, the same rare "fully real, testable today" status <see cref="XnbModelReader"/>
/// already has for the <c>.xnb</c> path.
/// </summary>
internal static class CnjModelReader
{
    private const int MaxSupportedCnjVersion = 2;

    internal static CnjModelData Read(string json, string assetName, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(assetName);
        ArgumentNullException.ThrowIfNull(rootDirectory);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ContentLoadException($"'{assetName}.cnj' is not valid JSON.", ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ContentLoadException($"'{assetName}.cnj' does not contain a JSON object at its root.");
            }

            ValidateEnvelope(root, assetName);

            IReadOnlyList<CnjBoneData> bones = ReadBones(root, assetName);

            var meshes = new List<CnjMeshData>();
            if (root.TryGetProperty("meshes", out JsonElement meshesElement) && meshesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement meshElement in meshesElement.EnumerateArray())
                {
                    CnjMeshData? mesh = ReadMesh(meshElement, assetName, rootDirectory, bones.Count);
                    if (mesh is not null)
                    {
                        meshes.Add(mesh);
                    }
                }
            }

            return new CnjModelData(bones, meshes);
        }
    }

    private static void ValidateEnvelope(JsonElement root, string assetName)
    {
        if (!root.TryGetProperty("cnjVersion", out JsonElement versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out int version))
        {
            throw new ContentLoadException($"'{assetName}.cnj' is missing a valid integer 'cnjVersion' field.");
        }

        if (version < 1 || version > MaxSupportedCnjVersion)
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' has cnjVersion {version}, which this minimal .cnj Model reader does not support " +
                $"(only versions 1-{MaxSupportedCnjVersion} are -- a higher version implies skinning semantics this reader doesn't implement).");
        }

        if (!root.TryGetProperty("type", out JsonElement typeElement) ||
            typeElement.ValueKind != JsonValueKind.String ||
            typeElement.GetString() != "Model")
        {
            throw new ContentLoadException($"'{assetName}.cnj' is not a Model asset (its 'type' field is not \"Model\").");
        }

        if (root.TryGetProperty("sourceFile", out _))
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' has a 'sourceFile' field -- Model .cnj documents must be self-contained.");
        }

        if (root.TryGetProperty("skeleton", out _))
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' has a 'skeleton' field (skinned rigid meshes), which this minimal .cnj Model reader does not support.");
        }

        // A present-but-non-array "bones" field (an author mistake, e.g. an object or a number) is
        // rejected here, at the envelope-validation stage, matching every other malformed-field case
        // in this reader -- ReadBones (called separately, after this method returns) handles the
        // real array-shaped case, including the "0 or 1 entries means no real hierarchy" convention.
        // JSON null is deliberately treated the same as "absent," not as malformed -- a code-review
        // finding caught an earlier version of this check over-rejecting "bones": null, which some
        // JSON authoring conventions emit for every unset optional field rather than omitting the
        // key entirely.
        if (root.TryGetProperty("bones", out JsonElement bonesElement) &&
            bonesElement.ValueKind != JsonValueKind.Null &&
            bonesElement.ValueKind != JsonValueKind.Array)
        {
            throw new ContentLoadException($"'{assetName}.cnj' has a 'bones' field that is not a JSON array.");
        }
    }

    /// <summary>Reads the document's real, multi-entry <c>"bones"</c> scene-graph hierarchy
    /// (cnjVersion 2) -- a flat array, parent-before-child, entry 0 always the root. Returns an
    /// empty list (matching the real engine's own <c>hasBoneHierarchy = cnjBones.size() &gt; 1</c>
    /// check) when <c>"bones"</c> is absent, null, or has 0-1 entries -- that's the
    /// cnjVersion-1-compatible "no real hierarchy" case, and <see cref="CnjModelBuilder"/> still
    /// synthesizes a fresh child bone per mesh for it, exactly as before this increment. A single
    /// forward pass suffices (unlike <see cref="XnbModelReader"/>'s own two-pass bone-hierarchy
    /// read): each entry encodes its own parent's index, always an already-constructed earlier
    /// entry, never a later one.</summary>
    private static IReadOnlyList<CnjBoneData> ReadBones(JsonElement root, string assetName)
    {
        if (!root.TryGetProperty("bones", out JsonElement bonesElement) ||
            bonesElement.ValueKind != JsonValueKind.Array ||
            bonesElement.GetArrayLength() <= 1)
        {
            return [];
        }

        int count = bonesElement.GetArrayLength();
        var bones = new List<CnjBoneData>(count);
        int index = 0;
        foreach (JsonElement boneElement in bonesElement.EnumerateArray())
        {
            if (boneElement.ValueKind != JsonValueKind.Object)
            {
                throw new ContentLoadException($"'{assetName}.cnj' has a non-object entry in its 'bones' array.");
            }

            string name = GetString(boneElement, "name") ?? "";
            if (name.Length == 0)
            {
                name = index == 0 ? "Root" : $"Node{index}";
            }

            // A code-review finding caught this diff being inconsistent with itself: JSON null is
            // deliberately treated as "absent" for the top-level "bones" field (see
            // ValidateEnvelope's own comment on why -- some authoring conventions emit null for
            // every unset optional field rather than omitting the key), but this per-bone "parent"
            // field originally rejected null instead of falling back to defaultParent the same way.
            int defaultParent = index == 0 ? -1 : 0;
            int parent = defaultParent;
            if (!IsAbsentOrNull(boneElement, "parent", out JsonElement parentElement))
            {
                if (parentElement.ValueKind != JsonValueKind.Number || !parentElement.TryGetInt32(out parent))
                {
                    throw new ContentLoadException($"'{assetName}.cnj' bone {index} ('{name}') field 'parent' must be an integer.");
                }
            }

            // Entry 0 is always the root regardless of its own recorded 'parent' value (there is no
            // earlier entry for it to reference); every later entry's 'parent' must reference an
            // already-constructed earlier entry -- parent-before-child is a real invariant, not just
            // a convention, matching the real engine's own cross-reference rejection here.
            if (index > 0 && (parent < 0 || parent >= index))
            {
                throw new ContentLoadException($"'{assetName}.cnj' bone {index} ('{name}') has an out-of-range 'parent' index ({parent}).");
            }

            Matrix transform = GetTransform(boneElement, assetName, index, name);

            bones.Add(new CnjBoneData(name, parent, transform));
            index++;
        }

        return bones;
    }

    private static Matrix GetTransform(JsonElement boneElement, string assetName, int index, string name)
    {
        // Same null-treated-as-absent fix as "parent" above.
        if (IsAbsentOrNull(boneElement, "transform", out JsonElement transformElement))
        {
            return Matrix.Identity;
        }

        if (transformElement.ValueKind != JsonValueKind.Array || transformElement.GetArrayLength() != 16)
        {
            throw new ContentLoadException($"'{assetName}.cnj' bone {index} ('{name}') field 'transform' must be a 16-element array.");
        }

        Span<float> m = stackalloc float[16];
        int i = 0;
        foreach (JsonElement element in transformElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetSingle(out float value))
            {
                throw new ContentLoadException($"'{assetName}.cnj' bone {index} ('{name}') field 'transform' must contain only numbers.");
            }

            m[i++] = value;
        }

        return new Matrix(
            m[0], m[1], m[2], m[3],
            m[4], m[5], m[6], m[7],
            m[8], m[9], m[10], m[11],
            m[12], m[13], m[14], m[15]);
    }

    private static CnjMeshData? ReadMesh(JsonElement meshElement, string assetName, string rootDirectory, int boneCount)
    {
        if (meshElement.ValueKind != JsonValueKind.Object)
        {
            throw new ContentLoadException($"'{assetName}.cnj' has a non-object entry in its 'meshes' array.");
        }

        string name = GetString(meshElement, "name") ?? "";
        if (name.Length == 0)
        {
            name = "mesh";
        }

        if (meshElement.TryGetProperty("morphTargets", out _))
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' mesh '{name}' has a 'morphTargets' field, which this minimal .cnj Model reader does not support.");
        }

        string verticesField = GetString(meshElement, "vertices") ?? "";
        string indicesField = GetString(meshElement, "indices") ?? "";
        if (verticesField.Length == 0 || indicesField.Length == 0)
        {
            // Matches the real engine's own "if (vertFile.empty() || idxFile.empty()) continue;" --
            // a mesh entry with no sidecar files is silently skipped, not an error.
            return null;
        }

        int stride = GetInt(meshElement, "vertexStride", 16, assetName, name, "vertexStride");
        if (stride <= 0)
        {
            // Matches the real engine's own "if (stride <= 0) continue;" -- also silently skipped.
            return null;
        }

        VertexDeclaration declaration = stride switch
        {
            16 => VertexPositionColor.VertexDeclaration,
            20 => VertexPositionTexture.VertexDeclaration,
            24 => VertexPositionColorTexture.VertexDeclaration,
            32 => VertexPositionNormalTexture.VertexDeclaration,
            _ => throw new ContentLoadException(
                $"'{assetName}.cnj' mesh '{name}' uses vertexStride {stride}, which this minimal .cnj Model reader does not support (only 16/20/24/32 are)."),
        };

        string? effectName = GetString(meshElement, "effect");
        if (!string.IsNullOrEmpty(effectName) && effectName != "BasicEffect")
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' mesh '{name}' uses effect '{effectName}', which this minimal .cnj Model reader does not support (only BasicEffect is).");
        }

        string verticesPath = ResolveContainedSidecarPath(rootDirectory, assetName, name, "vertices", verticesField);
        string indicesPath = ResolveContainedSidecarPath(rootDirectory, assetName, name, "indices", indicesField);

        byte[] vertexBytes = ReadSidecarBytes(verticesPath, assetName, name, "vertices");
        byte[] indexBytes = ReadSidecarBytes(indicesPath, assetName, name, "indices");

        // Reject (never silently truncate) a sidecar file whose byte length isn't a whole number of
        // elements -- XnbVertexBufferData/XnbIndexBufferData's own documented invariant (see
        // XnbVertexBufferReader.cs) is Data.Length == count * elementSize exactly, and this is the
        // exact same "size not a whole number of elements" corrupt-input case
        // XnbIndexBufferReader.cs already rejects for the .xnb path, for the identical reason: a
        // truncating caller here would otherwise silently drop real geometry with no diagnostic
        // (a first attempt at this fix truncated rather than threw -- a code-review finding caught
        // that as a quieter, less honest failure mode than every other corrupt-input case in this
        // reader, which all throw a clear ContentLoadException instead).
        int vertexCount = RequireWholeNumberOfElements(vertexBytes.Length, stride, assetName, name, "vertices");
        var vertexBufferData = new XnbVertexBufferData(declaration, vertexCount, vertexBytes);

        // Matches the same XNA-standard ModelProcessor convention already documented for the .xnb
        // path (see xnb-model-spec.md §9): sixteen-bit indices unless the vertex count itself
        // requires thirty-two-bit ones -- there is no separate flag for this in the file either.
        bool use32BitIndices = vertexCount > 65535;
        int indexSize = use32BitIndices ? 4 : 2;
        int indexCount = RequireWholeNumberOfElements(indexBytes.Length, indexSize, assetName, name, "indices");
        int primitiveCount = indexCount / 3;
        var indexBufferData = new XnbIndexBufferData(!use32BitIndices, indexBytes);

        string? textureField = GetString(meshElement, "texture");
        string? textureReference = string.IsNullOrEmpty(textureField)
            ? null
            : ResolveContainedSidecarPath(rootDirectory, assetName, name, "texture", textureField);

        bool vertexColorEnabled = GetBool(meshElement, "vertexColorEnabled", false);

        // Only consulted when the document has a real bone hierarchy (boneCount > 0) -- matching
        // this reader's own "0 or 1 'bones' entries means no real hierarchy" convention, in which
        // case CnjModelBuilder still synthesizes a fresh child bone per mesh instead, unchanged.
        int? parentBoneIndex = null;
        if (boneCount > 0)
        {
            int rawParentBone = GetOptionalInt(meshElement, "parentBone", 0, assetName, name, "parentBone");
            if (rawParentBone < 0 || rawParentBone >= boneCount)
            {
                throw new ContentLoadException(
                    $"'{assetName}.cnj' mesh '{name}' has an out-of-range 'parentBone' index ({rawParentBone}).");
            }

            parentBoneIndex = rawParentBone;
        }

        var effectData = new CnjBasicEffectData(textureReference, vertexColorEnabled);
        return new CnjMeshData(name, vertexBufferData, indexBufferData, primitiveCount, effectData, parentBoneIndex);
    }

    private static string ResolveContainedSidecarPath(string rootDirectory, string assetName, string meshName, string field, string relativePath)
    {
        if (!CnjPathContainment.TryResolve(rootDirectory, relativePath, out string resolved))
        {
            throw new ContentLoadException(
                $"manifest '{assetName}.cnj' mesh '{meshName}' field '{field}' must be a non-empty relative path contained within its authorized content root.");
        }

        return resolved;
    }

    private static byte[] ReadSidecarBytes(string path, string assetName, string meshName, string field)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' mesh '{meshName}' field '{field}' references a sidecar file that could not be read: '{path}'.", ex);
        }
    }

    /// <summary>Shared by both the vertex and index sidecar checks -- a code-review finding flagged
    /// the two near-duplicate inline checks this replaced as a drift risk (a future edit fixing one
    /// call site could easily miss the other). Returns the element count on success.</summary>
    private static int RequireWholeNumberOfElements(int totalBytes, int elementSize, string assetName, string meshName, string field)
    {
        if (totalBytes % elementSize != 0)
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' mesh '{meshName}' field '{field}' has a size ({totalBytes} bytes) that is not a whole number of {elementSize}-byte elements.");
        }

        return totalBytes / elementSize;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return defaultValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue,
        };
    }

    /// <summary>Strict: an absent field falls back to <paramref name="defaultValue"/>, but a
    /// *present* non-integer value -- including JSON <c>null</c> -- always throws. Used only for
    /// <c>"vertexStride"</c>, where silently guessing a value on <c>null</c> would be actively
    /// dangerous rather than merely permissive: a code-review finding caught an earlier version of
    /// this method treating <c>null</c> the same as "absent" here too, which would have let
    /// <c>"vertexStride": null</c> silently default to 16 -- and <see cref="RequireWholeNumberOfElements"/>'s
    /// own whole-number check can't reliably catch this: 32 (this reader's own most commonly-used
    /// supported stride -- the real fixture <c>quad.cnj</c> uses it) is itself an exact multiple of
    /// 16, so real stride-32 vertex bytes with a <c>null</c> stride would silently pass that check
    /// and be misread as twice as many stride-16 vertices with a completely wrong field layout, not
    /// merely rejected (a second code-review finding caught this doc comment's own earlier,
    /// overstated claim that "any" supported/unsupported stride divides evenly by 16 -- only 32 and
    /// 48 actually do; 20/24/52/56/68 don't, though specific vertex counts at those strides can still
    /// coincidentally produce a byte total divisible by 16, e.g. 4 vertices &#215; 20 = 80 bytes -- the
    /// point stands without needing every stride to divide evenly, so the claim was narrowed rather
    /// than dropped). See <see cref="GetOptionalInt"/> for the *other* shape (null genuinely means
    /// "absent," used for <c>"parentBone"</c>, where defaulting to the root bone on a missing/null
    /// value is always a safe, meaningful choice).</summary>
    private static int GetInt(JsonElement element, string propertyName, int defaultValue, string assetName, string meshName, string field)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return defaultValue;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
        {
            throw new ContentLoadException($"'{assetName}.cnj' mesh '{meshName}' field '{field}' must be an integer.");
        }

        return result;
    }

    /// <summary>Same shape as <see cref="GetInt"/>, but JSON <c>null</c> is treated the same as
    /// "absent" too (falls back to <paramref name="defaultValue"/>) -- see that method's own doc
    /// comment for why the two are kept deliberately separate rather than merged into one
    /// null-tolerant helper. Delegates to <see cref="GetInt"/> for the actual number-parsing/throw
    /// logic once the null-vs-absent question is settled -- two independent code-review passes
    /// converged on this: an earlier version duplicated that logic instead, the exact
    /// near-duplicate-logic drift risk <see cref="IsAbsentOrNull"/> was extracted to prevent, just
    /// one level up.</summary>
    private static int GetOptionalInt(JsonElement element, string propertyName, int defaultValue, string assetName, string meshName, string field)
    {
        return IsAbsentOrNull(element, propertyName, out _)
            ? defaultValue
            : GetInt(element, propertyName, defaultValue, assetName, meshName, field);
    }

    /// <summary>Shared by every field where JSON <c>null</c> is deliberately treated the same as
    /// "absent" (bone <c>"parent"</c>/<c>"transform"</c>, mesh <c>"parentBone"</c> via
    /// <see cref="GetOptionalInt"/>) -- a code-review finding caught this exact check being
    /// independently re-implemented at each call site with inconsistent boolean polarity (some
    /// `&amp;&amp; ValueKind != Null`, some `|| ValueKind == Null`), the same "near-duplicate inline
    /// checks are a drift risk" issue <see cref="RequireWholeNumberOfElements"/> was already
    /// extracted to prevent elsewhere in this file. Deliberately <b>not</b> used by
    /// <c>"vertexStride"</c>'s own <see cref="GetInt"/> -- see that method's own doc comment for
    /// why that field needs different (stricter) null handling.</summary>
    private static bool IsAbsentOrNull(JsonElement element, string propertyName, out JsonElement value)
    {
        return !element.TryGetProperty(propertyName, out value) || value.ValueKind == JsonValueKind.Null;
    }
}
