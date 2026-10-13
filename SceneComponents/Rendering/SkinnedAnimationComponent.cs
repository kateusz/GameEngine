using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public sealed class SkinnedAnimationComponent : IComponent
{
    public const int MaxJoints = 128;

    private string _clipName = "";

    public string ClipName
    {
        get => _clipName;
        set
        {
            value ??= "";
            if (_clipName == value)
                return;
            _clipName = value;
            Time = 0f;
            LastTime = 0f;
            PoseReady = false;
            if (KeyCursors.Length > 0)
                Array.Clear(KeyCursors);
        }
    }

    public bool Loop { get; set; } = true;

    [JsonIgnore] public float Time { get; set; }
    [JsonIgnore] public float LastTime { get; set; }
    [JsonIgnore] public bool PoseReady { get; set; }
    [JsonIgnore] public Matrix4x4[] Pose { get; private set; } = [];
    [JsonIgnore] public Matrix4x4[] Locals { get; private set; } = [];
    [JsonIgnore] public int[] KeyCursors { get; private set; } = [];

    public void EnsureCapacity(int jointCount)
    {
        if (jointCount == Pose.Length)
            return;
        Pose = new Matrix4x4[jointCount];
        Locals = new Matrix4x4[jointCount];
        KeyCursors = new int[jointCount * 3];
        PoseReady = false;
    }

    public IComponent Clone() => new SkinnedAnimationComponent
    {
        _clipName = _clipName,
        Loop = Loop
    };
}
