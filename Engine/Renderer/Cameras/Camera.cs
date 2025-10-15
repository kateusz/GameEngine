using System.Numerics;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Legacy base camera class. This class is part of the deprecated non-ECS camera system.
/// </summary>
/// <remarks>
/// <para><b>DEPRECATED:</b> This class is part of the legacy camera system and will be removed in a future version.</para>
/// <para><b>Migration Path:</b> Use <see cref="Engine.Scene.SceneCamera"/> with <see cref="Engine.Scene.Components.CameraComponent"/> instead.</para>
/// <para>
/// The legacy camera system (Camera, OrthographicCamera, OrthographicCameraController) is being replaced by the ECS-based system.
/// For editor code, consider migrating to use SceneCamera with editor-specific flags in future releases.
/// </para>
/// </remarks>
[Obsolete("Use SceneCamera with CameraComponent for ECS-based camera system. This legacy camera system will be removed in a future version.")]
public class Camera(Matrix4x4 projection)
{
    public Matrix4x4 Projection { get; protected set; } = projection;
}