using System.Numerics;
using Engine.Renderer.Animation;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using EngineSkeleton = Engine.Renderer.Animation.Skeleton;
using EngineVectorKey = Engine.Renderer.Animation.VectorKey;
using EngineQuatKey = Engine.Renderer.Animation.QuatKey;

namespace Engine.Renderer;

internal sealed class ModelImportResult
{
    public List<ModelSubmesh> Submeshes { get; init; } = [];
    public EngineSkeleton? Skeleton { get; init; }
    public IReadOnlyList<AnimationClip> Clips { get; init; } = Array.Empty<AnimationClip>();
}

internal sealed class AssimpModelImporter(Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();

    private const uint StaticFlags = (uint)(PostProcessSteps.Triangulate |
                                            PostProcessSteps.GenerateNormals |
                                            PostProcessSteps.CalculateTangentSpace |
                                            PostProcessSteps.FlipUVs |
                                            PostProcessSteps.PreTransformVertices);

    private const uint SkinnedFlags = (uint)(PostProcessSteps.Triangulate |
                                             PostProcessSteps.GenerateNormals |
                                             PostProcessSteps.CalculateTangentSpace |
                                             PostProcessSteps.FlipUVs |
                                             PostProcessSteps.LimitBoneWeights);

    public ModelImportResult Import(string path)
    {
        unsafe
        {
            var probe = assimp.ImportFile(path, (uint)PostProcessSteps.Triangulate);
            if (probe == null || (probe->MFlags & (uint)SceneFlags.Incomplete) != 0 || probe->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                if (probe != null)
                    assimp.ReleaseImport(probe);
                return new ModelImportResult();
            }

            var skinned = SceneHasBones(probe);
            assimp.ReleaseImport(probe);

            var flags = skinned ? SkinnedFlags : StaticFlags;
            var scene = assimp.ImportFile(path, flags);
            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return new ModelImportResult();
            }

            try
            {
                var directory = Path.GetDirectoryName(path) ?? string.Empty;
                EngineSkeleton? skeleton = null;
                Dictionary<string, int>? boneIndexByName = null;

                if (skinned)
                {
                    (skeleton, boneIndexByName) = BuildSkeleton(scene);
                    if (skeleton != null && skeleton.BoneCount > EngineSkeleton.MaxBones)
                    {
                        Logger.Warning(
                            "Model {Path} has {BoneCount} bones; truncating to {MaxBones}",
                            path, skeleton.BoneCount, EngineSkeleton.MaxBones);
                        (skeleton, boneIndexByName) = TruncateSkeleton(skeleton, boneIndexByName!, EngineSkeleton.MaxBones);
                    }
                }

                var submeshes = new List<ModelSubmesh>();
                for (uint i = 0; i < scene->MNumMeshes; i++)
                {
                    var aiMesh = scene->MMeshes[i];
                    var mesh = ExtractMesh(aiMesh, boneIndexByName);
                    var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                    submeshes.Add(new ModelSubmesh(mesh, material));
                }

                var clips = skinned && boneIndexByName != null
                    ? ExtractClips(path, scene, boneIndexByName)
                    : Array.Empty<AnimationClip>();

                return new ModelImportResult
                {
                    Submeshes = submeshes,
                    Skeleton = skeleton,
                    Clips = clips
                };
            }
            finally
            {
                assimp.ReleaseImport(scene);
            }
        }
    }

    private static unsafe bool SceneHasBones(AssimpScene* scene)
    {
        for (uint i = 0; i < scene->MNumMeshes; i++)
        {
            if (scene->MMeshes[i]->MNumBones > 0)
                return true;
        }

        return false;
    }

    private static unsafe (EngineSkeleton? Skeleton, Dictionary<string, int> BoneIndexByName) BuildSkeleton(AssimpScene* scene)
    {
        var boneIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var bones = new List<BoneData>();

        for (uint mi = 0; mi < scene->MNumMeshes; mi++)
        {
            var aiMesh = scene->MMeshes[mi];
            for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
            {
                var bone = aiMesh->MBones[bi];
                var name = bone->MName.AsString;
                if (string.IsNullOrEmpty(name) || boneIndexByName.ContainsKey(name))
                    continue;

                boneIndexByName[name] = bones.Count;
                bones.Add(new BoneData(name, -1, bone->MOffsetMatrix));
            }
        }

        if (bones.Count == 0)
            return (null, boneIndexByName);

        AssignBoneParents(scene->MRootNode, -1, bones, boneIndexByName);
        return (new EngineSkeleton(bones), boneIndexByName);
    }

    private static unsafe void AssignBoneParents(
        Node* node,
        int parentBoneIndex,
        List<BoneData> bones,
        Dictionary<string, int> boneIndexByName)
    {
        var name = node->MName.AsString;
        var currentParent = parentBoneIndex;
        if (!string.IsNullOrEmpty(name) && boneIndexByName.TryGetValue(name, out var boneIndex))
        {
            bones[boneIndex] = bones[boneIndex] with { ParentIndex = parentBoneIndex };
            currentParent = boneIndex;
        }

        for (uint i = 0; i < node->MNumChildren; i++)
            AssignBoneParents(node->MChildren[i], currentParent, bones, boneIndexByName);
    }

    private static (EngineSkeleton Skeleton, Dictionary<string, int> BoneIndexByName) TruncateSkeleton(
        EngineSkeleton skeleton,
        Dictionary<string, int> boneIndexByName,
        int maxBones)
    {
        var truncated = skeleton.Bones.Take(maxBones).ToList();
        for (var i = 0; i < truncated.Count; i++)
        {
            var b = truncated[i];
            if (b.ParentIndex >= maxBones)
                truncated[i] = b with { ParentIndex = -1 };
        }

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < truncated.Count; i++)
            map[truncated[i].Name] = i;

        return (new EngineSkeleton(truncated), map);
    }

    private unsafe Mesh ExtractMesh(AssimpMesh* aiMesh, Dictionary<string, int>? boneIndexByName)
    {
        var mesh = new Mesh(aiMesh->MName.AsString);
        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;

        var weightAcc = boneIndexByName != null
            ? new List<(int Bone, float Weight)>[aiMesh->MNumVertices]
            : null;
        if (weightAcc != null)
        {
            for (var i = 0; i < weightAcc.Length; i++)
                weightAcc[i] = [];
        }

        if (weightAcc != null)
        {
            for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
            {
                var bone = aiMesh->MBones[bi];
                var name = bone->MName.AsString;
                if (!boneIndexByName!.TryGetValue(name, out var boneIndex))
                    continue;

                for (uint wi = 0; wi < bone->MNumWeights; wi++)
                {
                    var w = bone->MWeights[wi];
                    if (w.MVertexId >= aiMesh->MNumVertices)
                        continue;
                    weightAcc[w.MVertexId].Add((boneIndex, w.MWeight));
                }
            }

            mesh.HasSkinning = aiMesh->MNumBones > 0;
        }

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

            if (weightAcc != null)
                AssignTopWeights(ref vertex, weightAcc[i]);

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

    private static void AssignTopWeights(ref Mesh.Vertex vertex, List<(int Bone, float Weight)> influences)
    {
        if (influences.Count == 0)
            return;

        influences.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        var count = System.Math.Min(4, influences.Count);
        var sum = 0f;
        for (var i = 0; i < count; i++)
            sum += influences[i].Weight;

        if (sum < 1e-8f)
            return;

        var inv = 1f / sum;
        var ids = new int[4];
        var weights = new float[4];
        for (var i = 0; i < count; i++)
        {
            ids[i] = influences[i].Bone;
            weights[i] = influences[i].Weight * inv;
        }

        vertex.BoneId0 = System.Math.Clamp(ids[0], 0, Engine.Renderer.Animation.Skeleton.MaxBones - 1);
        vertex.BoneId1 = System.Math.Clamp(ids[1], 0, Engine.Renderer.Animation.Skeleton.MaxBones - 1);
        vertex.BoneId2 = System.Math.Clamp(ids[2], 0, Engine.Renderer.Animation.Skeleton.MaxBones - 1);
        vertex.BoneId3 = System.Math.Clamp(ids[3], 0, Engine.Renderer.Animation.Skeleton.MaxBones - 1);
        vertex.BoneWeights = new Vector4(weights[0], weights[1], weights[2], weights[3]);
    }

    private unsafe IReadOnlyList<AnimationClip> ExtractClips(
        string path,
        AssimpScene* scene,
        Dictionary<string, int> boneIndexByName)
    {
        if (scene->MNumAnimations == 0)
        {
            Logger.Information("Model {Path}: no animations found", path);
            return Array.Empty<AnimationClip>();
        }

        var clips = new List<AnimationClip>((int)scene->MNumAnimations);
        for (uint ai = 0; ai < scene->MNumAnimations; ai++)
        {
            var anim = scene->MAnimations[ai];
            var ticksPerSecond = anim->MTicksPerSecond > 1e-8 ? anim->MTicksPerSecond : 25.0;
            var durationSeconds = (float)(anim->MDuration / ticksPerSecond);
            var name = anim->MName.AsString;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Animation_{ai}";

            var tracks = new List<BoneTrack>();
            for (uint ci = 0; ci < anim->MNumChannels; ci++)
            {
                var channel = anim->MChannels[ci];
                var nodeName = channel->MNodeName.AsString;
                if (!boneIndexByName.TryGetValue(nodeName, out var boneIndex))
                    continue;

                tracks.Add(new BoneTrack
                {
                    BoneIndex = boneIndex,
                    Positions = ReadPositions(channel, ticksPerSecond),
                    Rotations = ReadRotations(channel, ticksPerSecond),
                    Scales = ReadScales(channel, ticksPerSecond)
                });
            }

            clips.Add(new AnimationClip
            {
                Name = name,
                DurationSeconds = durationSeconds,
                Tracks = tracks
            });

            Logger.Information(
                "Model {Path}: found animation clip={ClipName} duration={Duration:F3}s channels={AssimpChannels} boneTracks={BoneTracks}",
                path, name, durationSeconds, anim->MNumChannels, tracks.Count);
        }

        Logger.Information("Model {Path}: {ClipCount} animation clip(s) loaded", path, clips.Count);
        return clips;
    }

    private static unsafe EngineVectorKey[] ReadPositions(NodeAnim* channel, double ticksPerSecond)
    {
        var keys = new EngineVectorKey[channel->MNumPositionKeys];
        for (uint i = 0; i < channel->MNumPositionKeys; i++)
        {
            var key = channel->MPositionKeys[i];
            keys[i] = new EngineVectorKey((float)(key.MTime / ticksPerSecond), key.MValue);
        }

        return keys;
    }

    private static unsafe EngineQuatKey[] ReadRotations(NodeAnim* channel, double ticksPerSecond)
    {
        var keys = new EngineQuatKey[channel->MNumRotationKeys];
        for (uint i = 0; i < channel->MNumRotationKeys; i++)
        {
            var key = channel->MRotationKeys[i];
            var q = key.MValue;
            keys[i] = new EngineQuatKey((float)(key.MTime / ticksPerSecond), new Quaternion(q.X, q.Y, q.Z, q.W));
        }

        return keys;
    }

    private static unsafe EngineVectorKey[] ReadScales(NodeAnim* channel, double ticksPerSecond)
    {
        var keys = new EngineVectorKey[channel->MNumScalingKeys];
        for (uint i = 0; i < channel->MNumScalingKeys; i++)
        {
            var key = channel->MScalingKeys[i];
            keys[i] = new EngineVectorKey((float)(key.MTime / ticksPerSecond), key.MValue);
        }

        return keys;
    }

    private unsafe MeshMaterial ExtractMaterial(
        AssimpScene* scene,
        uint materialIndex,
        string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];

        var albedoPath = ResolveTexturePath(aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Diffuse, directory);

        var mrPath = ResolveTexturePath(aiMaterial, TextureType.GltfMetallicRoughness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Metalness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.DiffuseRoughness, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Specular, directory)
            ?? ResolvePackedMrSibling(albedoPath);

        var normalPath = ResolveTexturePath(aiMaterial, TextureType.Normals, directory)
            ?? ResolveTexturePath(aiMaterial, TextureType.Height, directory);

        var material = new MeshMaterial
        {
            AlbedoTexturePath = albedoPath,
            MetallicRoughnessTexturePath = mrPath,
            NormalTexturePath = normalPath
        };

        var hasMetallic = TryGetFloat(aiMaterial, Assimp.MatkeyMetallicFactor, out var metallic);
        var hasRoughness = TryGetFloat(aiMaterial, Assimp.MatkeyRoughnessFactor, out var roughness);
        var metallicScalar = hasMetallic ? System.Math.Clamp(metallic, 0f, 1f) : 0f;
        var roughnessScalar = hasRoughness ? System.Math.Clamp(roughness, 0f, 1f) : 0.5f;

        if (mrPath != null || hasMetallic || hasRoughness)
        {
            material.Metallic = metallicScalar;
            material.Roughness = hasRoughness ? roughnessScalar : 0.5f;
        }
        else
        {
            material.Metallic = 0f;
            if (TryGetFloat(aiMaterial, Assimp.MaterialShininess, out var shininess) && shininess > 0f)
                material.Roughness = System.Math.Clamp(1f - shininess / 256f, 0.04f, 1f);
            else
                material.Roughness = 0.5f;
        }

        return material;
    }

    private static string? ResolvePackedMrSibling(string? albedoPath)
    {
        if (string.IsNullOrEmpty(albedoPath))
            return null;

        var specularPath = albedoPath.Replace("_BaseColor", "_Specular", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(specularPath, albedoPath, StringComparison.OrdinalIgnoreCase))
            return null;

        return System.IO.File.Exists(specularPath) ? specularPath : null;
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
