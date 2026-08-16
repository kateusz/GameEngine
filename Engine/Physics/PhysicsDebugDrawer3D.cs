using System.Numerics;
using ECS;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Systems;
using Math;
using SceneComponents;
using SceneComponents.Physics;

namespace Engine.Physics;

internal static class PhysicsDebugDrawer3D
{
    public static void Draw(
        IContext context,
        IGraphics3D graphics3D,
        PhysicsRuntimeBodyStore3D? bodyStore,
        in SceneRenderPipeline.CameraBinding camera,
        bool useTransformFallbackWhenNoBody)
    {
        if (!camera.IsValid)
            return;

        SceneRenderPipeline.Begin3DScene(graphics3D, camera);
        graphics3D.SetWireframe(true);

        foreach (var (entity, box) in context.View<BoxCollider3DComponent>())
        {
            if (TryGetPose(entity, bodyStore, useTransformFallbackWhenNoBody, out var position, out var orientation, out var color))
                DrawBox(graphics3D, entity, box, position, orientation, color);
        }

        foreach (var (entity, sphere) in context.View<SphereCollider3DComponent>())
        {
            if (TryGetPose(entity, bodyStore, useTransformFallbackWhenNoBody, out var position, out var orientation, out var color))
                DrawSphereAabb(graphics3D, entity, sphere, position, orientation, color);
        }

        foreach (var (entity, capsule) in context.View<CapsuleCollider3DComponent>())
        {
            if (TryGetPose(entity, bodyStore, useTransformFallbackWhenNoBody, out var position, out var orientation, out var color))
                DrawCapsuleAabb(graphics3D, entity, capsule, position, orientation, color);
        }

        graphics3D.SetWireframe(false);
        graphics3D.EndScene();
    }

    private static bool TryGetPose(
        Entity entity,
        PhysicsRuntimeBodyStore3D? bodyStore,
        bool useTransformFallbackWhenNoBody,
        out Vector3 position,
        out Quaternion orientation,
        out Vector4 color)
    {
        if (bodyStore is not null && bodyStore.TryGet(entity.Id, out var body))
        {
            position = body.Position;
            orientation = body.Orientation;
            color = GetRuntimeBodyDebugColor(body);
            return true;
        }

        if (!useTransformFallbackWhenNoBody)
        {
            position = default;
            orientation = default;
            color = default;
            return false;
        }

        var transform = entity.GetComponent<TransformComponent>();
        position = transform.Translation;
        orientation = MathHelpers.QuaternionFromEuler(transform.Rotation);
        color = GetEditorColliderColor(entity);
        return true;
    }

    private static void DrawBox(
        IGraphics3D graphics3D,
        Entity entity,
        BoxCollider3DComponent box,
        Vector3 origin,
        Quaternion orientation,
        Vector4 color)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var size = new Vector3(
            box.Size.X * 2f * transform.Scale.X,
            box.Size.Y * 2f * transform.Scale.Y,
            box.Size.Z * 2f * transform.Scale.Z);
        DrawWireCube(graphics3D, origin, orientation, box.Offset, transform.Scale, size, color, entity.Id);
    }

    private static void DrawSphereAabb(
        IGraphics3D graphics3D,
        Entity entity,
        SphereCollider3DComponent sphere,
        Vector3 origin,
        Quaternion orientation,
        Vector4 color)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var radiusScale = (MathF.Abs(transform.Scale.X) + MathF.Abs(transform.Scale.Y) + MathF.Abs(transform.Scale.Z)) / 3f;
        var diameter = sphere.Radius * radiusScale * 2f;
        DrawWireCube(graphics3D, origin, orientation, sphere.Offset, transform.Scale, new Vector3(diameter), color, entity.Id);
    }

    private static void DrawCapsuleAabb(
        IGraphics3D graphics3D,
        Entity entity,
        CapsuleCollider3DComponent capsule,
        Vector3 origin,
        Quaternion orientation,
        Vector4 color)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var radiusScale = (MathF.Abs(transform.Scale.X) + MathF.Abs(transform.Scale.Z)) * 0.5f;
        var radius = capsule.Radius * radiusScale;
        var height = capsule.Length * MathF.Abs(transform.Scale.Y) + radius * 2f;
        DrawWireCube(
            graphics3D,
            origin,
            orientation,
            capsule.Offset,
            transform.Scale,
            new Vector3(radius * 2f, height, radius * 2f),
            color,
            entity.Id);
    }

    private static void DrawWireCube(
        IGraphics3D graphics3D,
        Vector3 origin,
        Quaternion orientation,
        Vector3 offset,
        Vector3 scale,
        Vector3 size,
        Vector4 color,
        int entityId)
    {
        var worldPos = origin + Vector3.Transform(offset * scale, orientation);
        var trs = Matrix4x4.CreateScale(size)
                  * MathHelpers.MatrixFromQuaternion(orientation)
                  * Matrix4x4.CreateTranslation(worldPos);
        graphics3D.DrawCube(trs, color, entityId);
    }

    private static Vector4 GetEditorColliderColor(Entity entity)
    {
        if (!entity.TryGetComponent<RigidBody3DComponent>(out var rb))
            return new Vector4(0.0f, 1.0f, 1.0f, 1.0f);

        return rb.BodyType switch
        {
            RigidBodyType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            RigidBodyType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
            _ => new Vector4(1.0f, 0.0f, 0.3f, 1.0f)
        };
    }

    private static Vector4 GetRuntimeBodyDebugColor(IPhysicsBody3D body)
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
