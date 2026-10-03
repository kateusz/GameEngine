using System.Numerics;
using Engine.Core.Window;
using Engine.Renderer.Pipeline;
using Engine.Scene.Cameras;

namespace Benchmark;

public sealed class BenchmarkCamera
{
    const int Left = 0;
    const int Right = 1;
    const int Middle = 2;

    Vector2 _last;
    bool _hasLast;
    bool _left;
    bool _right;
    bool _middle;

    public Vector3 Focal { get; private set; }
    public float Distance { get; private set; } = MathF.Sqrt(12f * 12f + 18f * 18f);
    public float Yaw { get; private set; }
    public float Pitch { get; private set; } = MathF.Asin(12f / MathF.Sqrt(12f * 12f + 18f * 18f));
    public bool Dragging => _left || _right || _middle;

    public Vector3 Eye => Focal + Offset() * Distance;

    public void Button(int button, bool down)
    {
        if (button == Left) _left = down;
        else if (button == Right) _right = down;
        else if (button == Middle) _middle = down;
    }

    public void Move(Vector2 position)
    {
        if (!_hasLast)
        {
            _last = position;
            _hasLast = true;
            return;
        }

        var delta = (position - _last) * CameraConfig.EditorMouseSensitivity;
        _last = position;
        if (_left)
            Orbit(delta);
        else if (_right)
            Look(delta);
        else if (_middle)
            Pan(delta);
    }

    public void LookFrom(Vector3 eye, Vector3 focal)
    {
        var offset = eye - focal;
        var distance = offset.Length();
        if (distance < 0.05f)
            return;

        Distance = System.Math.Clamp(distance, 1f, 400f);
        var dir = offset / distance;
        Pitch = System.Math.Clamp(MathF.Asin(System.Math.Clamp(dir.Y, -1f, 1f)), -MathF.PI / 2f + 0.05f, MathF.PI / 2f - 0.05f);
        Yaw = MathF.Atan2(dir.X, dir.Z);
        Focal = focal;
    }

    public void Zoom(float scrollY)
    {
        var step = System.Math.Clamp(Distance * 0.12f, 0.5f, 30f);
        Distance = System.Math.Clamp(Distance - scrollY * step, 1f, 400f);
    }

    public SceneView CreateView(bool directionalShadows, bool pointShadows)
    {
        var eye = Eye;
        var view = Matrix4x4.CreateLookAt(eye, Focal, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 3f, DisplayConfig.DefaultAspectRatio, 0.1f, 500f);
        return new SceneView(view * projection, eye, pointShadows, DirectionalShadows: directionalShadows);
    }

    void Orbit(Vector2 delta)
    {
        Yaw -= delta.X * CameraConfig.EditorRotationSpeed;
        Pitch = System.Math.Clamp(
            Pitch - delta.Y * CameraConfig.EditorRotationSpeed,
            -MathF.PI / 2f + 0.05f,
            MathF.PI / 2f - 0.05f);
    }

    void Look(Vector2 delta)
    {
        var eye = Eye;
        Orbit(delta);
        Focal = eye - Offset() * Distance;
    }

    void Pan(Vector2 delta)
    {
        var forward = -Offset();
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var scale = Distance * 2f;
        Focal += -right * delta.X * scale + up * delta.Y * scale;
    }

    Vector3 Offset() => new(
        MathF.Cos(Pitch) * MathF.Sin(Yaw),
        MathF.Sin(Pitch),
        MathF.Cos(Pitch) * MathF.Cos(Yaw));
}
