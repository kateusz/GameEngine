using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace Engine.Platform.Assimp;

/// <summary>
/// Model loader implementation using the Assimp library.
/// </summary>
internal sealed class AssimpModelLoader(
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IModelLoader
{
    private readonly Silk.NET.Assimp.Assimp _assimp = Silk.NET.Assimp.Assimp.GetApi();
    private List<Texture2D> _texturesLoaded = [];

    public IReadOnlyList<string> SupportedExtensions { get; } =
    [
        ".obj", ".fbx", ".gltf", ".glb", ".dae", ".3ds", ".blend"
    ];

    public IModel Load(string path)
    {
        _texturesLoaded = [];
        var model = new Model();
        LoadModelInternal(path, model);
        return model;
    }

    private unsafe void LoadModelInternal(string path, Model model)
    {
        var scene = _assimp.ImportFile(path, (uint)PostProcessSteps.Triangulate);

        if (scene == null || scene->MFlags == Silk.NET.Assimp.Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException($"Failed to load model '{path}': {error}");
        }

        model.Directory = path;
        ProcessNode(scene->MRootNode, scene, model);
    }

    private unsafe void ProcessNode(Node* node, Silk.NET.Assimp.Scene* scene, Model model)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            model.Meshes.Add(ProcessMesh(mesh, scene));
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene, model);
        }
    }

    private unsafe Renderer.Mesh ProcessMesh(AssimpMesh* mesh, Silk.NET.Assimp.Scene* scene)
    {
        var vertices = new List<Renderer.Mesh.Vertex>();
        var indices = new List<uint>();
        var textures = new List<Texture2D>();

        // Walk through each of the mesh's vertices
        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Renderer.Mesh.Vertex
            {
                Position = mesh->MVertices[i],
                Normal = mesh->MNormals != null ? mesh->MNormals[i] : Vector3.Zero,
                TexCoord = Vector2.Zero
            };

            // Texture coordinates
            if (mesh->MTextureCoords[0] != null)
            {
                var texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            vertices.Add(vertex);
        }

        // Walk through each face and retrieve vertex indices
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        // Process materials
        var material = scene->MMaterials[mesh->MMaterialIndex];

        // Diffuse maps
        var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
        if (diffuseMaps.Count > 0)
            textures.AddRange(diffuseMaps);

        // Specular maps
        var specularMaps = LoadMaterialTextures(material, TextureType.Specular);
        if (specularMaps.Count > 0)
            textures.AddRange(specularMaps);

        // Normal maps
        var normalMaps = LoadMaterialTextures(material, TextureType.Height);
        if (normalMaps.Count > 0)
            textures.AddRange(normalMaps);

        // Height maps
        var heightMaps = LoadMaterialTextures(material, TextureType.Ambient);
        if (heightMaps.Count > 0)
            textures.AddRange(heightMaps);

        var result = new Renderer.Mesh("Model_Mesh", textureFactory)
        {
            Vertices = vertices,
            Indices = indices,
            Textures = textures
        };
        result.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        return result;
    }

    private unsafe List<Texture2D> LoadMaterialTextures(Material* mat, TextureType type)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        var textures = new List<Texture2D>();

        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);

            var skip = false;
            for (var j = 0; j < _texturesLoaded.Count; j++)
            {
                if (_texturesLoaded[j].Path == path)
                {
                    textures.Add(_texturesLoaded[j]);
                    skip = true;
                    break;
                }
            }

            if (!skip)
            {
                var texture = textureFactory.Create(path);
                texture.Path = path;
                textures.Add(texture);
                _texturesLoaded.Add(texture);
            }
        }

        return textures;
    }
}
