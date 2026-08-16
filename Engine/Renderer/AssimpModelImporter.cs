using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

internal sealed partial class AssimpModelImporter(Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();
    private static readonly string EmbeddedCacheDir =
        Path.Combine(Path.GetTempPath(), "GameEngine", "embedded-textures");

    public List<ModelSubmesh> Import(string path)
    {
        var submeshes = new List<ModelSubmesh>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        // glTF/GLB UVs are already OpenGL-style (v=0 bottom). Do NOT FlipUVs here —
        // textures are uploaded with stbi_flip; FlipUVs + flip = double-flip atlas scramble.
        // PreTransformVertices keeps single-file Create in a common space (no hierarchy).
        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.PreTransformVertices);

        unsafe
        {
            var scene = assimp.ImportFile(path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return submeshes;
            }

            Logger.Information(
                "Assimp import ok path={Path} meshes={MeshCount} materials={MaterialCount} embeddedTextures={EmbeddedCount} flags={Flags}",
                path, scene->MNumMeshes, scene->MNumMaterials, scene->MNumTextures, scene->MFlags);

            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                var aiMesh = scene->MMeshes[i];
                var mesh = ExtractMesh(aiMesh);
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(new ModelSubmesh(mesh, material));
            }

            assimp.ReleaseImport(scene);
        }

        return submeshes;
    }

    /// <summary>
    /// Node walk without <see cref="PostProcessSteps.PreTransformVertices"/> —
    /// one part per mesh-bearing node with transform relative to the Assimp root.
    /// </summary>
    public List<AssimpModelPart> ImportParts(string path)
    {
        var parts = new List<AssimpModelPart>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace);

        unsafe
        {
            var scene = assimp.ImportFile(path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model parts path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return parts;
            }

            Logger.Information(
                "Assimp import parts ok path={Path} meshes={MeshCount} materials={MaterialCount} embeddedTextures={EmbeddedCount}",
                path, scene->MNumMeshes, scene->MNumMaterials, scene->MNumTextures);

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            WalkNode(scene, scene->MRootNode, Matrix4x4.Identity, directory, nameCounts, parts);

            assimp.ReleaseImport(scene);
        }

        return parts;
    }

    private unsafe void WalkNode(
        Silk.NET.Assimp.Scene* scene,
        Node* node,
        Matrix4x4 parentWorld,
        string directory,
        Dictionary<string, int> nameCounts,
        List<AssimpModelPart> parts)
    {
        // Assimp: node transform maps local → parent (column-vector storage).
        // Accumulated in Assimp space; transposed to Numerics when emitting parts.
        var world = Matrix4x4.Multiply(parentWorld, node->MTransformation);

        if (node->MNumMeshes > 0 && node->MMeshes != null)
        {
            var submeshes = new List<ModelSubmesh>((int)node->MNumMeshes);
            for (uint i = 0; i < node->MNumMeshes; i++)
            {
                var meshIndex = node->MMeshes[i];
                if (meshIndex >= scene->MNumMeshes)
                    continue;

                var aiMesh = scene->MMeshes[meshIndex];
                var mesh = ExtractMesh(aiMesh);
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(new ModelSubmesh(mesh, material));
            }

            if (submeshes.Count > 0)
            {
                var rawName = node->MName.AsString;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = submeshes[0].Mesh.Name;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = "part";

                var partName = AssimpPartNaming.UniqueSanitize(rawName, nameCounts);
                // Assimp aiMatrix4x4 is column-vector (T in 4th column). System.Numerics /
                // TransformComponent use row-vector (T in 4th row) — transpose before TRS.
                parts.Add(new AssimpModelPart(partName, Matrix4x4.Transpose(world), submeshes));
            }
        }

        for (uint i = 0; i < node->MNumChildren; i++)
        {
            var child = node->MChildren[i];
            if (child != null)
                WalkNode(scene, child, world, directory, nameCounts, parts);
        }
    }

    private static unsafe Mesh ExtractMesh(Silk.NET.Assimp.Mesh* aiMesh)
    {
        var mesh = new Mesh(aiMesh->MName.AsString);

        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex
            {
                Position = aiMesh->MVertices[i],
                BoneId0 = -1,
                BoneId1 = -1,
                BoneId2 = -1,
                BoneId3 = -1
            };

            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);

            if (aiMesh->MNormals != null)
                vertex.Normal = aiMesh->MNormals[i];

            if (hasTangents)
                vertex.Tangent = aiMesh->MTangents[i];

            if (aiMesh->MBitangents != null)
                vertex.Bitangent = aiMesh->MBitangents[i];

            if (hasTexCoords)
            {
                var texcoord3 = aiMesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            mesh.Vertices.Add(vertex);
        }

        for (uint i = 0; i < aiMesh->MNumFaces; i++)
        {
            var face = aiMesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                mesh.Indices.Add(face.MIndices[j]);
        }

        var extent = max - min;
        Logger.Debug(
            "Mesh extracted name={Name} verts={Verts} indices={Indices} hasUV={HasUV} hasNormals={HasNormals} boundsMin={Min} boundsMax={Max} extent={Extent}",
            mesh.Name, mesh.Vertices.Count, mesh.Indices.Count, hasTexCoords, aiMesh->MNormals != null, min, max, extent);

        return mesh;
    }

    private unsafe MeshMaterial ExtractMaterial(
        Silk.NET.Assimp.Scene* scene,
        uint materialIndex,
        string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];

        var albedoPath = ResolveTexturePath(scene, aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Diffuse, directory);

        // Shader expects glTF ORM (R=AO, G=roughness, B=metallic). FBX Substance
        // ships separate grayscale maps — pack them at cook so the shader contract holds.
        var ormPath = ResolveTexturePath(scene, aiMaterial, TextureType.GltfMetallicRoughness, directory);
        var metalPath = ResolveTexturePath(scene, aiMaterial, TextureType.Metalness, directory);
        var roughPath = ResolveTexturePath(scene, aiMaterial, TextureType.DiffuseRoughness, directory);
        var aoPath = ResolveTexturePath(scene, aiMaterial, TextureType.AmbientOcclusion, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Lightmap, directory);

        var destOrm = Path.Combine(directory, $"mat{materialIndex}_orm.bmp");
        string? mrPath;
        try
        {
            mrPath = OrmTexturePacker.PackMaterialMaps(destOrm, ormPath, aoPath, roughPath, metalPath)
                     ?? ormPath;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Material[{Index}] ORM pack failed — falling back to source maps", materialIndex);
            mrPath = ormPath;
        }

        var normalPath = ResolveTexturePath(scene, aiMaterial, TextureType.Normals, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Height, directory);
        var emissivePath = ResolveTexturePath(scene, aiMaterial, TextureType.Emissive, directory);

        var material = new MeshMaterial
        {
            AlbedoTexturePath = albedoPath,
            MetallicRoughnessTexturePath = mrPath,
            NormalTexturePath = normalPath,
            EmissiveTexturePath = emissivePath
        };

        if (TryGetColor(aiMaterial, Assimp.MatkeyBaseColor, out var baseColor))
            material.BaseColorFactor = baseColor;
        else if (TryGetColor(aiMaterial, Assimp.MatkeyColorDiffuse, out var diffuseColor))
            material.BaseColorFactor = new Vector4(diffuseColor.X, diffuseColor.Y, diffuseColor.Z, diffuseColor.W);

        if (TryGetColor(aiMaterial, Assimp.MatkeyColorEmissive, out var emissiveColor))
            material.EmissiveFactor = new Vector3(emissiveColor.X, emissiveColor.Y, emissiveColor.Z);

        if (TryGetFloat(aiMaterial, "$mat.twosided", out var twoSided) && twoSided > 0.5f)
            material.DoubleSided = true;

        if (TryGetString(aiMaterial, "$mat.gltf.alphaMode", out var alphaMode))
        {
            if (alphaMode.Equals("MASK", StringComparison.OrdinalIgnoreCase))
            {
                material.AlphaMode = MaterialAlphaMode.Mask;
                if (TryGetFloat(aiMaterial, "$mat.gltf.alphaCutoff", out var cutoff))
                    material.AlphaCutoff = System.Math.Clamp(cutoff, 0f, 1f);
            }
            else if (alphaMode.Equals("BLEND", StringComparison.OrdinalIgnoreCase))
            {
                material.AlphaMode = MaterialAlphaMode.Blend;
            }
        }

        material.Metallic = TryGetFloat(aiMaterial, Assimp.MatkeyMetallicFactor, out var metallic)
            ? System.Math.Clamp(metallic, 0f, 1f)
            : 0f;

        if (TryGetFloat(aiMaterial, Assimp.MatkeyRoughnessFactor, out var roughness))
            material.Roughness = System.Math.Clamp(roughness, 0f, 1f);
        else if (mrPath == null &&
                 TryGetFloat(aiMaterial, Assimp.MaterialShininess, out var shininess) &&
                 shininess > 0f)
            material.Roughness = System.Math.Clamp(1f - shininess / 256f, 0.04f, 1f);
        else
            material.Roughness = 0.5f;

        // glTF default metallicFactor is 1.0. Albedo-only (or AO-only pack) assets that
        // keep that default render near-black — treat as dielectric without a metal/rough map.
        var packedPbrMaps = ormPath != null || metalPath != null || roughPath != null;
        if (!packedPbrMaps && material.Metallic >= 0.99f)
        {
            Logger.Warning(
                "Material[{Index}] metallic=1 with no metallic-roughness map — " +
                "defaulting metallic to 0 (glTF default is unlit without IBL). " +
                "Use Metallic Override if this should stay metal.",
                materialIndex);
            material.Metallic = 0f;
        }

        var hasBaseColor = TryGetColor(aiMaterial, Assimp.MatkeyBaseColor, out _);
        var hasDiffuse = TryGetColor(aiMaterial, Assimp.MatkeyColorDiffuse, out var loggedDiffuseColor);
        Logger.Debug(
            "Material[{Index}] albedoPath={Albedo} mrPath={MR} normalPath={Normal} emissivePath={Emissive} metallic={Metallic} roughness={Roughness} baseColor={BaseColor} alphaMode={AlphaMode} doubleSided={DoubleSided} diffuseColor={HasDiff}:{DiffuseColor}",
            materialIndex,
            albedoPath ?? "<null>",
            mrPath ?? "<null>",
            normalPath ?? "<null>",
            emissivePath ?? "<null>",
            material.Metallic,
            material.Roughness,
            material.BaseColorFactor,
            material.AlphaMode,
            material.DoubleSided,
            hasDiffuse, loggedDiffuseColor);

        return material;
    }

    private unsafe bool TryGetString(Silk.NET.Assimp.Material* aiMaterial, string key, out string value)
    {
        value = string.Empty;
        AssimpString aiString;
        var result = assimp.GetMaterialString(aiMaterial, key, 0, 0, &aiString);
        if (result != Return.Success)
            return false;

        value = aiString.AsString;
        return !string.IsNullOrEmpty(value);
    }

    private unsafe bool TryGetFloat(Silk.NET.Assimp.Material* aiMaterial, string key, out float value)
    {
        value = 0f;
        var result = assimp.GetMaterialFloatArray(aiMaterial, key, 0, 0, ref value, (uint*)null);
        return result == Return.Success;
    }

    private unsafe bool TryGetColor(Silk.NET.Assimp.Material* aiMaterial, string key, out Vector4 color)
    {
        color = default;
        var result = assimp.GetMaterialColor(aiMaterial, key, 0, 0, ref color);
        return result == Return.Success;
    }

    private unsafe string? ResolveTexturePath(
        Silk.NET.Assimp.Scene* scene,
        Silk.NET.Assimp.Material* aiMaterial,
        TextureType textureType,
        string directory)
    {
        var slotCount = assimp.GetMaterialTextureCount(aiMaterial, textureType);
        if (slotCount == 0)
            return null;

        AssimpString aiPath;
        var result = assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
        {
            Logger.Warning("GetMaterialTexture failed type={Type} result={Result}", textureType, result);
            return null;
        }

        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
        {
            Logger.Warning("Texture type={Type} has empty path (count={Count})", textureType, slotCount);
            return null;
        }

        // GLB/glTF embedded images show up as "*0", "*1", …
        if (texturePath.StartsWith('*'))
        {
            var cached = ExtractEmbeddedTextureToCache(scene, texturePath);
            if (cached == null)
            {
                Logger.Warning("Failed to extract embedded texture type={Type} ref={Ref}", textureType, texturePath);
                return null;
            }

            Logger.Debug("Texture type={Type} extracted embedded {Ref} → {Path}", textureType, texturePath, cached);
            return cached;
        }

        var resolved = AssimpTexturePath.Resolve(texturePath, directory);
        if (resolved == null)
        {
            Logger.Warning(
                "Texture type={Type} path missing on disk: {Path}",
                textureType, texturePath);
            return null;
        }

        Logger.Debug("Texture type={Type} resolved to file {Path}", textureType, resolved);
        return resolved;
    }

    private unsafe string? ExtractEmbeddedTextureToCache(Silk.NET.Assimp.Scene* scene, string embeddedRef)
    {
        // Native Assimp shipped with Silk.NET may lack aiGetEmbeddedTexture — index into MTextures instead.
        // Refs look like "*0" or "*0:filename.png".
        if (embeddedRef.Length < 2 || embeddedRef[0] != '*')
            return null;

        var indexSpan = embeddedRef.AsSpan(1);
        var colon = indexSpan.IndexOf(':');
        if (colon >= 0)
            indexSpan = indexSpan[..colon];

        if (!uint.TryParse(indexSpan, out var index) || index >= scene->MNumTextures)
            return null;

        var tex = scene->MTextures[index];
        if (tex == null)
            return null;

        // Compressed image blob (png/jpg/…). Uncompressed texel buffers are rare for glTF/GLB.
        if (tex->MHeight != 0)
        {
            Logger.Warning(
                "Embedded texture {Ref} is uncompressed ({W}x{H}) — not supported yet",
                embeddedRef, tex->MWidth, tex->MHeight);
            return null;
        }

        var byteCount = (int)tex->MWidth;
        if (byteCount <= 0 || tex->PcData == null)
            return null;

        var bytes = new byte[byteCount];
        fixed (byte* dst = bytes)
            System.Buffer.MemoryCopy(tex->PcData, dst, byteCount, byteCount);

        var ext = GuessImageExtension(bytes, ReadFormatHint(tex));
        // ponytail: content-addressed temp cache; switch to CreateFromMemory if temp files become a problem
        Directory.CreateDirectory(EmbeddedCacheDir);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).AsSpan(0, 16);
        var cachePath = Path.Combine(EmbeddedCacheDir, $"{hash}{ext}");
        if (!System.IO.File.Exists(cachePath))
            System.IO.File.WriteAllBytes(cachePath, bytes);

        return cachePath;
    }

    private static string GuessImageExtension(byte[] bytes, string hint)
    {
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G')
            return ".png";
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return ".jpg";
        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";

        // Assimp AchFormatHint is often garbage on GLB — only trust short alphanumeric hints.
        if (hint.Length is > 0 and <= 4 && hint.All(char.IsLetterOrDigit))
            return "." + hint.ToLowerInvariant();

        return ".bin";
    }

    private static unsafe string ReadFormatHint(Silk.NET.Assimp.Texture* tex)
    {
        // Assimp stores up to 8 chars + null in AchFormatHint.
        var sb = new StringBuilder(8);
        var p = (byte*)&tex->AchFormatHint;
        for (var i = 0; i < 8; i++)
        {
            var c = p[i];
            if (c == 0)
                break;
            if (c is < 32 or > 126)
                return string.Empty;
            sb.Append((char)c);
        }

        return sb.ToString();
    }
}
