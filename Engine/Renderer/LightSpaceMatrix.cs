using System.Numerics;

namespace Engine.Renderer;

internal static class LightSpaceMatrix
{
    private const float Near = 0.1f;
    private const float Far = 100f;

    public static Matrix4x4 Create(Vector3 direction, Vector3 origin, float orthoSize)
    {
        var dir = LightingMath.NormalizeDirection(direction);
        var up = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        var view = Matrix4x4.CreateLookAt(origin, origin + dir, up);
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            -orthoSize, orthoSize, -orthoSize, orthoSize, Near, Far);
        return view * projection;
    }

    public static Matrix4x4[] CreateCubemapFaces(Vector3 lightPos, float farPlane)
    {
        var near = MathF.Min(0.05f, farPlane * 0.5f);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, near, farPlane);
        return
        [
            Matrix4x4.CreateLookAt(lightPos, lightPos + Vector3.UnitX, -Vector3.UnitY) * projection,
            Matrix4x4.CreateLookAt(lightPos, lightPos - Vector3.UnitX, -Vector3.UnitY) * projection,
            Matrix4x4.CreateLookAt(lightPos, lightPos + Vector3.UnitY, Vector3.UnitZ) * projection,
            Matrix4x4.CreateLookAt(lightPos, lightPos - Vector3.UnitY, -Vector3.UnitZ) * projection,
            Matrix4x4.CreateLookAt(lightPos, lightPos + Vector3.UnitZ, -Vector3.UnitY) * projection,
            Matrix4x4.CreateLookAt(lightPos, lightPos - Vector3.UnitZ, -Vector3.UnitY) * projection
        ];
    }

    public static bool IsFinite(Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12) && float.IsFinite(matrix.M13) &&
        float.IsFinite(matrix.M14) && float.IsFinite(matrix.M22) && float.IsFinite(matrix.M23) &&
        float.IsFinite(matrix.M24) && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34) &&
        float.IsFinite(matrix.M44);

    public static Vector3 TransformPoint(Matrix4x4 matrix, Vector3 point)
    {
        var clip = Vector4.Transform(new Vector4(point, 1f), matrix);
        if (MathF.Abs(clip.W) < 1e-6f)
            return new Vector3(float.NaN, float.NaN, float.NaN);

        return new Vector3(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
    }
}
