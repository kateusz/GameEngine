using System.Numerics;
using Engine.Renderer.Textures;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace Engine.Renderer;

public class Model : IModel
{
    public Model(string path)
    {
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
        List<Mesh.Vertex> vertices = new List<Mesh.Vertex>();
        List<uint> indices = new List<uint>();
        List<Texture2D> textures = new List<Texture2D>();

        // walk through each of the mesh's vertices
        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            Mesh.Vertex vertex = new Mesh.Vertex();
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
                Vector3 texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            vertices.Add(vertex);
        }

        // now wak through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            // retrieve all indices of the face and store them in the indices vector
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        // process materials
        Material* material = scene->MMaterials[mesh->MMaterialIndex];
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
        var result = new Mesh
        {
            Vertices = vertices,
            Indices = indices,
            Textures = textures
        };
        result.Initialize();
        return result;
    }

    private unsafe List<Texture2D> LoadMaterialTextures(Material* mat, TextureType type)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        List<Texture2D> textures = new List<Texture2D>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            bool skip = false;
            for (int j = 0; j < _texturesLoaded.Count; j++)
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
                var texture = TextureFactory.Create(path);
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

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Dispose managed resources

            // Dispose all meshes
            foreach (var mesh in Meshes)
            {
                mesh?.Dispose();
            }
            Meshes.Clear();

            // Dispose all loaded textures
            foreach (var texture in _texturesLoaded)
            {
                texture?.Dispose();
            }
            _texturesLoaded.Clear();
        }

        // No unmanaged resources to clean up directly
        // (Assimp handles are managed by Silk.NET)

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~Model()
    {
        Dispose(disposing: false);
    }
}