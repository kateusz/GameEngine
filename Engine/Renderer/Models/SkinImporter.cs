using System.Numerics;
using Engine.Renderer.Models.RuntimeMesh;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace Engine.Renderer.Models;

internal static class SkinImporter
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SkinImporter));

    public static unsafe SkinData? TryRead(
        Silk.NET.Assimp.Scene* scene,
        List<SourceSubmesh> submeshes,
        IReadOnlyDictionary<uint, int> meshIndexMap,
        ModelSceneNode graph)
    {
        AssimpMesh* first = null;
        for (uint i = 0; i < scene->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[i];
            if (mesh != null && mesh->MNumBones > 0 && meshIndexMap.ContainsKey(i))
            {
                first = mesh;
                break;
            }
        }

        if (first == null)
            return null;

        if (first->MNumBones > RuntimeMeshFormat.MaxJoints)
        {
            Logger.Debug("Dropped skin because it has {Count} joints", first->MNumBones);
            return null;
        }

        var nodes = new Dictionary<string, (string? Parent, Matrix4x4 Local)>(StringComparer.Ordinal);
        IndexNodes(graph, null, nodes);

        var joints = new JointData[first->MNumBones];
        var nameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (uint boneIndex = 0; boneIndex < first->MNumBones; boneIndex++)
        {
            var bone = first->MBones[boneIndex];
            var name = bone->MName.AsString;
            if (string.IsNullOrEmpty(name) || !nameToIndex.TryAdd(name, (int)boneIndex))
            {
                Logger.Debug("Dropped skin because joint name {Name} is missing or repeated", name);
                return null;
            }

            if (!nodes.TryGetValue(name, out var node))
            {
                Logger.Debug("Dropped skin because joint {Name} has no node", name);
                return null;
            }

            if (!Matrix4x4.Decompose(node.Local, out var scale, out var rotation, out var translation))
            {
                Logger.Debug("Dropped skin because joint {Name} does not decompose", name);
                return null;
            }

            var inverseBind = AssimpModelImporter.ToEngineMatrix(bone->MOffsetMatrix);
            if (!IsFinite(inverseBind))
            {
                Logger.Debug("Dropped skin because joint {Name} has a non-finite inverse bind", name);
                return null;
            }

            joints[boneIndex] = new JointData
            {
                Parent = -1,
                InverseBind = inverseBind,
                RestTranslation = translation,
                RestRotation = rotation,
                RestScale = scale
            };
        }

        for (var i = 0; i < joints.Length; i++)
        {
            var name = NameOf(first, (uint)i);
            var parentName = nodes[name].Parent;
            while (parentName != null && !nameToIndex.ContainsKey(parentName))
            {
                if (!nodes.TryGetValue(parentName, out var parentNode))
                    break;
                parentName = parentNode.Parent;
            }

            joints[i] = joints[i] with
            {
                Parent = parentName != null && nameToIndex.TryGetValue(parentName, out var parent) ? parent : -1
            };
        }

        if (HasCycle(joints))
        {
            Logger.Debug("Dropped skin because the joint parents cycle");
            return null;
        }

        var weights = new Dictionary<int, (int[] Joints, float[] Weights)>();
        var ignoredExtraSkin = false;
        for (uint meshIndex = 0; meshIndex < scene->MNumMeshes; meshIndex++)
        {
            if (!meshIndexMap.TryGetValue(meshIndex, out var compact))
                continue;

            var mesh = scene->MMeshes[meshIndex];
            var count = submeshes[compact].Vertices.Count;
            var jointIds = new int[count * 4];
            var influenceWeights = new float[count * 4];
            var filled = new int[count];
            if (mesh->MNumBones > 0)
            {
                for (uint boneIndex = 0; boneIndex < mesh->MNumBones; boneIndex++)
                {
                    var bone = mesh->MBones[boneIndex];
                    if (!nameToIndex.TryGetValue(bone->MName.AsString, out var joint))
                    {
                        ignoredExtraSkin = true;
                        continue;
                    }

                    for (uint weightIndex = 0; weightIndex < bone->MNumWeights; weightIndex++)
                    {
                        var weight = bone->MWeights[weightIndex];
                        if (weight.MVertexId >= (uint)count)
                        {
                            Logger.Debug("Dropped skin because a weight points past the vertex list");
                            return null;
                        }

                        var slot = filled[weight.MVertexId];
                        if (slot >= 4)
                            continue;

                        var at = (int)weight.MVertexId * 4 + slot;
                        jointIds[at] = joint;
                        influenceWeights[at] = weight.MWeight;
                        filled[weight.MVertexId] = slot + 1;
                    }
                }
            }

            weights[compact] = (jointIds, influenceWeights);
        }

        if (ignoredExtraSkin)
            Logger.Debug("Ignored joints that are not on the first skin");

        for (var i = 0; i < submeshes.Count; i++)
        {
            if (!weights.TryGetValue(i, out var record))
            {
                var zeros = submeshes[i].Vertices.Count * 4;
                submeshes[i].Joints = new int[zeros];
                submeshes[i].Weights = new float[zeros];
                continue;
            }

            submeshes[i].Joints = record.Joints;
            submeshes[i].Weights = record.Weights;
        }

        return new SkinData
        {
            Joints = joints,
            Clips = ReadClips(scene, nameToIndex)
        };
    }

    private static unsafe ClipData[] ReadClips(Silk.NET.Assimp.Scene* scene, Dictionary<string, int> nameToIndex)
    {
        var clips = new List<ClipData>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (uint animationIndex = 0; animationIndex < scene->MNumAnimations; animationIndex++)
        {
            var animation = scene->MAnimations[animationIndex];
            var name = animation->MName.AsString;
            if (string.IsNullOrEmpty(name) || !names.Add(name))
                continue;

            var ticksPerSecond = animation->MTicksPerSecond > 0 ? animation->MTicksPerSecond : 25.0;
            var channels = new List<ClipChannel>();
            var duration = 0f;
            for (uint channelIndex = 0; channelIndex < animation->MNumChannels; channelIndex++)
            {
                var channel = animation->MChannels[channelIndex];
                if (!nameToIndex.TryGetValue(channel->MNodeName.AsString, out var joint))
                    continue;

                AddChannel(channels, joint, ChannelProperty.Translation, channel->MNumPositionKeys, channel->MPositionKeys, ticksPerSecond, ref duration);
                AddRotation(channels, joint, channel, ticksPerSecond, ref duration);
                AddChannel(channels, joint, ChannelProperty.Scale, channel->MNumScalingKeys, channel->MScalingKeys, ticksPerSecond, ref duration);
            }

            clips.Add(new ClipData
            {
                Name = name,
                Duration = duration,
                Channels = channels.ToArray()
            });
        }

        return clips.ToArray();
    }

    private static unsafe void AddChannel(
        List<ClipChannel> channels,
        int joint,
        ChannelProperty property,
        uint keyCount,
        Silk.NET.Assimp.VectorKey* keys,
        double ticksPerSecond,
        ref float duration)
    {
        var times = new List<float>();
        var values = new List<float>();
        for (uint i = 0; i < keyCount; i++)
        {
            var time = (float)(keys[i].MTime / ticksPerSecond);
            var value = keys[i].MValue;
            if (!float.IsFinite(time) || !float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
                continue;
            times.Add(time);
            values.Add(value.X);
            values.Add(value.Y);
            values.Add(value.Z);
            if (time > duration)
                duration = time;
        }

        if (times.Count == 0)
            return;

        channels.Add(new ClipChannel
        {
            Joint = joint,
            Property = property,
            Interpolation = ChannelInterpolation.Linear,
            Times = times.ToArray(),
            Values = values.ToArray()
        });
    }

    private static unsafe void AddRotation(
        List<ClipChannel> channels,
        int joint,
        Silk.NET.Assimp.NodeAnim* channel,
        double ticksPerSecond,
        ref float duration)
    {
        var times = new List<float>();
        var values = new List<float>();
        for (uint i = 0; i < channel->MNumRotationKeys; i++)
        {
            var key = channel->MRotationKeys[i];
            var time = (float)(key.MTime / ticksPerSecond);
            var value = key.MValue;
            if (!float.IsFinite(time) || !float.IsFinite(value.X) || !float.IsFinite(value.Y)
                || !float.IsFinite(value.Z) || !float.IsFinite(value.W))
                continue;
            times.Add(time);
            values.Add(value.X);
            values.Add(value.Y);
            values.Add(value.Z);
            values.Add(value.W);
            if (time > duration)
                duration = time;
        }

        if (times.Count == 0)
            return;

        channels.Add(new ClipChannel
        {
            Joint = joint,
            Property = ChannelProperty.Rotation,
            Interpolation = ChannelInterpolation.Linear,
            Times = times.ToArray(),
            Values = values.ToArray()
        });
    }

    private static void IndexNodes(
        ModelSceneNode node,
        string? parent,
        Dictionary<string, (string? Parent, Matrix4x4 Local)> nodes)
    {
        nodes.TryAdd(node.Name, (parent, node.LocalTransform));
        foreach (var child in node.Children)
            IndexNodes(child, node.Name, nodes);
    }

    private static unsafe string NameOf(AssimpMesh* mesh, uint boneIndex) =>
        mesh->MBones[boneIndex]->MName.AsString;

    private static bool HasCycle(JointData[] joints)
    {
        var state = new byte[joints.Length];
        for (var i = 0; i < joints.Length; i++)
        {
            var index = i;
            while (index >= 0)
            {
                if (index >= joints.Length || state[index] == 1)
                    return true;
                if (state[index] == 2)
                    break;
                state[index] = 1;
                index = joints[index].Parent;
            }

            index = i;
            while (index >= 0 && index < joints.Length && state[index] == 1)
            {
                state[index] = 2;
                index = joints[index].Parent;
            }
        }

        return false;
    }

    private static bool IsFinite(Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12) && float.IsFinite(matrix.M13) && float.IsFinite(matrix.M14)
        && float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22) && float.IsFinite(matrix.M23) && float.IsFinite(matrix.M24)
        && float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32) && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34)
        && float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42) && float.IsFinite(matrix.M43) && float.IsFinite(matrix.M44);
}
