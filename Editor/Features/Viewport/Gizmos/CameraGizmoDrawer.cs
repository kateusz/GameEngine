using System.Numerics;
using ECS;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Camera;
using Serilog;

namespace Editor.Features.Viewport.Gizmos;

/// <summary>
/// Edit-mode camera icons. Same overlay slot as collider debug.
/// </summary>
public sealed class CameraGizmoDrawer(ITextureFactory textureFactory)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(CameraGizmoDrawer));

    private Texture2D? _icon;
    private bool _iconLoadFailed;

    public void Draw(
        IContext context,
        IGraphics2D graphics2D,
        EditorCamera editorCamera)
    {
        EnsureIcon();
        if (_icon == null)
            return;

        var editorPos = editorCamera.GetPosition();
        var right = editorCamera.GetRightDirection();
        var up = editorCamera.GetUpDirection();
        var face = -editorCamera.GetForwardDirection();

        foreach (var (entity, _) in context.View<CameraComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var billboard = BillboardGizmoHelper.BuildBillboard(
                transform.GetWorldTransform().Translation, editorPos, right, up, face);

            graphics2D.DrawQuad(billboard, _icon, BillboardGizmoHelper.TextureCoords,
                tilingFactor: 1.0f, tintColor: Vector4.One, entity.Id);
        }
    }

    private void EnsureIcon()
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
}
