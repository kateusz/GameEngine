using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.Features.Viewport;

internal static class VisibilityZoneDebugDrawer
{
    private static readonly Vector4 WireColor = new(0.2f, 0.85f, 0.95f, 1f);

    public static void Draw(Context context, IGraphics2D graphics2D)
    {
        foreach (var (_, zone, transform) in context.View<VisibilityZoneComponent, TransformComponent>())
        {
            var world = transform.GetWorldTransform();
            var local = new Aabb(zone.Min, zone.Max);
            Span<Vector3> corners = stackalloc Vector3[8];
            Aabb.TransformCorners(world, local, corners);
            DrawBoxEdges(graphics2D, corners);
        }
    }

    private static void DrawBoxEdges(IGraphics2D graphics2D, ReadOnlySpan<Vector3> corners)
    {
        // corner order matches Aabb.TransformCorners (x,y,z loops)
        ReadOnlySpan<int> edges =
        [
            0, 1, 1, 3, 3, 2, 2, 0,
            4, 5, 5, 7, 7, 6, 6, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        ];

        for (var i = 0; i < edges.Length; i += 2)
            graphics2D.DrawLine(corners[edges[i]], corners[edges[i + 1]], WireColor, -1);
    }
}
