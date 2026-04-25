using System.Numerics;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

public record FbxModelResult(List<Mesh> Meshes, List<PbrMaterial> Materials, List<ModelLightData> Lights, string Directory);

internal sealed class FbxModelLoader(ITextureFactory textureFactory, Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<FbxModelLoader>();

    public FbxModelResult Load(string path)
    {
        var meshes = new List<Mesh>();
        var materials = new List<PbrMaterial>();
        var lights = new List<ModelLightData>();
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
                return new FbxModelResult(meshes, materials, lights, directory);
            }

            Logger.Information("Loading model from {Path}, directory: {Directory}", path, directory);

            lights = ExtractLights(scene);
            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, directory, meshes, materials);

            Logger.Information("Model loaded: {MeshCount} meshes, {LightCount} lights", meshes.Count, lights.Count);

            assimp.ReleaseImport(scene);
        }

        return new FbxModelResult(meshes, materials, lights, directory);
    }

    private unsafe void ProcessNode(Node* rootNode, Silk.NET.Assimp.Scene* scene, Matrix4x4 parentTransform,
        string directory, List<Mesh> meshes, List<PbrMaterial> materials)
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

    private unsafe List<ModelLightData> ExtractLights(Silk.NET.Assimp.Scene* scene)
    {
        var result = new List<ModelLightData>();
        if (scene->MNumLights == 0)
            return result;

        // Build name → raw light metadata map (type + color from the light struct)
        var lightMeta = new Dictionary<string, (LightSourceType Type, Vector3 Color)>(StringComparer.Ordinal);
        for (var i = 0; i < scene->MNumLights; i++)
        {
            var light = scene->MLights[i];
            var name = light->MName.AsString;
            var raw = light->MColorDiffuse;
            // glTF KHR_lights_punctual stores color*intensity in MColorDiffuse.
            // If Assimp reports black (extension not read / intensity=0), fall back to white.
            var color = new Vector3(raw.X, raw.Y, raw.Z);
            if (color == Vector3.Zero)
                color = Vector3.One;

            lightMeta[name] = (light->MType, color);
        }

        // Traverse scene graph: accumulate world transforms and match nodes to lights.
        var stack = new Stack<(nint NodePtr, Matrix4x4 ParentWorld)>();
        stack.Push(((nint)scene->MRootNode, Matrix4x4.Identity));

        while (stack.Count > 0)
        {
            var (nodePtr, parentWorld) = stack.Pop();
            var node = (Node*)nodePtr;
            if (node == null) continue;

            var m = node->MTransformation;
            var local = Matrix4x4.Transpose(new Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44));
            var world = local * parentWorld;

            var nodeName = node->MName.AsString;
            if (lightMeta.TryGetValue(nodeName, out var meta))
            {
                // Position = translation of world matrix
                var position = new Vector3(world.M41, world.M42, world.M43);
                // glTF lights point along -Z in local space
                var direction = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0f, 0f, -1f), world));

                var lightType = meta.Type switch
                {
                    LightSourceType.Directional => ModelLightType.Directional,
                    LightSourceType.Point => ModelLightType.Point,
                    LightSourceType.Spot => ModelLightType.Spot,
                    _ => ModelLightType.Point
                };

                result.Add(new ModelLightData(nodeName, lightType, position, direction, meta.Color, 1.0f));
                Logger.Information(
                    "Light '{Name}' ({Type}) — pos: {Position}, dir: {Direction}, color: {Color}",
                    nodeName, lightType, position, direction, meta.Color);
            }

            for (var i = 0; i < node->MNumChildren; i++)
                stack.Push(((nint)node->MChildren[i], world));
        }

        return result;
    }

    private unsafe Mesh ExtractMesh(Silk.NET.Assimp.Mesh* aiMesh)
    {
        var name = aiMesh->MName.AsString;
        var mesh = new Mesh(name);

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

    private unsafe PbrMaterial ExtractMaterial(Silk.NET.Assimp.Scene* scene, uint materialIndex, string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];
        var material = new PbrMaterial();

        // BaseColor: prefer glTF BaseColor slot, fall back to legacy Diffuse for FBX
        material.BaseColorTexture =
            LoadMaterialTexture(aiMaterial, TextureType.BaseColor, directory, out var bcPath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.Diffuse, directory, out bcPath)
            ?? textureFactory.GetWhiteTexture();
        material.BaseColorTexturePath = bcPath;

        // MetallicRoughness: Assimp exposes glTF packed texture via Unknown or Metalness slot
        var mrTexture =
            LoadMaterialTexture(aiMaterial, TextureType.Metalness, directory, out var mrPath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.DiffuseRoughness, directory, out mrPath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.Unknown, directory, out mrPath);
        material.MetallicRoughnessTexture = mrTexture;
        material.MetallicRoughnessTexturePath = mrPath;

        // Normal map
        material.NormalTexture =
            LoadMaterialTexture(aiMaterial, TextureType.Normals, directory, out var nPath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.Height, directory, out nPath)
            ?? textureFactory.GetFlatNormalTexture();
        material.NormalTexturePath = nPath;

        // AO
        material.AoTexture =
            LoadMaterialTexture(aiMaterial, TextureType.AmbientOcclusion, directory, out var aoPath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.Lightmap, directory, out aoPath);
        material.AoTexturePath = aoPath;

        // Emissive
        material.EmissiveTexture =
            LoadMaterialTexture(aiMaterial, TextureType.EmissionColor, directory, out var ePath)
            ?? LoadMaterialTexture(aiMaterial, TextureType.Emissive, directory, out ePath);
        material.EmissiveTexturePath = ePath;
        if (material.EmissiveTexture != null)
            material.EmissiveFactor = System.Numerics.Vector3.One;

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