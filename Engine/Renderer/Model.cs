using System.Diagnostics;
using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Materials;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using File = System.IO.File;

namespace Engine.Renderer;

public class Model : IModel
{
    private static readonly ILogger Logger = Log.ForContext<Model>();

    private readonly ITextureFactory _textureFactory;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;

    public Model(string path, ITextureFactory textureFactory, IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        _textureFactory = textureFactory;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;

        var assimp = Assimp.GetApi();
        _assimp = assimp;
        LoadModel(path);
    }

    private Assimp _assimp;
    private List<Texture2D> _texturesLoaded = new();
    public string Directory { get; protected set; } = string.Empty;
    public List<Mesh> Meshes { get; protected set; } = new();
    private bool _disposed;

    private unsafe void LoadModel(string path)
    {
        var scene = _assimp.ImportFile(path,
            (uint)(PostProcessSteps.Triangulate |
                   PostProcessSteps.GenerateNormals |
                   PostProcessSteps.CalculateTangentSpace |
                   PostProcessSteps.FlipUVs));

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

        Directory = Path.GetDirectoryName(path) ?? string.Empty;
        Logger.Information("Loading model from {Path}, directory: {Directory}", path, Directory);

        ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity);

        Logger.Information("Model loaded: {MeshCount} meshes, {TextureCount} textures",
            Meshes.Count, _texturesLoaded.Count);
    }

    private unsafe void ProcessNode(Node* rootNode, Silk.NET.Assimp.Scene* scene, Matrix4x4 parentTransform)
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
                var mesh = scene->MMeshes[node->MMeshes[i]];
                var processedMesh = ProcessMesh(mesh, scene);
                processedMesh.NodeTransform = worldTransform;
                Meshes.Add(processedMesh);
            }

            for (var i = 0; i < node->MNumChildren; i++)
                stack.Push(((nint)node->MChildren[i], worldTransform));
        }
    }

    private unsafe Mesh ProcessMesh(AssimpMesh* mesh, Silk.NET.Assimp.Scene* scene)
    {
        var vertices = new List<Mesh.Vertex>();
        var indices = new List<uint>();
        var textures = new List<Texture2D>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex();

            vertex.Position = mesh->MVertices[i];

            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];

            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];

            if (mesh->MBitangents != null)
                vertex.Bitangent = mesh->MBitangents[i];

            if (mesh->MTextureCoords[0] != null)
            {
                var texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            vertices.Add(vertex);
        }

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        // Extract PBR material
        PBRMaterial? pbrMaterial = null;
        if (mesh->MMaterialIndex < scene->MNumMaterials)
        {
            var material = scene->MMaterials[mesh->MMaterialIndex];
            pbrMaterial = ExtractPBRMaterial(material, ref textures);
        }

        var result = new Mesh("Model_Mesh", _textureFactory)
        {
            Vertices = vertices,
            Indices = indices,
            Textures = textures,
            Material = pbrMaterial
        };

        // Set albedo as diffuse texture for backward compatibility
        if (pbrMaterial?.AlbedoMap != null)
            result.DiffuseTexture = pbrMaterial.AlbedoMap;

        result.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
        return result;
    }

    private unsafe PBRMaterial ExtractPBRMaterial(Material* mat, ref List<Texture2D> textures)
    {
        var pbrMaterial = new PBRMaterial();

        // Albedo/Diffuse
        var diffuseMaps = LoadMaterialTextures(mat, TextureType.Diffuse);
        if (diffuseMaps.Count > 0)
        {
            pbrMaterial.AlbedoMap = diffuseMaps[0];
            textures.AddRange(diffuseMaps);
        }

        // Try BaseColor for glTF PBR
        if (pbrMaterial.AlbedoMap == null)
        {
            var baseColorMaps = LoadMaterialTextures(mat, TextureType.BaseColor);
            if (baseColorMaps.Count > 0)
            {
                pbrMaterial.AlbedoMap = baseColorMaps[0];
                textures.AddRange(baseColorMaps);
            }
        }

        // Normal maps
        var normalMaps = LoadMaterialTextures(mat, TextureType.Normals);
        if (normalMaps.Count > 0)
        {
            pbrMaterial.NormalMap = normalMaps[0];
            textures.AddRange(normalMaps);
        }
        else
        {
            var heightMaps = LoadMaterialTextures(mat, TextureType.Height);
            if (heightMaps.Count > 0)
            {
                pbrMaterial.NormalMap = heightMaps[0];
                textures.AddRange(heightMaps);
            }
        }

        // Metallic
        var metallicMaps = LoadMaterialTextures(mat, TextureType.Metalness);
        if (metallicMaps.Count > 0)
        {
            pbrMaterial.MetallicMap = metallicMaps[0];
            textures.AddRange(metallicMaps);
        }

        // Roughness
        var roughnessMaps = LoadMaterialTextures(mat, TextureType.DiffuseRoughness);
        if (roughnessMaps.Count > 0)
        {
            pbrMaterial.RoughnessMap = roughnessMaps[0];
            textures.AddRange(roughnessMaps);
        }

        if (pbrMaterial.RoughnessMap == null)
        {
            var specularMaps = LoadMaterialTextures(mat, TextureType.Specular);
            if (specularMaps.Count > 0)
            {
                pbrMaterial.RoughnessMap = specularMaps[0];
                textures.AddRange(specularMaps);
            }
        }

        // Ambient Occlusion
        var aoMaps = LoadMaterialTextures(mat, TextureType.AmbientOcclusion);
        if (aoMaps.Count > 0)
        {
            pbrMaterial.AmbientOcclusionMap = aoMaps[0];
            textures.AddRange(aoMaps);
        }
        else
        {
            var ambientMaps = LoadMaterialTextures(mat, TextureType.Ambient);
            if (ambientMaps.Count > 0)
            {
                pbrMaterial.AmbientOcclusionMap = ambientMaps[0];
                textures.AddRange(ambientMaps);
            }
        }

        // Emissive
        var emissiveMaps = LoadMaterialTextures(mat, TextureType.Emissive);
        if (emissiveMaps.Count > 0)
        {
            pbrMaterial.EmissiveMap = emissiveMaps[0];
            pbrMaterial.EmissiveIntensity = 1.0f;
            textures.AddRange(emissiveMaps);
        }

        ExtractMaterialScalars(mat, pbrMaterial);

        return pbrMaterial;
    }

    private unsafe void ExtractMaterialScalars(Material* mat, PBRMaterial pbrMaterial)
    {
        // Diffuse color
        var diffuseKey = System.Text.Encoding.ASCII.GetBytes("$clr.diffuse\0");
        fixed (byte* keyBytes = diffuseKey)
        {
            float* colorData = stackalloc float[4];
            uint max = 4;
            var result = _assimp.GetMaterialFloatArray(mat, (byte*)keyBytes, 0, 0, colorData, ref max);
            if (result == Return.Success && max >= 3)
            {
                pbrMaterial.AlbedoColor = new Vector4(colorData[0], colorData[1], colorData[2],
                    max >= 4 ? colorData[3] : 1.0f);
            }
        }

        // Metallic factor
        var metallicKey = System.Text.Encoding.ASCII.GetBytes("$mat.metallicFactor\0");
        fixed (byte* keyBytes = metallicKey)
        {
            float metallicValue = 0;
            uint max = 1;
            var result = _assimp.GetMaterialFloatArray(mat, (byte*)keyBytes, 0, 0, &metallicValue, ref max);
            if (result == Return.Success)
                pbrMaterial.Metallic = metallicValue;
        }

        // Roughness factor
        var roughnessKey = System.Text.Encoding.ASCII.GetBytes("$mat.roughnessFactor\0");
        fixed (byte* keyBytes = roughnessKey)
        {
            float roughnessValue = 0.5f;
            uint max = 1;
            var result = _assimp.GetMaterialFloatArray(mat, (byte*)keyBytes, 0, 0, &roughnessValue, ref max);
            if (result == Return.Success)
                pbrMaterial.Roughness = roughnessValue;
        }
    }

    private unsafe List<Texture2D> LoadMaterialTextures(Material* mat, TextureType type)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        var textures = new List<Texture2D>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);

            // Resolve texture path relative to model directory
            string texturePath = path;
            if (!string.IsNullOrEmpty(Directory) && !Path.IsPathRooted(texturePath))
            {
                texturePath = Path.Combine(Directory, texturePath);
            }

            texturePath = texturePath.Replace('\\', '/');

            var skip = false;
            for (var j = 0; j < _texturesLoaded.Count; j++)
            {
                if (_texturesLoaded[j].Path == texturePath)
                {
                    textures.Add(_texturesLoaded[j]);
                    skip = true;
                    break;
                }
            }
            if (!skip)
            {
                if (!File.Exists(texturePath))
                {
                    Logger.Warning("Texture file not found: {Path}", texturePath);
                    continue;
                }

                try
                {
                    var texture = _textureFactory.Create(texturePath);
                    texture.Path = texturePath;
                    textures.Add(texture);
                    _texturesLoaded.Add(texture);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to load texture {Path}: {Error}", texturePath, ex.Message);
                }
            }
        }
        return textures;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var mesh in Meshes)
        {
            mesh?.Dispose();
        }
        Meshes.Clear();

        foreach (var texture in _texturesLoaded)
        {
            texture?.Dispose();
        }
        _texturesLoaded.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~Model()
    {
        if (!_disposed)
        {
            Debug.WriteLine(
                $"MODEL LEAK: Model '{Directory}' not disposed! " +
                $"Meshes: {Meshes.Count}, Textures: {_texturesLoaded.Count}"
            );
        }
    }
#endif
}
