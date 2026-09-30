using System.Numerics;
using System.Text;
using Engine.Renderer.Models.RuntimeMesh;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpNode = Silk.NET.Assimp.Node;
using Mesh = Engine.Renderer.Meshes.Mesh;

namespace Engine.Renderer.Models;

internal sealed class AssimpModelImporter : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();
    
    private readonly Assimp _assimp = Assimp.GetApi();
    private bool _disposed;

    public SourceModel? ImportSource(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(normalizedPath) ?? string.Empty;
        var textureSidecar = new ModelSourceTextureSidecar(normalizedPath);
        var submeshes = new List<SourceSubmesh>();
        ModelSceneNode? sceneGraph;

        unsafe
        {
            var scene = _assimp.ImportFile(normalizedPath, RuntimeMeshFormat.AssimpPostProcessFlags);
            if (scene == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    normalizedPath, _assimp.GetErrorStringS());
                return null;
            }

            try
            {
                if ((scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
                {
                    Logger.Error(
                        "Failed to import model path={Path} assimpError={AssimpError}",
                        normalizedPath, _assimp.GetErrorStringS());
                    return null;
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

                    var submesh = ExtractSourceSubmesh(aiMesh);
                    if (submesh == null)
                        continue;

                    // glTF TEXCOORD origin is the top of the image. Assimp's FlipUVs leaves those
                    // values unchanged. TextureFileDecoder flips the bitmap for OpenGL, so mirror V
                    // or a packed atlas samples the wrong row.
                    if (IsGltf(normalizedPath))
                        FlipTexCoordV(submesh);

                    var material = ExtractMaterialInfo(scene, aiMesh->MMaterialIndex, directory, textureSidecar);
                    submesh.Metallic = material.MetallicFactor;
                    submesh.Roughness = material.RoughnessFactor;
                    submesh.BaseColorFactor = material.BaseColorFactor;
                    submesh.DiffusePath = material.DiffusePath ?? string.Empty;
                    submesh.NormalPath = material.NormalPath ?? string.Empty;
                    submesh.MetallicRoughnessPath = material.MetallicRoughnessPath ?? string.Empty;
                    submesh.OcclusionPath = material.OcclusionPath ?? string.Empty;
                    Logger.Debug(
                        "PBR set for mesh={Mesh} model={Path}: metallic={Metallic} roughness={Roughness} baseColor={BaseColor} " +
                        "maps albedo={Albedo} normal={Normal} metallicRoughness={Mr} occlusion={Occlusion} " +
                        "hasAlbedo={HasAlbedo} hasNormal={HasNormal} hasMr={HasMr} hasOcclusion={HasOcclusion}",
                        submesh.Name,
                        normalizedPath,
                        submesh.Metallic,
                        submesh.Roughness,
                        submesh.BaseColorFactor,
                        submesh.DiffusePath,
                        submesh.NormalPath,
                        submesh.MetallicRoughnessPath,
                        submesh.OcclusionPath,
                        !string.IsNullOrEmpty(submesh.DiffusePath),
                        !string.IsNullOrEmpty(submesh.NormalPath),
                        !string.IsNullOrEmpty(submesh.MetallicRoughnessPath),
                        !string.IsNullOrEmpty(submesh.OcclusionPath));
                    meshIndexMap[i] = submeshes.Count;
                    submeshes.Add(submesh);
                }

                if (submeshes.Count == 0)
                    return null;

                var pendingLights = CollectLights(scene);
                if (pendingLights.Skipped > 0)
                {
                    Logger.Debug(
                        "Skipped unsupported lights count={Count} path={Path}",
                        pendingLights.Skipped, normalizedPath);
                }

                sceneGraph = WalkNode(scene->MRootNode, meshIndexMap, pendingLights, Matrix4x4.Identity);
                sceneGraph = WithUnmatchedLights(sceneGraph, pendingLights);
            }
            finally
            {
                _assimp.ReleaseImport(scene);
            }
        }

        var (nodes, lights) = RuntimeMeshSceneGraph.Flatten(sceneGraph);
        return new SourceModel(submeshes, nodes, lights);
    }

    private static unsafe ModelSceneNode WalkNode(
        AssimpNode* node,
        IReadOnlyDictionary<uint, int> meshIndexMap,
        PendingLights pending,
        Matrix4x4 parentWorld)
    {
        var meshIndices = new List<int>();
        for (uint i = 0; i < node->MNumMeshes; i++)
        {
            var assimpMeshIndex = node->MMeshes[i];
            if (!meshIndexMap.TryGetValue(assimpMeshIndex, out var compactIndex))
                continue;

            meshIndices.Add(compactIndex);
        }

        var name = string.IsNullOrWhiteSpace(node->MName.AsString) ? "Node" : node->MName.AsString;
        var nodeLocal = ToEngineMatrix(node->MTransformation);
        var world = nodeLocal * parentWorld;
        var local = nodeLocal;
        ImportedLight? imported = null;
        if (pending.TryTake(name, out var raw))
            imported = ConvertLight(node, raw, world, meshIndices.Count > 0, ref local);

        var children = new List<ModelSceneNode>((int)node->MNumChildren);
        for (uint i = 0; i < node->MNumChildren; i++)
            children.Add(WalkNode(node->MChildren[i], meshIndexMap, pending, world));

        return new ModelSceneNode(name, meshIndices, children, local, imported);
    }

    private static unsafe PendingLights CollectLights(Silk.NET.Assimp.Scene* scene)
    {
        var pending = new PendingLights();
        for (uint i = 0; i < scene->MNumLights; i++)
        {
            var light = scene->MLights[i];
            if (light == null)
                continue;

            if (light->MType == LightSourceType.Point ||
                light->MType == LightSourceType.Directional)
            {
                pending.Add(light->MName.AsString, new RawLight(
                    light->MName.AsString,
                    light->MType,
                    light->MColorDiffuse,
                    light->MPosition,
                    light->MDirection));
                continue;
            }

            pending.Skip();
        }

        return pending;
    }

    private static unsafe ImportedLight? ConvertLight(
        AssimpNode* node,
        RawLight raw,
        Matrix4x4 world,
        bool nodeHasMesh,
        ref Matrix4x4 local)
    {
        if (raw.Type == LightSourceType.Point)
        {
            if (!ModelLightConversion.TryPoint(raw.Diffuse, ReadRange(node), out var color, out var intensity, out var range))
            {
                Logger.Debug("Skipped point light with no brightness name={Name}", node->MName.AsString);
                return null;
            }

            if (!nodeHasMesh)
                local = Matrix4x4.CreateTranslation(raw.Position) * local;
            else if (raw.Position.LengthSquared() > 1e-8f)
                Logger.Debug("Ignoring light position offset on a mesh node name={Name}", node->MName.AsString);

            return new ImportedPointLight(color, intensity, range);
        }

        var direction = Vector3.TransformNormal(raw.Direction, world);
        if (!ModelLightConversion.TryDirectional(raw.Diffuse, direction, out var sunColor, out var baked))
        {
            Logger.Debug("Skipped directional light with no brightness name={Name}", node->MName.AsString);
            return null;
        }

        return new ImportedDirectionalLight(sunColor, baked);
    }

    private static unsafe float? ReadRange(AssimpNode* node)
    {
        var meta = node->MMetaData;
        if (meta == null)
            return null;

        for (uint i = 0; i < meta->MNumProperties; i++)
        {
            if (meta->MKeys[i].AsString != "PBR_LightRange")
                continue;

            var entry = meta->MValues[i];
            if (entry.MData == null)
                return null;

            if (entry.MType == MetadataType.Float)
                return *(float*)entry.MData;
            if (entry.MType == MetadataType.Double)
                return (float)*(double*)entry.MData;
        }

        return null;
    }

    private static ModelSceneNode WithUnmatchedLights(ModelSceneNode root, PendingLights pending)
    {
        var left = pending.TakeRemaining();
        if (left.Count == 0)
            return root;

        Logger.Debug("Lights without a matching node count={Count}", left.Count);
        var children = new List<ModelSceneNode>(root.Children.Count + left.Count);
        children.AddRange(root.Children);
        foreach (var raw in left)
        {
            var local = Matrix4x4.CreateTranslation(raw.Position);
            ImportedLight? imported;
            if (raw.Type == LightSourceType.Point)
            {
                if (!ModelLightConversion.TryPoint(raw.Diffuse, null, out var color, out var intensity, out var range))
                {
                    Logger.Debug("Skipped point light with no brightness name={Name}", raw.Name);
                    continue;
                }

                imported = new ImportedPointLight(color, intensity, range);
            }
            else if (!ModelLightConversion.TryDirectional(raw.Diffuse, raw.Direction, out var sunColor, out var baked))
            {
                Logger.Debug("Skipped directional light with no brightness name={Name}", raw.Name);
                continue;
            }
            else
            {
                imported = new ImportedDirectionalLight(sunColor, baked);
            }

            var name = string.IsNullOrWhiteSpace(raw.Name) ? "Light" : raw.Name;
            children.Add(new ModelSceneNode(name, [], [], local, imported));
        }

        return new ModelSceneNode(root.Name, root.MeshIndices, children, root.LocalTransform, root.Light);
    }

    private readonly record struct RawLight(
        string Name,
        LightSourceType Type,
        Vector3 Diffuse,
        Vector3 Position,
        Vector3 Direction);

    private sealed class PendingLights
    {
        private readonly Dictionary<string, Queue<RawLight>> _byName = new(StringComparer.Ordinal);
        public int Skipped { get; private set; }

        public void Add(string name, RawLight light)
        {
            if (!_byName.TryGetValue(name, out var queue))
            {
                queue = new Queue<RawLight>();
                _byName[name] = queue;
            }

            queue.Enqueue(light);
        }

        public void Skip() => Skipped++;

        public bool TryTake(string name, out RawLight light)
        {
            light = default;
            if (!_byName.TryGetValue(name, out var queue) || queue.Count == 0)
                return false;

            light = queue.Dequeue();
            return true;
        }

        public List<RawLight> TakeRemaining() =>
            _byName.Values.SelectMany(queue => queue).ToList();
    }

    internal static Matrix4x4 ToEngineMatrix(Matrix4x4 assimpMatrix)
    {
        var rowTranslation = new Vector3(assimpMatrix.M41, assimpMatrix.M42, assimpMatrix.M43);
        var colTranslation = new Vector3(assimpMatrix.M14, assimpMatrix.M24, assimpMatrix.M34);

        // Silk.NET already matches System.Numerics (translation in row 4). Transposing broke GLB imports.
        if (rowTranslation.LengthSquared() > 1e-6f || colTranslation.LengthSquared() <= 1e-6f)
            return assimpMatrix;

        // Raw Assimp aiMatrix4x4 layout: translation in column 4.
        return assimpMatrix with
        {
            M14 = 0f,
            M24 = 0f,
            M34 = 0f,
            M41 = colTranslation.X,
            M42 = colTranslation.Y,
            M43 = colTranslation.Z
        };
    }

    private static bool IsGltf(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".glb", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase);
    }

    private static void FlipTexCoordV(SourceSubmesh submesh)
    {
        var verts = submesh.Vertices;
        for (var i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            v.TexCoord = v.TexCoord with { Y = 1f - v.TexCoord.Y };
            verts[i] = v;
        }
    }

    private static unsafe SourceSubmesh? ExtractSourceSubmesh(AssimpMesh* aiMesh)
    {
        var submesh = new SourceSubmesh { Name = aiMesh->MName.AsString };
        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex { Position = aiMesh->MVertices[i] };

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

            submesh.Vertices.Add(vertex);
        }

        submesh.Indices.Capacity = (int)aiMesh->MNumFaces * 3;
        for (uint i = 0; i < aiMesh->MNumFaces; i++)
        {
            var face = aiMesh->MFaces[i];
            AddTriangleFace(submesh.Indices, new ReadOnlySpan<uint>(face.MIndices, (int)face.MNumIndices));
        }

        if (submesh.Indices.Count == 0)
        {
            Logger.Debug("Skipping mesh with no triangles name={Name}", submesh.Name);
            return null;
        }

        if (!RuntimeMeshVertexLayout.TryComputeBounds(submesh.Vertices, out var boundsMin, out var boundsMax))
        {
            Logger.Debug("Skipping mesh with invalid positions name={Name}", submesh.Name);
            return null;
        }

        submesh.BoundsMin = boundsMin;
        submesh.BoundsMax = boundsMax;
        return submesh;
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

    internal static string? ChooseMetallicRoughnessPath(string? packed, string? metalness, string? roughness)
    {
        if (!string.IsNullOrEmpty(packed))
            return packed;
        if (!string.IsNullOrEmpty(metalness))
            return metalness;
        if (!string.IsNullOrEmpty(roughness))
            return roughness;
        return null;
    }

    internal static float ImportedFactor(bool found, float value, float missing)
    {
        if (!found || !float.IsFinite(value))
            return missing;
        return System.Math.Clamp(value, 0f, 1f);
    }

    private readonly record struct MaterialInfo(
        string? DiffusePath,
        string? NormalPath,
        string? MetallicRoughnessPath,
        string? OcclusionPath,
        float MetallicFactor,
        float RoughnessFactor,
        Vector3 BaseColorFactor);

    private unsafe MaterialInfo ExtractMaterialInfo(
        Silk.NET.Assimp.Scene* scene,
        uint materialIndex,
        string directory,
        ModelSourceTextureSidecar textureSidecar)
    {
        if (materialIndex >= scene->MNumMaterials)
        {
            Logger.Warning(
                "Material index {MaterialIndex} out of range (MNumMaterials={MaterialCount})",
                materialIndex, scene->MNumMaterials);
            return new MaterialInfo(null, null, null, null, 0f, 0.5f, Vector3.One);
        }

        var aiMaterial = scene->MMaterials[materialIndex];

        // glTF/GLB puts albedo on BASE_COLOR. Assimp often also stuffs the first
        // image (a normal map, if that node is first) into DIFFUSE — using that as
        // color looks like random mosaic UVs.
        var diffuseTexturePath =
            ResolveTexturePath(scene, aiMaterial, TextureType.BaseColor, directory, textureSidecar)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Diffuse, directory, textureSidecar);
        var normalTexturePath = ResolveTexturePath(scene, aiMaterial, TextureType.Normals, directory, textureSidecar)
                                ?? ResolveTexturePath(scene, aiMaterial, TextureType.Height, directory, textureSidecar);

        if (diffuseTexturePath == null && normalTexturePath != null)
        {
            var inferred = AssimpTexturePath.InferAlbedoFromNormal(normalTexturePath, directory);
            if (inferred != null)
            {
                Logger.Debug(
                    "Texture type=Diffuse inferred from normal {Normal} → {Path}",
                    normalTexturePath, inferred);
                diffuseTexturePath = inferred;
            }
        }

        var packed = ResolveTexturePath(scene, aiMaterial, TextureType.GltfMetallicRoughness, directory, textureSidecar);
        var metalness = ResolveTexturePath(scene, aiMaterial, TextureType.Metalness, directory, textureSidecar);
        var roughness = ResolveTexturePath(scene, aiMaterial, TextureType.DiffuseRoughness, directory, textureSidecar);
        if (string.IsNullOrEmpty(packed)
            && !string.IsNullOrEmpty(metalness)
            && !string.IsNullOrEmpty(roughness)
            && !string.Equals(metalness, roughness, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warning(
                "Metallic and roughness textures differ; using metallic {Path}",
                metalness);
        }

        var metallicRoughnessPath = ChooseMetallicRoughnessPath(packed, metalness, roughness);
        var occlusionPath = ResolveTexturePath(scene, aiMaterial, TextureType.AmbientOcclusion, directory, textureSidecar);
        var metallicFactor = ReadFactor(aiMaterial, Assimp.MatkeyMetallicFactor, missing: 0f);
        var roughnessFactor = ReadFactor(aiMaterial, Assimp.MatkeyRoughnessFactor, missing: 0.5f);
        var baseColor = ReadBaseColor(aiMaterial);

        return new MaterialInfo(
            diffuseTexturePath,
            normalTexturePath,
            metallicRoughnessPath,
            occlusionPath,
            metallicFactor,
            roughnessFactor,
            baseColor);
    }

    private unsafe float ReadFactor(Material* material, string key, float missing)
    {
        var value = 0f;
        var found = _assimp.GetMaterialFloatArray(material, key, 0, 0, ref value, (uint*)null) == Return.Success;
        return ImportedFactor(found, value, missing);
    }

    private unsafe Vector3 ReadBaseColor(Material* material)
    {
        var color = new Vector4(1f, 1f, 1f, 1f);
        if (_assimp.GetMaterialColor(material, Assimp.MatkeyBaseColor, 0, 0, ref color) != Return.Success)
            return Vector3.One;

        return new Vector3(
            System.Math.Clamp(color.X, 0f, 1f),
            System.Math.Clamp(color.Y, 0f, 1f),
            System.Math.Clamp(color.Z, 0f, 1f));
    }

    private unsafe string? ResolveTexturePath(
        Silk.NET.Assimp.Scene* scene,
        Material* aiMaterial,
        TextureType textureType,
        string directory,
        ModelSourceTextureSidecar textureSidecar)
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
            var relative = textureSidecar.PersistEmbeddedTexture(scene, texturePath);
            if (string.IsNullOrEmpty(relative))
            {
                Logger.Warning("Failed to extract embedded texture type={Type} ref={Ref}", textureType, texturePath);
                return null;
            }

            Logger.Debug("Texture type={Type} extracted embedded {Ref} → {Path}", textureType, texturePath, relative);
            return relative;
        }

        var resolved = AssimpTexturePath.Resolve(texturePath, directory);
        if (resolved == null)
        {
            Logger.Warning(
                "Texture type={Type} path missing on disk: {Path}",
                textureType, texturePath);
            return null;
        }

        var cooked = textureSidecar.ToRelativeTexturePath(resolved);
        if (string.IsNullOrEmpty(cooked))
            return null;

        Logger.Debug("Texture type={Type} resolved to file {Path}", textureType, cooked);
        return cooked;
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
