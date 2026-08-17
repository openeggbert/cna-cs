using System.Text.Json;
using CNA.Content.Xnb;
using CNA.Graphics;

namespace CNA.Content.Cnj;

/// <summary>
/// Reads a real, minimal-scope <c>.cnj</c> <c>Model</c> document into a <see cref="CnjModelData"/>,
/// matching the real openeggbert/cna C++ engine's own <c>ModelTypeReader::Read</c>
/// (<c>modules/content/src/Xna/ContentManager.cpp</c>) for the subset this reader supports: a JSON
/// envelope plus a flat <c>"meshes"</c> array, <c>BasicEffect</c> only, vertex sidecar strides
/// 16/20/24/32 only. Deliberately, explicitly out of scope (rejected with a clear
/// <see cref="ContentLoadException"/>, never silently ignored or mis-loaded -- matching this
/// project's own <c>.xnb</c>-side "detect and throw a clear, documented exception" precedent for
/// LZX/LZ4 compression): the <c>"bones"</c> hierarchy (cnjVersion 2), <c>"skeleton"</c>/
/// <c>"animations"</c> (skinned rigid meshes), per-mesh <c>"morphTargets"</c>, every non-
/// <c>BasicEffect</c> effect type, and vertex strides 48/52/56/68 (the PBR/skinned shapes).
/// <c>"lights"</c> is the one field this reader silently ignores rather than rejects -- a pure
/// lighting *enhancement*, not a structural feature, so omitting it changes a loaded model's lit
/// appearance, not its structural correctness (the same tier this project's own
/// <c>XnbBasicEffectReader</c> already accepted for stubbing full material fidelity).
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
    private const int MaxSupportedCnjVersion = 1;

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

            var meshes = new List<CnjMeshData>();
            if (root.TryGetProperty("meshes", out JsonElement meshesElement) && meshesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement meshElement in meshesElement.EnumerateArray())
                {
                    CnjMeshData? mesh = ReadMesh(meshElement, assetName, rootDirectory);
                    if (mesh is not null)
                    {
                        meshes.Add(mesh);
                    }
                }
            }

            return new CnjModelData(meshes);
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
                $"(only version {MaxSupportedCnjVersion} is -- a higher version implies bone-hierarchy/skinning semantics this reader doesn't implement).");
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

        // A "bones" array of 0 or 1 entries is the cnjVersion-1-compatible "no real hierarchy" case
        // the real engine itself falls back to -- fine to silently ignore. More than one entry
        // implies real bone-hierarchy semantics (cnjVersion 2), rejected outright as an independent
        // safety net alongside the cnjVersion check above.
        if (root.TryGetProperty("bones", out JsonElement bonesElement) &&
            bonesElement.ValueKind == JsonValueKind.Array &&
            bonesElement.GetArrayLength() > 1)
        {
            throw new ContentLoadException(
                $"'{assetName}.cnj' has a multi-entry 'bones' array (bone hierarchy), which this minimal .cnj Model reader does not support.");
        }
    }

    private static CnjMeshData? ReadMesh(JsonElement meshElement, string assetName, string rootDirectory)
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

        int vertexCount = vertexBytes.Length / stride;
        var vertexBufferData = new XnbVertexBufferData(declaration, vertexCount, vertexBytes);

        // Matches the same XNA-standard ModelProcessor convention already documented for the .xnb
        // path (see xnb-model-spec.md §9): sixteen-bit indices unless the vertex count itself
        // requires thirty-two-bit ones -- there is no separate flag for this in the file either.
        bool use32BitIndices = vertexCount > 65535;
        int indexSize = use32BitIndices ? 4 : 2;
        int indexCount = indexBytes.Length / indexSize;
        int primitiveCount = indexCount / 3;
        var indexBufferData = new XnbIndexBufferData(!use32BitIndices, indexBytes);

        string? textureField = GetString(meshElement, "texture");
        string? textureReference = string.IsNullOrEmpty(textureField)
            ? null
            : ResolveContainedSidecarPath(rootDirectory, assetName, name, "texture", textureField);

        bool vertexColorEnabled = GetBool(meshElement, "vertexColorEnabled", false);

        var effectData = new CnjBasicEffectData(textureReference, vertexColorEnabled);
        return new CnjMeshData(name, vertexBufferData, indexBufferData, primitiveCount, effectData);
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
}
