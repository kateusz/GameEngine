using System.Numerics;
using Engine.Math;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Legacy orthographic camera implementation. This class is part of the deprecated non-ECS camera system.
/// </summary>
/// <remarks>
/// <para><b>DEPRECATED:</b> This class is part of the legacy camera system and will be removed in a future version.</para>
/// <para><b>Migration Path:</b> Use <see cref="Engine.Scene.SceneCamera"/> with <see cref="Engine.Scene.Components.CameraComponent"/> instead.</para>
/// <para>
/// Key differences when migrating:
/// <list type="bullet">
/// <item><description>SceneCamera requires external transform matrix (from TransformComponent)</description></item>
/// <item><description>Use CameraComponent.Camera property to access the SceneCamera instance</description></item>
/// <item><description>SceneCamera supports both Orthographic and Perspective projections</description></item>
/// <item><description>Use Graphics2D.BeginScene(Camera, Matrix4x4) overload instead of Graphics2D.BeginScene(OrthographicCamera)</description></item>
/// </list>
/// </para>
/// <para><b>Current Usage:</b> Currently used in editor mode (Scene.OnUpdateEditor). This will be migrated in Phase 3.</para>
/// </remarks>
[Obsolete("Use SceneCamera with CameraComponent for ECS-based camera system. This legacy camera system will be removed in a future version.")]
public class OrthographicCamera
{
    public OrthographicCamera(float left, float right, float bottom, float top)
    {
        Position = Vector3.Zero;
        Rotation = 0.0f;
        Scale = Vector3.One;

        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewMatrix = Matrix4x4.Identity;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    public Matrix4x4 ProjectionMatrix { get; private set; }
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ViewProjectionMatrix { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public Vector3 Scale { get; private set; }

    public void SetPosition(Vector3 position)
    {
        Position = position;
        RecalculateViewMatrix();
    }

    public void SetRotation(float rotation)
    {
        Rotation = rotation;
        RecalculateViewMatrix();
    }

    public void SetScale(Vector3 scale)
    {
        Scale = scale;
        RecalculateViewMatrix();
    }

    public void SetProjection(float left, float right, float bottom, float top)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    private void RecalculateViewMatrix()
    {
        var position = Matrix4x4.CreateTranslation(Position.X, Position.Y, 0);
        var rotation = Matrix4x4.CreateRotationZ(MathHelpers.DegreesToRadians(Rotation));
        var scale = Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z);

        var transform = Matrix4x4.Identity;
        transform *= position;
        transform *= rotation;

        Matrix4x4.Invert(transform, out var result);
        ViewMatrix = result;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }
}