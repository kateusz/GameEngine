using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpNode = Silk.NET.Assimp.Node;
using Mesh = Engine.Renderer.Meshes.Mesh;

namespace Engine.Renderer.Models;

internal sealed class AssimpModelImporter(ITextureFactory textureFactory) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();
    
    private static readonly string EmbeddedCacheDir =
        Path.Combine(Path.GetTempPath(), "GameEngine", "embedded-textures");
    
    private readonly Assimp _assimp = Assimp.GetApi();
    private bool _disposed;

    public (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph) Import(string path, bool mergeByMaterial = false)
    {
        var submeshes = new List<Mesh>();
        var pendingTextures = new List<(Mesh Mesh, MeshMaterialMerger.ModelMaterialInfo Material)>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var isGltf = IsGltfPath(path);
        var flags = BuildImportFlags(isGltf);

        ModelSceneNode? sceneGraph = null;

        unsafe
        {
            var scene = _assimp.ImportFile(path, flags);
            if (scene == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, _assimp.GetErrorStringS());
                return ([], null);
            }

            try
            {
                if ((scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
                {
                    Logger.Error(
                        "Failed to import model path={Path} assimpError={AssimpError}",
                        path, _assimp.GetErrorStringS());
                    return ([], null);
                }

                var meshIndexMap = new Dictionary<uint, int>();
                for (uint i = 0; i < scene->MNumMeshes; i++)
                {
                    var aiMesh = scene->MMeshes[i];
                    if (aiMesh->MNumVertices == 0 || aiMesh->MNumFaces == 0)
                    {
                        Logger.Debug(
                            "Skipping empty mesh name={Name} vertices={Vertices} faces={Faces}",
                            aiMesh->MName.AsString, aiMesh->MNumVertices, aiMesh->MNumFaces);
                        continue;
                    }

                    var mesh = ExtractMesh(aiMesh);
                    if (mesh.Indices.Count == 0)
                    {
                        Logger.Debug("Skipping mesh with no triangles name={Name}", mesh.Name);
                        continue;
                    }

                    var material = ExtractMaterialInfo(scene, aiMesh->MMaterialIndex, directory, isGltf);
                    mesh.Shininess = material.Shininess;
                    pendingTextures.Add((mesh, material));
                    meshIndexMap[i] = submeshes.Count;
                    submeshes.Add(mesh);
                }

                sceneGraph = WalkNode(scene->MRootNode, meshIndexMap);
            }
            finally
            {
                _assimp.ReleaseImport(scene);
            }
        }

        foreach (var (mesh, material) in pendingTextures)
        {
            mesh.DiffuseTexture = LoadTexture(material.DiffusePath, sRgb: true);
            mesh.SpecularTexture = LoadTexture(material.SpecularPath);
            mesh.NormalTexture = LoadTexture(material.NormalPath);
        }

        if (!mergeByMaterial)
            return (submeshes, sceneGraph);

        var materialInfos = pendingTextures.Select(static t => t.Material).ToList();
        var merged = MeshMaterialMerger.Merge(submeshes, sceneGraph, materialInfos);
        foreach (var mesh in submeshes)
            mesh.Dispose();

        return (merged, null);
    }

    private static unsafe ModelSceneNode WalkNode(
        AssimpNode* node,
        IReadOnlyDictionary<uint, int> meshIndexMap)
    {
        var meshIndices = new List<int>();
        for (uint i = 0; i < node->MNumMeshes; i++)
        {
            var assimpMeshIndex = node->MMeshes[i];
            if (!meshIndexMap.TryGetValue(assimpMeshIndex, out var compactIndex))
                continue;

            meshIndices.Add(compactIndex);
        }

        var children = new List<ModelSceneNode>((int)node->MNumChildren);
        for (uint i = 0; i < node->MNumChildren; i++)
            children.Add(WalkNode(node->MChildren[i], meshIndexMap));

        var name = string.IsNullOrWhiteSpace(node->MName.AsString) ? "Node" : node->MName.AsString;
        var localTransform = ToEngineMatrix(node->MTransformation);
        return new ModelSceneNode(name, meshIndices, children, localTransform);
    }

    private static Matrix4x4 ToEngineMatrix(Matrix4x4 assimpMatrix) =>
        Matrix4x4.Transpose(assimpMatrix);

    private static unsafe Mesh ExtractMesh(AssimpMesh* aiMesh)
    {
        var mesh = new Mesh(aiMesh->MName.AsString);

        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex
            {
                Position = aiMesh->MVertices[i]
            };

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

        mesh.Indices.Capacity = (int)aiMesh->MNumFaces * 3;
        for (uint i = 0; i < aiMesh->MNumFaces; i++)
        {
            var face = aiMesh->MFaces[i];
            // DrawElements(Triangles) groups EBO by 3. Points/lines shift every later triangle.
            AddTriangleFace(mesh.Indices, new ReadOnlySpan<uint>(face.MIndices, (int)face.MNumIndices));
        }

        return mesh;
    }

    internal static void AddTriangleFace(List<uint> indices, ReadOnlySpan<uint> face)
    {
        if (face.Length != 3)
            return;

        indices.Add(face[0]);
        indices.Add(face[1]);
        indices.Add(face[2]);
    }

    private static bool IsGltfPath(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".glb", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase);
    }

    private static uint BuildImportFlags(bool isGltf)
    {
        var flags = PostProcessSteps.Triangulate |
                    PostProcessSteps.SortByPrimitiveType |
                    PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.GenerateNormals |
                    PostProcessSteps.CalculateTangentSpace |
                    PostProcessSteps.OptimizeMeshes;
        // glTF UVs are already OpenGL-style; FlipUVs + stbi vertical flip smears albedo on GLB.
        if (!isGltf)
            flags |= PostProcessSteps.FlipUVs;
        return (uint)flags;
    }

    private unsafe MeshMaterialMerger.ModelMaterialInfo ExtractMaterialInfo(Silk.NET.Assimp.Scene* scene, uint materialIndex,
        string directory, bool isGltf)
    {
        if (materialIndex >= scene->MNumMaterials)
        {
            Logger.Warning(
                "Material index {MaterialIndex} out of range (MNumMaterials={MaterialCount})",
                materialIndex, scene->MNumMaterials);
            return new MeshMaterialMerger.ModelMaterialInfo(null, null, null, 32.0f);
        }

        var aiMaterial = scene->MMaterials[materialIndex];

        // glTF/GLB puts albedo on BASE_COLOR. Assimp often also stuffs the first
        // image (a normal map, if that node is first) into DIFFUSE — using that as
        // color looks like random mosaic UVs.
        var diffuseTexturePath =
            ResolveTexturePath(scene, aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Diffuse, directory);
        // Phong specular ≠ glTF metallic-roughness; binding MR as specular muddies the look.
        var specularTexturePath = isGltf
            ? null
            : ResolveTexturePath(scene, aiMaterial, TextureType.Specular, directory);
        var normalTexturePath = ResolveTexturePath(scene, aiMaterial, TextureType.Normals, directory)
                                ?? ResolveTexturePath(scene, aiMaterial, TextureType.Height, directory);

        var shininess = 32.0f;
        _assimp.GetMaterialFloatArray(aiMaterial, Assimp.MaterialShininess, 0, 0, ref shininess, (uint*)null);
        shininess = shininess > 0 ? shininess : 32.0f;

        return new MeshMaterialMerger.ModelMaterialInfo(diffuseTexturePath, specularTexturePath, normalTexturePath, shininess);
    }

    private unsafe string? ResolveTexturePath(Silk.NET.Assimp.Scene* scene, Material* aiMaterial,
        TextureType textureType,
        string directory)
    {
        if (_assimp.GetMaterialTextureCount(aiMaterial, textureType) == 0)
            return null;

        AssimpString aiPath;
        var result = _assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
        {
            Logger.Warning("GetMaterialTexture failed type={Type} result={Result}", textureType, result);
            return null;
        }
        
        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
        {
            Logger.Warning("Texture type={Type} has empty path", textureType);
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
    
    private static unsafe string? ExtractEmbeddedTextureToCache(Silk.NET.Assimp.Scene* scene, string embeddedRef)
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

        // Compressed image blob (png/jpg/…)
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
        if (bytes is [0xFF, 0xD8, ..])
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
    
    private Texture2D? LoadTexture(string? path, bool sRgb = false)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return textureFactory.Create(path, sRgb);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", path);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _assimp.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
