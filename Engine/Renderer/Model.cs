using System.Diagnostics;
using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace Engine.Renderer;

public class Model : IModel
{
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
    private bool _disposed = false;
        
    private unsafe void LoadModel(string path)
    {
        var scene = _assimp.ImportFile(path, (uint)PostProcessSteps.Triangulate);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

        Directory = path;

        ProcessNode(scene->MRootNode, scene);
    }

    private unsafe void ProcessNode(Node* node, Silk.NET.Assimp.Scene* scene)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            Meshes.Add(ProcessMesh(mesh, scene));

        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene);
        }
    }

    private unsafe Mesh ProcessMesh(AssimpMesh* mesh, Silk.NET.Assimp.Scene* scene)
    {
        // data to fill
        var vertices = new List<Mesh.Vertex>();
        var indices = new List<uint>();
        var textures = new List<Texture2D>();

        // walk through each of the mesh's vertices
        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex();
            //vertex.BoneIds = new int[Mesh.Vertex.MAX_BONE_INFLUENCE];
            //vertex.Weights = new float[Mesh.Vertex.MAX_BONE_INFLUENCE];

            vertex.Position = mesh->MVertices[i];

            // normals
            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];
            // tangent
            // if (mesh->MTangents != null)
            //     vertex.Tangent = mesh->MTangents[i];
            // // bitangent
            // if (mesh->MBitangents != null)
            //     vertex.Bitangent = mesh->MBitangents[i];

            // texture coordinates
            if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
            {
                // a vertex can contain up to 8 different texture coordinates. We thus make the assumption that we won't 
                // use models where a vertex can have multiple texture coordinates so we always take the first set (0).
                var texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            vertices.Add(vertex);
        }

        // now wak through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            // retrieve all indices of the face and store them in the indices vector
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        // process materials
        var material = scene->MMaterials[mesh->MMaterialIndex];
        // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
        // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
        // Same applies to other texture as the following list summarizes:
        // diffuse: texture_diffuseN
        // specular: texture_specularN
        // normal: texture_normalN

        // 1. diffuse maps
        var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
        if (diffuseMaps.Any())
            textures.AddRange(diffuseMaps);
        // 2. specular maps
        var specularMaps = LoadMaterialTextures(material, TextureType.Specular);
        if (specularMaps.Any())
            textures.AddRange(specularMaps);
        // 3. normal maps
        var normalMaps = LoadMaterialTextures(material, TextureType.Height);
        if (normalMaps.Any())
            textures.AddRange(normalMaps);
        // 4. height maps
        var heightMaps = LoadMaterialTextures(material, TextureType.Ambient);
        if (heightMaps.Any())
            textures.AddRange(heightMaps);

        // return a mesh object created from the extracted mesh data
        //var result = new Mesh(, BuildIndices(indices), textures);
        var result = new Mesh("Model_Mesh", _textureFactory)
        {
            Vertices = vertices,
            Indices = indices,
            Textures = textures
        };
        result.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
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
                var texture = _textureFactory.Create(path);
                texture.Path = path;
                textures.Add(texture);
                _texturesLoaded.Add(texture);
            }
        }
        return textures;
    }

    private float[] BuildVertices(List<Mesh.Vertex> vertexCollection)
    {
        var vertices = new List<float>();

        foreach (var vertex in vertexCollection)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
            vertices.Add(vertex.TexCoord.X);
            vertices.Add(vertex.TexCoord.Y);
        }

        return vertices.ToArray();
    }

    private uint[] BuildIndices(List<uint> indices)
    {
        return indices.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Dispose all meshes
        foreach (var mesh in Meshes)
        {
            mesh?.Dispose();
        }
        Meshes.Clear();

        // Factory owns texture lifetime; just release our references
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