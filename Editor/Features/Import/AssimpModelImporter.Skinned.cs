using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer;
using Engine.Renderer.Models;
using Serilog;
using Silk.NET.Assimp;

// Silk.NET.Assimp declares `Mesh`/`SkeletonBone`/`VectorKey`; the engine reserves those names for
// the runtime format types — alias the engine types so unqualified names stay Engine.Renderer.
using Mesh = Engine.Renderer.Meshes.Mesh;
using SkeletonBone = Engine.Renderer.Models.SkeletonBone;
using VectorKey = Engine.Renderer.Models.VectorKey;

namespace Editor.Features.Import;

internal sealed record AssimpSkinnedImport(
    IReadOnlyList<AssimpModelPart> Parts,
    IReadOnlyList<SkeletonBone> Bones,
    IReadOnlyList<AnimationClip> Clips);

internal sealed partial class AssimpModelImporter
{
    public AssimpSkinnedImport? ImportSkinned(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.LimitBoneWeights |
                                  PostProcessSteps.JoinIdenticalVertices);

        unsafe
        {
            var scene = assimp.ImportFile(path, flags);
            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import skinned model path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return null;
            }

            try
            {
                var nodesByName = new Dictionary<string, nint>(StringComparer.Ordinal);
                CollectNodesByName(scene->MRootNode, nodesByName);
                if (!TryBuildSkeleton(scene, nodesByName, out var bones, out var error))
                {
                    if (error is not null)
                        throw new InvalidOperationException(error);
                    return null;
                }

                var indexByName = new Dictionary<string, int>(bones.Count, StringComparer.Ordinal);
                for (var i = 0; i < bones.Count; i++)
                    indexByName[bones[i].Name] = i;

                var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var parts = new List<AssimpModelPart>();
                WalkSkinnedNode(scene, scene->MRootNode, Matrix4x4.Identity, directory, nameCounts, indexByName, parts);
                if (parts.Count == 0)
                    return null;

                var clips = ExtractClips(scene, indexByName);
                return new AssimpSkinnedImport(parts, bones, clips);
            }
            finally
            {
                assimp.ReleaseImport(scene);
            }
        }
    }

    private static unsafe void CollectNodesByName(Node* node, Dictionary<string, nint> nodesByName)
    {
        var name = node->MName.AsString;
        if (!string.IsNullOrEmpty(name))
            nodesByName.TryAdd(name, (nint)node);

        for (uint i = 0; i < node->MNumChildren; i++)
        {
            var child = node->MChildren[i];
            if (child != null)
                CollectNodesByName(child, nodesByName);
        }
    }

    private static unsafe bool TryBuildSkeleton(
        Silk.NET.Assimp.Scene* scene,
        Dictionary<string, nint> nodesByName,
        out List<SkeletonBone> bones,
        out string? error)
    {
        bones = [];
        error = null;
        var boneNames = new HashSet<string>(StringComparer.Ordinal);
        var offsetByName = new Dictionary<string, Matrix4x4>(StringComparer.Ordinal);

        for (uint mi = 0; mi < scene->MNumMeshes; mi++)
        {
            var aiMesh = scene->MMeshes[mi];
            if (aiMesh->MNumBones == 0 || aiMesh->MBones == null)
                continue;
            for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
            {
                var bone = aiMesh->MBones[bi];
                var name = bone->MName.AsString;
                if (string.IsNullOrEmpty(name))
                    continue;
                boneNames.Add(name);
                offsetByName.TryAdd(name, Matrix4x4.Transpose(bone->MOffsetMatrix));
            }
        }

        if (boneNames.Count == 0)
            return false;

        for (uint ai = 0; ai < scene->MNumAnimations; ai++)
        {
            var anim = scene->MAnimations[ai];
            for (uint ci = 0; ci < anim->MNumChannels; ci++)
            {
                var nodeName = anim->MChannels[ci]->MNodeName.AsString;
                if (!string.IsNullOrEmpty(nodeName))
                    boneNames.Add(nodeName);
            }
        }

        if (boneNames.Count > SkeletalLimits.MaxBones)
        {
            error = $"Skeleton has {boneNames.Count} bones; maximum is {SkeletalLimits.MaxBones}.";
            return false;
        }

        var sorted = boneNames.ToList();
        sorted.Sort((a, b) =>
        {
            var cmp = NodeDepth(a, nodesByName).CompareTo(NodeDepth(b, nodesByName));
            return cmp != 0 ? cmp : string.CompareOrdinal(a, b);
        });

        var indexByName = new Dictionary<string, int>(sorted.Count, StringComparer.Ordinal);
        for (var i = 0; i < sorted.Count; i++)
            indexByName[sorted[i]] = i;

        bones = new List<SkeletonBone>(sorted.Count);
        foreach (var name in sorted)
        {
            var parentIndex = -1;
            nint nodePtr = 0;
            if (nodesByName.TryGetValue(name, out nodePtr))
            {
                var node = (Node*)nodePtr;
                for (var parent = node->MParent; parent != null; parent = parent->MParent)
                {
                    if (indexByName.TryGetValue(parent->MName.AsString, out var pi))
                    {
                        parentIndex = pi;
                        break;
                    }
                }
            }

            if (!offsetByName.TryGetValue(name, out var inverseBind) || inverseBind == Matrix4x4.Identity)
            {
                if (nodePtr != 0)
                {
                    var bindGlobal = BindGlobalRow((Node*)nodePtr);
                    if (!Matrix4x4.Invert(bindGlobal, out inverseBind))
                        inverseBind = Matrix4x4.Identity;
                }
                else
                    inverseBind = Matrix4x4.Identity;
            }

            bones.Add(new SkeletonBone(name, parentIndex, inverseBind));
        }

        return true;
    }

    private static int NodeDepth(string name, Dictionary<string, nint> nodesByName)
    {
        if (!nodesByName.TryGetValue(name, out var ptr))
            return int.MaxValue;
        unsafe
        {
            var depth = 0;
            for (var n = ((Node*)ptr)->MParent; n != null; n = n->MParent)
                depth++;
            return depth;
        }
    }

    private static unsafe Matrix4x4 BindGlobalRow(Node* node)
    {
        var chain = new List<nint>();
        for (var n = node; n != null; n = n->MParent)
            chain.Add((nint)n);

        var worldColumn = Matrix4x4.Identity;
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var n = (Node*)chain[i];
            worldColumn = Matrix4x4.Multiply(worldColumn, n->MTransformation);
        }

        return Matrix4x4.Transpose(worldColumn);
    }

    private unsafe void WalkSkinnedNode(
        Silk.NET.Assimp.Scene* scene,
        Node* node,
        Matrix4x4 parentWorld,
        string directory,
        Dictionary<string, int> nameCounts,
        Dictionary<string, int> boneIndexByName,
        List<AssimpModelPart> parts)
    {
        var world = Matrix4x4.Multiply(parentWorld, node->MTransformation);
        if (node->MNumMeshes > 0 && node->MMeshes != null)
        {
            var submeshes = new List<ModelSubmesh>((int)node->MNumMeshes);
            for (uint i = 0; i < node->MNumMeshes; i++)
            {
                var meshIndex = node->MMeshes[i];
                if (meshIndex >= scene->MNumMeshes)
                    continue;
                var aiMesh = scene->MMeshes[meshIndex];
                var mesh = ExtractSkinnedMesh(aiMesh, boneIndexByName);
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(new ModelSubmesh(mesh, material));
            }

            if (submeshes.Count > 0)
            {
                var rawName = node->MName.AsString;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = submeshes[0].Mesh.Name;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = "part";
                parts.Add(new AssimpModelPart(
                    AssimpPartNaming.UniqueSanitize(rawName, nameCounts),
                    Matrix4x4.Transpose(world),
                    submeshes));
            }
        }

        for (uint i = 0; i < node->MNumChildren; i++)
        {
            var child = node->MChildren[i];
            if (child != null)
                WalkSkinnedNode(scene, child, world, directory, nameCounts, boneIndexByName, parts);
        }
    }

    private static unsafe Mesh ExtractSkinnedMesh(
        Silk.NET.Assimp.Mesh* aiMesh,
        Dictionary<string, int> boneIndexByName)
    {
        var mesh = ExtractMesh(aiMesh);
        if (aiMesh->MNumBones == 0 || aiMesh->MBones == null)
            return mesh;

        var influences = new List<(int Bone, float Weight)>[mesh.Vertices.Count];
        for (var i = 0; i < influences.Length; i++)
            influences[i] = [];

        for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
        {
            var bone = aiMesh->MBones[bi];
            var name = bone->MName.AsString;
            if (!boneIndexByName.TryGetValue(name, out var boneIndex))
                continue;
            for (uint wi = 0; wi < bone->MNumWeights; wi++)
            {
                var weight = bone->MWeights[wi];
                var vertexId = (int)weight.MVertexId;
                if ((uint)vertexId >= (uint)mesh.Vertices.Count)
                    continue;
                influences[vertexId].Add((boneIndex, weight.MWeight));
            }
        }

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var list = influences[i];
            list.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            if (list.Count > 4)
                list.RemoveRange(4, list.Count - 4);
            var sum = 0f;
            foreach (var inf in list)
                sum += inf.Weight;
            if (sum > 1e-8f)
            {
                for (var k = 0; k < list.Count; k++)
                    list[k] = (list[k].Bone, list[k].Weight / sum);
            }

            var v = mesh.Vertices[i];
            v.BoneId0 = list.Count > 0 ? list[0].Bone : -1;
            v.BoneId1 = list.Count > 1 ? list[1].Bone : -1;
            v.BoneId2 = list.Count > 2 ? list[2].Bone : -1;
            v.BoneId3 = list.Count > 3 ? list[3].Bone : -1;
            v.Weights = new Vector4(
                list.Count > 0 ? list[0].Weight : 0f,
                list.Count > 1 ? list[1].Weight : 0f,
                list.Count > 2 ? list[2].Weight : 0f,
                list.Count > 3 ? list[3].Weight : 0f);
            mesh.Vertices[i] = v;
        }

        return mesh;
    }

    private static unsafe List<AnimationClip> ExtractClips(
        Silk.NET.Assimp.Scene* scene,
        Dictionary<string, int> boneIndexByName)
    {
        var clips = new List<AnimationClip>((int)scene->MNumAnimations);
        for (uint ai = 0; ai < scene->MNumAnimations; ai++)
        {
            var anim = scene->MAnimations[ai];
            var ticksPerSecond = anim->MTicksPerSecond > 0 ? anim->MTicksPerSecond : 25.0;
            var duration = (float)(anim->MDuration / ticksPerSecond);
            var name = anim->MName.AsString;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Clip{ai}";

            var channels = new List<BoneChannel>((int)anim->MNumChannels);
            for (uint ci = 0; ci < anim->MNumChannels; ci++)
            {
                var channel = anim->MChannels[ci];
                var nodeName = channel->MNodeName.AsString;
                if (!boneIndexByName.TryGetValue(nodeName, out var boneIndex))
                    continue;

                var positions = new List<VectorKey>((int)channel->MNumPositionKeys);
                for (uint k = 0; k < channel->MNumPositionKeys; k++)
                {
                    var key = channel->MPositionKeys[k];
                    positions.Add(new VectorKey((float)(key.MTime / ticksPerSecond), key.MValue));
                }

                var rotations = ReadAssimp5RotationKeys(channel, ticksPerSecond);

                var scales = new List<VectorKey>((int)channel->MNumScalingKeys);
                for (uint k = 0; k < channel->MNumScalingKeys; k++)
                {
                    var key = channel->MScalingKeys[k];
                    scales.Add(new VectorKey((float)(key.MTime / ticksPerSecond), key.MValue));
                }

                channels.Add(new BoneChannel(boneIndex, positions, rotations, scales));
            }

            clips.Add(new AnimationClip(name, duration, channels));
        }

        return clips;
    }

    // Silk.NET.Assimp 2.23 QuatKey is 32 bytes (assimp 6 + MInterpolation). The native
    // loaded on macOS is libassimp.5.dylib (5.4.1), where aiQuatKey is 24 bytes. Using
    // the managed indexer skips 8 bytes per key and corrupts every rotation after 0.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeAiQuatKey
    {
        public double Time;
        public float W;
        public float X;
        public float Y;
        public float Z;
    }

    private static unsafe List<RotationKey> ReadAssimp5RotationKeys(NodeAnim* channel, double ticksPerSecond)
    {
        var rotations = new List<RotationKey>((int)channel->MNumRotationKeys);
        var src = (byte*)channel->MRotationKeys;
        for (uint k = 0; k < channel->MNumRotationKeys; k++)
        {
            var key = (NativeAiQuatKey*)(src + k * 24);
            rotations.Add(new RotationKey(
                (float)(key->Time / ticksPerSecond),
                new Quaternion(key->X, key->Y, key->Z, key->W)));
        }

        return rotations;
    }
}
