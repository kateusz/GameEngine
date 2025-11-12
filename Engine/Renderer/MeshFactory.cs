using System.Numerics;
using Serilog;

namespace Engine.Renderer.Models;

public static class MeshFactory
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(MeshFactory));
    private static readonly Dictionary<string, Mesh> _loadedMeshes = new();
    private static readonly Dictionary<string, Model> _loadedModels = new();
    
    // In MeshFactory.cs
    public static Mesh Create(string objFilePath)
    {
        // Check if we've already loaded this mesh
        if (_loadedMeshes.TryGetValue(objFilePath, out var existingMesh))
        {
            return existingMesh;
        }

        // Load the model and keep it alive to prevent disposal of shared textures
        var model = new Model(objFilePath);
        var mesh = model.Meshes.First();

        // Log information about mesh size
        if (mesh.Vertices.Count > 50000 || mesh.Indices.Count > 100000)
        {
            Logger.Warning("Large mesh loaded from {ObjFilePath}: {VertexCount} vertices, {IndexCount} indices",
                objFilePath, mesh.Vertices.Count, mesh.Indices.Count);
        }

        // Cache both the mesh and the model to prevent resource disposal
        _loadedMeshes[objFilePath] = mesh;
        _loadedModels[objFilePath] = model;
        return mesh;
    }
    
    public static Mesh CreateCube()
    {
        var mesh = new Mesh("Cube");
        
        // Define vertices
        float size = 0.5f;
        
        // Front face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), Vector3.UnitZ, new Vector2(0.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitZ, new Vector2(1.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitZ, new Vector2(1.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitZ, new Vector2(0.0f, 1.0f)));
        
        // Back face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitZ, new Vector2(1.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitZ, new Vector2(1.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), -Vector3.UnitZ, new Vector2(0.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitZ, new Vector2(0.0f, 0.0f)));
        
        // Top face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), Vector3.UnitY, new Vector2(0.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitY, new Vector2(0.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitY, new Vector2(1.0f, 0.0f)));
        
        // Bottom face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitY, new Vector2(0.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitY, new Vector2(1.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), -Vector3.UnitY, new Vector2(1.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitY, new Vector2(0.0f, 0.0f)));
        
        // Right face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), Vector3.UnitX, new Vector2(0.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitX, new Vector2(0.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitX, new Vector2(1.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitX, new Vector2(1.0f, 0.0f)));
        
        // Left face
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitX, new Vector2(1.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitX, new Vector2(0.0f, 0.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), -Vector3.UnitX, new Vector2(0.0f, 1.0f)));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitX, new Vector2(1.0f, 1.0f)));
        
        // Define indices (6 faces, 2 triangles per face, 3 indices per triangle)
        // Front face
        mesh.Indices.AddRange([0, 1, 2, 2, 3, 0]);
        
        // Back face
        mesh.Indices.AddRange([4, 5, 6, 6, 7, 4]);
        
        // Top face
        mesh.Indices.AddRange([8, 9, 10, 10, 11, 8]);
        
        // Bottom face
        mesh.Indices.AddRange([12, 13, 14, 14, 15, 12]);
        
        // Right face
        mesh.Indices.AddRange([16, 17, 18, 18, 19, 16]);
        
        // Left face
        mesh.Indices.AddRange([20, 21, 22, 22, 23, 20]);

        mesh.Initialize();
        return mesh;
    }

    /// <summary>
    /// Clears all cached meshes and disposes loaded models to free GPU resources.
    /// Should be called when shutting down or when clearing the asset cache.
    /// </summary>
    public static void Clear()
    {
        // Dispose all loaded models (which will dispose their meshes and textures)
        foreach (var model in _loadedModels.Values)
        {
            model?.Dispose();
        }

        _loadedModels.Clear();
        _loadedMeshes.Clear();

        Logger.Information("MeshFactory cache cleared and resources disposed");
    }
}