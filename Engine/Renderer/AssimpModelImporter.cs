using System.Numerics;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

internal sealed class AssimpModelImporter(Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();

    public List<ModelSubmesh> Import(string path)
    {
        var submeshes = new List<ModelSubmesh>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.FlipUVs |
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

    private unsafe Mesh ExtractMesh(Silk.NET.Assimp.Mesh* aiMesh)
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

        for (uint i = 0; i < aiMesh->MNumFaces; i++)
        {
            var face = aiMesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                mesh.Indices.Add(face.MIndices[j]);
        }

        return mesh;
    }

    private unsafe MeshMaterial ExtractMaterial(
        Silk.NET.Assimp.Scene* scene,
        uint materialIndex,
        string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];

        var albedoPath = ResolveTexturePath(aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Diffuse, directory);

        var mrPath = ResolveTexturePath(aiMaterial, TextureType.GltfMetallicRoughness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Metalness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.DiffuseRoughness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Specular, directory);

        var normalPath = ResolveTexturePath(aiMaterial, TextureType.Normals, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Height, directory);

        var material = new MeshMaterial
        {
            AlbedoTexturePath = albedoPath,
            MetallicRoughnessTexturePath = mrPath,
            NormalTexturePath = normalPath
        };

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

        return material;
    }

    private unsafe bool TryGetFloat(Silk.NET.Assimp.Material* aiMaterial, string key, out float value)
    {
        value = 0f;
        var result = assimp.GetMaterialFloatArray(aiMaterial, key, 0, 0, ref value, (uint*)null);
        return result == Return.Success;
    }

    private unsafe string? ResolveTexturePath(
        Silk.NET.Assimp.Material* aiMaterial,
        TextureType textureType,
        string directory)
    {
        if (assimp.GetMaterialTextureCount(aiMaterial, textureType) == 0)
            return null;

        AssimpString aiPath;
        var result = assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
            return null;

        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
            return null;

        if (!Path.IsPathRooted(texturePath))
            texturePath = Path.Combine(directory, texturePath);

        texturePath = texturePath.Replace('\\', '/');
        return System.IO.File.Exists(texturePath) ? texturePath : null;
    }
}
