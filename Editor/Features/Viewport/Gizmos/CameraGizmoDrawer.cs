using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Cameras;
using SceneComponents;
using SceneComponents.Camera;
using Serilog;

namespace Editor.Features.Viewport.Gizmos;

/// <summary>
/// Edit-mode camera icons. Same overlay slot as collider debug.
/// </summary>
internal static class CameraGizmoDrawer
{
    private static readonly ILogger Logger = Log.ForContext(typeof(CameraGizmoDrawer));
    private static readonly Vector2[] TextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    // ponytail: fixed screen-ish size via distance scale; upgrade to true pixel size if icons feel off
    private const float IconDistanceScale = 0.06f;
    private const float MinIconSize = 0.15f;

    private static Texture2D? _icon;
    private static bool _iconLoadFailed;

    public static void Draw(
        IContext context,
        IGraphics2D graphics2D,
        EditorCamera editorCamera,
        ITextureFactory textureFactory)
    {
        EnsureIcon(textureFactory);
        if (_icon == null)
            return;

        graphics2D.BeginScene(editorCamera);

        var editorPos = editorCamera.GetPosition();
        var right = editorCamera.GetRightDirection();
        var up = editorCamera.GetUpDirection();
        // Quad lies in XY; face the view so +Z points toward the editor camera.
        var face = -editorCamera.GetForwardDirection();

        foreach (var (entity, _) in context.View<CameraComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            DrawIcon(graphics2D, entity.Id, transform.Translation, editorPos, right, up, face);
        }

        graphics2D.EndScene();
    }

    private static void EnsureIcon(ITextureFactory textureFactory)
    {
        if (_icon != null || _iconLoadFailed)
            return;

        try
        {
            _icon = textureFactory.Create("Resources/Icons/camera.png");
        }
        catch (Exception ex)
        {
            _iconLoadFailed = true;
            Logger.Warning(ex, "Failed to load camera gizmo icon; icons skipped");
        }
    }

    private static void DrawIcon(
        IGraphics2D graphics2D,
        int entityId,
        Vector3 position,
        Vector3 editorPos,
        Vector3 right,
        Vector3 up,
        Vector3 face)
    {
        var distance = Vector3.Distance(editorPos, position);
        var size = MathF.Max(MinIconSize, distance * IconDistanceScale);

        var billboard = new Matrix4x4(
            right.X * size, right.Y * size, right.Z * size, 0.0f,
            up.X * size, up.Y * size, up.Z * size, 0.0f,
            face.X * size, face.Y * size, face.Z * size, 0.0f,
            position.X, position.Y, position.Z, 1.0f);

        graphics2D.DrawQuad(billboard, _icon, TextureCoords, tilingFactor: 1.0f, tintColor: Vector4.One, entityId);
    }
}
