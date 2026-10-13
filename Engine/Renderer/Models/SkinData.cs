using System.Numerics;

namespace Engine.Renderer.Models;

public sealed class SkinData
{
    public required JointData[] Joints { get; init; }
    public required ClipData[] Clips { get; init; }
}

public readonly struct JointData
{
    public required int Parent { get; init; }
    public required Matrix4x4 InverseBind { get; init; }
    public required Vector3 RestTranslation { get; init; }
    public required Quaternion RestRotation { get; init; }
    public required Vector3 RestScale { get; init; }
}

public sealed class ClipData
{
    public required string Name { get; init; }
    public required float Duration { get; init; }
    public required ClipChannel[] Channels { get; init; }
}

public readonly struct ClipChannel
{
    public required int Joint { get; init; }
    public required ChannelProperty Property { get; init; }
    public required ChannelInterpolation Interpolation { get; init; }
    public required float[] Times { get; init; }
    public required float[] Values { get; init; }
}

public enum ChannelProperty
{
    Translation = 1,
    Rotation = 2,
    Scale = 3
}

public enum ChannelInterpolation
{
    Step = 1,
    Linear = 2
}
