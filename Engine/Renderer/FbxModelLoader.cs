using System.Numerics;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

public record FbxModelResult(List<Mesh> Meshes, List<MeshMaterial> Materials, string Directory);

internal sealed class FbxModelLoader(ITextureFactory textureFactory, Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<FbxModelLoader>();

    public FbxModelResult Load(string path)
    {
        var meshes = new List<Mesh>();
        var materials = new List<MeshMaterial>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                   PostProcessSteps.GenerateNormals |
                                   PostProcessSteps.CalculateTangentSpace |
                                   PostProcessSteps.FlipUVs);

        unsafe
        {
            var scene = assimp.ImportFile(path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error("Failed to load FBX model: {Path}", path);
                return new FbxModelResult(meshes, materials, directory);
            }

            Logger.Information("Loading model from {Path}, directory: {Directory}", path, directory);

            ExtractLights(scene);
            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, directory, meshes, materials);

            Logger.Information("Model loaded: {MeshCount} meshes", meshes.Count);

            assimp.ReleaseImport(scene);
        }

        return new FbxModelResult(meshes, materials, directory);
    }

    private unsafe void ProcessNode(Node* rootNode, Silk.NET.Assimp.Scene* scene, Matrix4x4 parentTransform,
        string directory, List<Mesh> meshes, List<MeshMaterial> materials)
    {
        if (rootNode == null) return;

        var stack = new Stack<(nint Node, Matrix4x4 ParentTransform)>();
        stack.Push(((nint)rootNode, parentTransform));

        while (stack.Count > 0)
        {
            var (nodePtr, parent) = stack.Pop();
            var node = (Node*)nodePtr;
            if (node == null) continue;

            var nodeMatrix = node->MTransformation;
            // Assimp stores transforms in row-major order, System.Numerics uses column-major
            var localTransform = Matrix4x4.Transpose(new Matrix4x4(
                nodeMatrix.M11, nodeMatrix.M12, nodeMatrix.M13, nodeMatrix.M14,
                nodeMatrix.M21, nodeMatrix.M22, nodeMatrix.M23, nodeMatrix.M24,
                nodeMatrix.M31, nodeMatrix.M32, nodeMatrix.M33, nodeMatrix.M34,
                nodeMatrix.M41, nodeMatrix.M42, nodeMatrix.M43, nodeMatrix.M44));

            var worldTransform = localTransform * parent;

            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var aiMesh = scene->MMeshes[node->MMeshes[i]];
                var mesh = ExtractMesh(aiMesh);
                mesh.NodeTransform = worldTransform;
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                meshes.Add(mesh);
                materials.Add(material);
            }

            for (var i = 0; i < node->MNumChildren; i++)
                stack.Push(((nint)node->MChildren[i], worldTransform));
        }
    }

    private unsafe void ExtractLights(Silk.NET.Assimp.Scene* scene)
    {
        for (var i = 0; i < scene->MNumLights; i++)
        {
            var light = scene->MLights[i];
            var name = light->MName.AsString;
            var type = light->MType;
            var pos = light->MPosition;
            var dir = light->MDirection;
            var color = light->MColorDiffuse;

            Logger.Information(
                "FBX Light found: \"{Name}\"\n  Type: {Type}\n  Position: ({PX:F1}, {PY:F1}, {PZ:F1})\n  Direction: ({DX:F1}, {DY:F1}, {DZ:F1})\n  Color: ({CR:F2}, {CG:F2}, {CB:F2})\n  AttenuationConstant: {AC:F2}",
                name, type,
                pos.X, pos.Y, pos.Z,
                dir.X, dir.Y, dir.Z,
                color.X, color.Y, color.Z,
                light->MAttenuationConstant);
        }
    }

    private unsafe Mesh ExtractMesh(Silk.NET.Assimp.Mesh* aiMesh)
    {
        var name = aiMesh->MName.AsString;
        var mesh = new Mesh(name);

        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex();

            vertex.Position = aiMesh->MVertices[i];

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

    private unsafe MeshMaterial ExtractMaterial(Silk.NET.Assimp.Scene* scene, uint materialIndex, string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];
        var material = new MeshMaterial
        {
            DiffuseTexture = LoadMaterialTexture(aiMaterial, TextureType.Diffuse, directory, out var diffusePath)
                             ?? textureFactory.GetWhiteTexture(),
            DiffuseTexturePath = diffusePath,
            SpecularTexture = LoadMaterialTexture(aiMaterial, TextureType.Specular, directory, out var specularPath)
                              ?? textureFactory.GetBlackTexture(),
            SpecularTexturePath = specularPath
        };

        var normalTexture = LoadMaterialTexture(aiMaterial, TextureType.Normals, directory, out var normalPath);
        
        if (normalTexture == null)
            normalTexture = LoadMaterialTexture(aiMaterial, TextureType.Height, directory, out normalPath);
        
        material.NormalTexture = normalTexture ?? textureFactory.GetFlatNormalTexture();
        material.NormalTexturePath = normalPath;

        var shininess = 32.0f;
        assimp.GetMaterialFloatArray(aiMaterial, Assimp.MaterialShininess, 0, 0, ref shininess, (uint*)null);
        material.Shininess = shininess > 0 ? shininess : 32.0f;

        return material;
    }

    private unsafe Texture2D? LoadMaterialTexture(Silk.NET.Assimp.Material* aiMaterial, TextureType textureType,
        string directory, out string? resolvedPath)
    {
        resolvedPath = null;
        var count = assimp.GetMaterialTextureCount(aiMaterial, textureType);
        if (count == 0)
            return null;

        AssimpString aiPath;
        var result = assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
            return null;

        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
            return null;

        // Keep the full relative path from Assimp and combine with model directory
        if (!Path.IsPathRooted(texturePath))
            texturePath = Path.Combine(directory, texturePath);

        texturePath = texturePath.Replace('\\', '/');

        if (!System.IO.File.Exists(texturePath))
        {
            Logger.Warning("Texture not found: {FullPath}", texturePath);
            return null;
        }

        resolvedPath = texturePath;

        try
        {
            return textureFactory.Create(texturePath);
        }
        catch (Exception ex)
        {
            Logger.Warning("Failed to load texture {Path}: {Error}", texturePath, ex.Message);
            return null;
        }
    }
}
