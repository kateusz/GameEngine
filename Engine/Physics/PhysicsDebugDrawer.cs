using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Physics;

namespace Engine.Physics;

internal static class PhysicsDebugDrawer
{
    private const int CircleSegmentCount = 32;

    public static void Draw(
        IContext context,
        IGraphics2D graphics2D,
        PhysicsRuntimeBodyStore bodyStore,
        in SceneRenderPipeline.CameraBinding camera,
        bool useTransformFallbackWhenNoBody)
    {
        if (!camera.IsValid)
            return;

        SceneRenderPipeline.Begin2DScene(graphics2D, camera);

        foreach (var (entity, boxCollider) in context.View<BoxCollider2DComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out var body))
                DrawBox(graphics2D, entity, boxCollider, body.Position, body.Angle, GetRuntimeBodyDebugColor(body));
            else if (useTransformFallbackWhenNoBody)
            {
                var transform = entity.GetComponent<TransformComponent>();
                DrawBox(
                    graphics2D,
                    entity,
                    boxCollider,
                    new Vector2(transform.Translation.X, transform.Translation.Y),
                    transform.Rotation.Z,
                    GetEditorColliderColor(entity));
            }
        }

        foreach (var (entity, circleCollider) in context.View<CircleCollider2DComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out var body))
                DrawCircle(graphics2D, entity, circleCollider, body.Position, body.Angle, GetRuntimeBodyDebugColor(body));
            else if (useTransformFallbackWhenNoBody)
            {
                var transform = entity.GetComponent<TransformComponent>();
                DrawCircle(
                    graphics2D,
                    entity,
                    circleCollider,
                    new Vector2(transform.Translation.X, transform.Translation.Y),
                    transform.Rotation.Z,
                    GetEditorColliderColor(entity));
            }
        }

        foreach (var (entity, edgeCollider) in context.View<EdgeCollider2DComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out var body))
                DrawPolyline(graphics2D, entity, edgeCollider.Points, body.Position, body.Angle, GetRuntimeBodyDebugColor(body));
            else if (useTransformFallbackWhenNoBody)
            {
                var transform = entity.GetComponent<TransformComponent>();
                DrawPolyline(
                    graphics2D,
                    entity,
                    edgeCollider.Points,
                    new Vector2(transform.Translation.X, transform.Translation.Y),
                    transform.Rotation.Z,
                    GetEditorColliderColor(entity));
            }
        }

        graphics2D.EndScene();
    }

    private static void DrawBox(
        IGraphics2D graphics2D,
        Entity entity,
        BoxCollider2DComponent boxCollider,
        Vector2 origin,
        float angle,
        Vector4 color)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var size = new Vector2(
            boxCollider.Size.X * 2.0f * transform.Scale.X,
            boxCollider.Size.Y * 2.0f * transform.Scale.Y);
        var worldPos = WorldPositionWithOffset(origin, angle, boxCollider.Offset, transform.Scale);
        var trs = Matrix4x4.CreateScale(size.X, size.Y, 1.0f)
                  * Matrix4x4.CreateRotationZ(angle)
                  * Matrix4x4.CreateTranslation(worldPos);
        graphics2D.DrawRect(trs, color, entity.Id);
    }

    private static void DrawCircle(
        IGraphics2D graphics2D,
        Entity entity,
        CircleCollider2DComponent circleCollider,
        Vector2 origin,
        float angle,
        Vector4 color)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var radiusScale = (MathF.Abs(transform.Scale.X) + MathF.Abs(transform.Scale.Y)) * 0.5f;
        var radius = circleCollider.Radius * radiusScale;
        if (radius <= 0f)
            return;

        var center = WorldPositionWithOffset(origin, angle, circleCollider.Offset, transform.Scale);
        Vector3 previous = default;
        for (var i = 0; i <= CircleSegmentCount; i++)
        {
            var t = i * (MathF.PI * 2f / CircleSegmentCount);
            var point = new Vector3(
                center.X + MathF.Cos(t) * radius,
                center.Y + MathF.Sin(t) * radius,
                0f);
            if (i > 0)
                graphics2D.DrawLine(previous, point, color, entity.Id);
            previous = point;
        }
    }

    private static void DrawPolyline(
        IGraphics2D graphics2D,
        Entity entity,
        List<Vector2> localPoints,
        Vector2 origin,
        float angle,
        Vector4 color)
    {
        if (localPoints.Count < 2)
            return;

        var transform = entity.GetComponent<TransformComponent>();
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var world = new Vector3[localPoints.Count];
        for (var i = 0; i < localPoints.Count; i++)
        {
            var scaled = new Vector2(localPoints[i].X * transform.Scale.X, localPoints[i].Y * transform.Scale.Y);
            world[i] = new Vector3(
                origin.X + scaled.X * cos - scaled.Y * sin,
                origin.Y + scaled.X * sin + scaled.Y * cos,
                0f);
        }

        for (var i = 0; i < world.Length - 1; i++)
            graphics2D.DrawLine(world[i], world[i + 1], color, entity.Id);
    }

    private static Vector3 WorldPositionWithOffset(
        Vector2 origin,
        float angle,
        Vector2 offset,
        Vector3 scale)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var scaledOffset = new Vector2(offset.X * scale.X, offset.Y * scale.Y);
        var rotatedOffset = new Vector2(
            scaledOffset.X * cos - scaledOffset.Y * sin,
            scaledOffset.X * sin + scaledOffset.Y * cos);
        return new Vector3(origin.X + rotatedOffset.X, origin.Y + rotatedOffset.Y, 0f);
    }

    private static Vector4 GetEditorColliderColor(Entity entity)
    {
        if (!entity.TryGetComponent<RigidBody2DComponent>(out var rb))
            return new Vector4(0.0f, 1.0f, 1.0f, 1.0f);

        return rb.BodyType switch
        {
            RigidBodyType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            RigidBodyType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
            _ => new Vector4(1.0f, 0.0f, 0.3f, 1.0f)
        };
    }

    private static Vector4 GetRuntimeBodyDebugColor(IPhysicsBody2D body)
    {
        if (!body.IsEnabled())
            return new Vector4(0.5f, 0.5f, 0.0f, 1.0f);

        return body.MotionType switch
        {
            PhysicsBodyMotionType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            PhysicsBodyMotionType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
            _ => body.IsAwake()
                ? new Vector4(1.0f, 0.0f, 0.3f, 1.0f)
                : new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
        };
    }
}
