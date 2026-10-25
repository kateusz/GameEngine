using Math;

namespace IslandDash;

public static class GameMath
{
    /// <summary>Frame-rate independent exponential smoothing factor for a given sharpness (1/s).</summary>
    public static float DampFactor(float sharpness, float dt) => 1f - MathF.Exp(-sharpness * dt);

    public static float Damp(float current, float target, float sharpness, float dt) =>
        current + (target - current) * DampFactor(sharpness, dt);

    public static Vector3 Damp(Vector3 current, Vector3 target, float sharpness, float dt) =>
        Vector3.Lerp(current, target, DampFactor(sharpness, dt));

    public static float DampAngle(float current, float target, float sharpness, float dt) =>
        current + DeltaAngle(current, target) * DampFactor(sharpness, dt);

    /// <summary>Shortest signed difference target - current, in (-PI, PI].</summary>
    public static float DeltaAngle(float current, float target)
    {
        var delta = (target - current) % MathF.Tau;
        if (delta > MathF.PI) delta -= MathF.Tau;
        if (delta <= -MathF.PI) delta += MathF.Tau;
        return delta;
    }

    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
    {
        var diff = target - current;
        var length = diff.Length();
        return length <= maxDelta || length < 1e-6f ? target : current + diff / length * maxDelta;
    }

    /// <summary>Yaw (radians around +Y) that turns the +Z model forward axis toward <paramref name="direction"/>.</summary>
    public static float YawFromDirection(Vector3 direction) => MathF.Atan2(direction.X, direction.Z);

    public static Quaternion YawRotation(float yaw) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);

    /// <summary>
    /// Converts a rotation to the Euler angles <see cref="SceneComponents.TransformComponent.Rotation"/> expects
    /// (inverse of <see cref="MathHelpers.QuaternionFromEuler"/>: X applied first, then Y, then Z).
    /// </summary>
    public static Vector3 ToEngineEuler(Quaternion q)
    {
        q = Quaternion.Normalize(q);
        var x = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
        var y = MathF.Asin(System.Math.Clamp(2f * (q.W * q.Y - q.Z * q.X), -1f, 1f));
        var z = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
        return new Vector3(x, y, z);
    }

    public static float Get(Vector3 v, int axis) => axis switch { 0 => v.X, 1 => v.Y, _ => v.Z };

    public static Vector3 With(Vector3 v, int axis, float value) => axis switch
    {
        0 => v with { X = value },
        1 => v with { Y = value },
        _ => v with { Z = value }
    };

    public static float Hash01(int seed)
    {
        unchecked
        {
            var h = (uint)seed * 2654435761u;
            h ^= h >> 15;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
