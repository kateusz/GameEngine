using System.Numerics;
using ECS;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Lighting;

namespace Editor.Features.Viewport.Gizmos;

/// <summary>
/// Edit-mode point/spot light bubble icons at each light's world position.
/// </summary>
public sealed class LightGizmoDrawer(ITextureFactory textureFactory)
{
    private const int BubbleTextureSize = 64;
    private const float IconDistanceScale = 0.07f;

    private Texture2D? _bubbleTexture;

    public void Draw(IContext context, IGraphics2D graphics2D, EditorCamera editorCamera)
    {
        EnsureBubbleTexture();
        if (_bubbleTexture == null)
            return;

        var editorPos = editorCamera.GetPosition();
        var right = editorCamera.GetRightDirection();
        var up = editorCamera.GetUpDirection();
        var face = -editorCamera.GetForwardDirection();

        foreach (var (entity, plc) in context.View<PointLightComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            DrawBubble(graphics2D, entity.Id, transform.GetWorldTransform().Translation,
                new Vector3(plc.Color.X, plc.Color.Y, plc.Color.Z), editorPos, right, up, face);
        }

        foreach (var (entity, slc) in context.View<SpotLightComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            DrawBubble(graphics2D, entity.Id, transform.GetWorldTransform().Translation,
                new Vector3(slc.Color.X, slc.Color.Y, slc.Color.Z), editorPos, right, up, face);
        }
    }

    private void EnsureBubbleTexture()
    {
        if (_bubbleTexture != null)
            return;

        var pixels = new byte[BubbleTextureSize * BubbleTextureSize * 4];
        var center = (BubbleTextureSize - 1) * 0.5f;
        var radius = BubbleTextureSize * 0.5f - 1f;

        for (var y = 0; y < BubbleTextureSize; y++)
        {
            for (var x = 0; x < BubbleTextureSize; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var t = MathF.Sqrt(dx * dx + dy * dy) / radius;
                var alpha = MathF.Max(0f, 1f - t);
                alpha *= alpha;
                var glow = MathF.Max(0f, 1f - t * 0.85f);

                var i = (y * BubbleTextureSize + x) * 4;
                var value = (byte)(255 * glow);
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = (byte)(255 * alpha);
            }
        }

        _bubbleTexture = textureFactory.CreateFromRgba(pixels, BubbleTextureSize, BubbleTextureSize);
    }

    private void DrawBubble(
        IGraphics2D graphics2D,
        int entityId,
        Vector3 position,
        Vector3 lightColor,
        Vector3 editorPos,
        Vector3 right,
        Vector3 up,
        Vector3 face)
    {
        var billboard = BillboardGizmoHelper.BuildBillboard(
            position, editorPos, right, up, face, IconDistanceScale);

        var tint = new Vector4(
            MathF.Max(lightColor.X, 0.15f),
            MathF.Max(lightColor.Y, 0.15f),
            MathF.Max(lightColor.Z, 0.15f),
            1.0f);

        graphics2D.DrawQuad(billboard, _bubbleTexture, BillboardGizmoHelper.TextureCoords,
            tilingFactor: 1.0f, tintColor: tint, entityId);
    }
}
