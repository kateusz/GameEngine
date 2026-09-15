using System.Numerics;
using Engine.Renderer.Meshes;
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

    private readonly Assimp _assimp = Assimp.GetApi();
    private bool _disposed;

    public (IReadOnlyList<Mesh> Submeshes, IReadOnlyList<MeshMaterial> Materials, ModelSceneNode? SceneGraph) Import(string path)
    {
        var submeshes = new List<Mesh>();
        var pendingMaterials = new List<MaterialInfo>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.SortByPrimitiveType |
                                  PostProcessSteps.JoinIdenticalVertices |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.FlipUVs);

        ModelSceneNode? sceneGraph = null;

        unsafe
        {
            var scene = _assimp.ImportFile(path, flags);
            if (scene == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, _assimp.GetErrorStringS());
                return ([], [], null);
            }

            try
            {
                if ((scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
                {
                    Logger.Error(
                        "Failed to import model path={Path} assimpError={AssimpError}",
                        path, _assimp.GetErrorStringS());
                    return ([], [], null);
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

                    var meshName = aiMesh->MName.AsString;
                    if (IsUnrealCollisionMesh(meshName))
                    {
                        Logger.Debug("Skipping Unreal collision mesh name={Name}", meshName);
                        continue;
                    }

                    var mesh = ExtractMesh(aiMesh);
                    if (mesh.Indices.Count == 0)
                    {
                        Logger.Debug("Skipping mesh with no triangles name={Name}", mesh.Name);
                        continue;
                    }

                    var material = ExtractMaterialInfo(scene, aiMesh->MMaterialIndex, directory);
                    pendingMaterials.Add(material);
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

        var materials = new List<MeshMaterial>(pendingMaterials.Count);
        foreach (var material in pendingMaterials)
            materials.Add(LoadMaterial(material));

        return (submeshes, materials, sceneGraph);
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

    internal static bool IsUnrealCollisionMesh(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return name.StartsWith("UCX_", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("UBX_", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("USP_", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("UCP_", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct TextureSlot(string? Path, byte[]? Encoded);

    private readonly record struct MaterialInfo(
        TextureSlot Diffuse,
        TextureSlot Specular,
        TextureSlot Normal,
        float Shininess);

    private unsafe MaterialInfo ExtractMaterialInfo(Silk.NET.Assimp.Scene* scene, uint materialIndex,
        string directory)
    {
        if (materialIndex >= scene->MNumMaterials)
        {
            Logger.Warning(
                "Material index {MaterialIndex} out of range (MNumMaterials={MaterialCount})",
                materialIndex, scene->MNumMaterials);
            return new MaterialInfo(default, default, default, 32.0f);
        }

        var aiMaterial = scene->MMaterials[materialIndex];

        // glTF/GLB puts albedo on BASE_COLOR. Assimp often also stuffs the first
        // image (a normal map, if that node is first) into DIFFUSE — using that as
        // color looks like random mosaic UVs.
        var diffuse =
            ResolveTexture(scene, aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexture(scene, aiMaterial, TextureType.Diffuse, directory)
            ?? default;
        // Phong specular ≠ glTF metallic-roughness. Leave ORM maps out of this slot.
        var specular = ResolveTexture(scene, aiMaterial, TextureType.Specular, directory) ?? default;
        var normal = ResolveTexture(scene, aiMaterial, TextureType.Normals, directory)
                     ?? ResolveTexture(scene, aiMaterial, TextureType.Height, directory)
                     ?? default;

        if (diffuse.Path == null && diffuse.Encoded == null && normal.Path != null)
        {
            var inferred = AssimpTexturePath.InferAlbedoFromNormal(normal.Path, directory);
            if (inferred != null)
            {
                Logger.Debug(
                    "Texture type=Diffuse inferred from normal {Normal} → {Path}",
                    normal.Path, inferred);
                diffuse = new TextureSlot(inferred, null);
            }
        }

        var shininess = 32.0f;
        _assimp.GetMaterialFloatArray(aiMaterial, Assimp.MaterialShininess, 0, 0, ref shininess, (uint*)null);
        shininess = shininess > 0 ? shininess : 32.0f;

        return new MaterialInfo(diffuse, specular, normal, shininess);
    }

    private unsafe TextureSlot? ResolveTexture(Silk.NET.Assimp.Scene* scene, Material* aiMaterial,
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

        if (texturePath.StartsWith('*'))
        {
            var bytes = ExtractEmbeddedBytes(scene, texturePath);
            if (bytes == null)
            {
                Logger.Warning("Failed to extract embedded texture type={Type} ref={Ref}", textureType, texturePath);
                return null;
            }

            Logger.Debug("Texture type={Type} extracted embedded {Ref} ({Bytes} bytes)", textureType, texturePath,
                bytes.Length);
            return new TextureSlot(null, bytes);
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
        return new TextureSlot(resolved, null);
    }

    private static unsafe byte[]? ExtractEmbeddedBytes(Silk.NET.Assimp.Scene* scene, string embeddedRef)
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

        return bytes;
    }

    private MeshMaterial LoadMaterial(MaterialInfo material) =>
        new(
            LoadSlot(material.Diffuse, sRgb: true),
            LoadSlot(material.Specular),
            LoadSlot(material.Normal),
            material.Shininess);

    private Texture2D? LoadSlot(TextureSlot slot, bool sRgb = false)
    {
        if (slot.Encoded != null)
        {
            try
            {
                return textureFactory.CreateFromEncoded(slot.Encoded, sRgb);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load embedded texture");
                return null;
            }
        }

        if (string.IsNullOrEmpty(slot.Path))
            return null;

        try
        {
            return textureFactory.Create(slot.Path, sRgb);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", slot.Path);
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
