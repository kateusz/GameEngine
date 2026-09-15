using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using SceneComponents;
using SceneComponents.Physics;

namespace Engine.Scene.Systems;

/// <summary>
/// Fixed-timestep 2D physics simulation via <see cref="IPhysicsWorld2D"/>.
/// Per-scene system — each scene owns its own physics world instance.
/// </summary>
internal sealed class PhysicsSimulationSystem(
    IPhysicsWorld2D physicsWorld,
    IContext context,
    PhysicsRuntimeBodyStore bodyStore) : ISystem, IDisposable
{
    private const float Timestep = 1f / 60f;
    private const int MaxPhysicsStepsPerFrame = 5;

    private float _physicsAccumulator;
    private bool _disposed;
    private readonly Dictionary<int, PhysicsBodyIdentity> _identities = [];
    private readonly HashSet<int> _activeBodyIds = [];

    public int Priority => 100;

    public void OnInit()
    {
        _physicsAccumulator = 0f;
        SyncBodies();
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        _physicsAccumulator += (float)deltaTime.TotalSeconds;

        SyncBodies();

        var stepCount = 0;
        while (_physicsAccumulator >= Timestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            SyncKinematicTransformsToBodies();
            SyncVelocitiesToBodies();
            physicsWorld.Step(Timestep);
            _physicsAccumulator -= Timestep;
            stepCount++;
        }

        if (_physicsAccumulator >= Timestep)
            _physicsAccumulator = Timestep * 0.5f;

        foreach (var (entity, component, transform) in
                 context.View<RigidBody2DComponent, TransformComponent>())
        {
            var collider = ReadCollider(entity);
            if (collider.Kind == ColliderKind.None)
                continue;
            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.UpdateFixtureMaterial(collider.Density, collider.Friction, collider.Restitution);

            var position = body.Position;
            transform.Translation = new Vector3(position.X, position.Y, 0);
            transform.Rotation = transform.Rotation with { Z = body.Angle };

            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
                component.Velocity = body.LinearVelocity;
        }
    }

    public void OnShutdown()
    {
        foreach (var id in bodyStore.Snapshot().Keys.ToList())
            DropBody(id);
    }

    private void SyncBodies()
    {
        _activeBodyIds.Clear();
        foreach (var (entity, component, transform) in context.View<RigidBody2DComponent, TransformComponent>())
        {
            _activeBodyIds.Add(entity.Id);
            var collider = ReadCollider(entity);
            var identity = CaptureIdentity(component, transform, collider);
            if (bodyStore.TryGet(entity.Id, out _)
                && _identities.TryGetValue(entity.Id, out var baked)
                && baked == identity)
                continue;

            DropBody(entity.Id);

            var body = physicsWorld.CreateBody(new PhysicsBodyDef(
                new Vector2(transform.Translation.X, transform.Translation.Y),
                transform.Rotation.Z,
                component.BodyType,
                component.FixedRotation,
                component.GravityScale,
                component.IsBullet));

            body.Entity = entity;
            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
                body.LinearVelocity = component.Velocity;
            bodyStore.Set(entity.Id, body);
            _identities[entity.Id] = identity;
            AttachFixture(body, transform, collider);
        }

        foreach (var id in bodyStore.Snapshot().Keys.ToList())
        {
            if (!_activeBodyIds.Contains(id))
                DropBody(id);
        }
    }

    private static void AttachFixture(IPhysicsBody2D body, TransformComponent transform, ColliderInfo collider)
    {
        var scale = transform.Scale;
        switch (collider.Kind)
        {
            case ColliderKind.Box:
                body.CreateBoxFixture(new PhysicsBoxFixtureDef(
                    collider.Size.X * scale.X,
                    collider.Size.Y * scale.Y,
                    new Vector2(collider.Offset.X * scale.X, collider.Offset.Y * scale.Y),
                    collider.Density,
                    collider.Friction,
                    collider.Restitution,
                    collider.IsTrigger));
                break;
            case ColliderKind.Circle:
                var radiusScale = (MathF.Abs(scale.X) + MathF.Abs(scale.Y)) * 0.5f;
                body.CreateCircleFixture(new PhysicsCircleFixtureDef(
                    collider.Size.X * radiusScale,
                    new Vector2(collider.Offset.X * scale.X, collider.Offset.Y * scale.Y),
                    collider.Density,
                    collider.Friction,
                    collider.Restitution,
                    collider.IsTrigger));
                break;
            case ColliderKind.Edge:
                body.CreateEdgeFixture(new PhysicsEdgeFixtureDef(
                    ScalePoints(collider.Points!, scale),
                    collider.Density,
                    collider.Friction,
                    collider.Restitution,
                    collider.IsTrigger));
                break;
        }
    }

    private static Vector2[] ScalePoints(List<Vector2> points, Vector3 scale)
    {
        var scaled = new Vector2[points.Count];
        for (var i = 0; i < points.Count; i++)
            scaled[i] = new Vector2(points[i].X * scale.X, points[i].Y * scale.Y);
        return scaled;
    }

    private void SyncVelocitiesToBodies()
    {
        foreach (var (entity, component) in context.View<RigidBody2DComponent>())
        {
            if (component.BodyType is not (RigidBodyType.Dynamic or RigidBodyType.Kinematic))
                continue;

            if (bodyStore.TryGet(entity.Id, out var body))
                body.LinearVelocity = component.Velocity;
        }
    }

    private void SyncKinematicTransformsToBodies()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody2DComponent, TransformComponent>())
        {
            if (component.BodyType != RigidBodyType.Kinematic)
                continue;

            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.Position = new Vector2(transform.Translation.X, transform.Translation.Y);
            body.Angle = transform.Rotation.Z;
        }
    }

    private void DropBody(int entityId)
    {
        if (!bodyStore.TryGet(entityId, out var body))
            return;

        // ponytail: recreate drops native angular velocity; linear lives on the component
        physicsWorld.DestroyBody(body);
        bodyStore.Remove(entityId);
        _identities.Remove(entityId);
    }

    private static PhysicsBodyIdentity CaptureIdentity(
        RigidBody2DComponent component, TransformComponent transform, ColliderInfo collider) =>
        new(component.BodyType, component.FixedRotation, component.GravityScale, component.IsBullet,
            collider.Kind, collider.Size, collider.Offset, transform.Scale, collider.Density, collider.IsTrigger,
            collider.PointsHash);

    private static ColliderInfo ReadCollider(Entity entity)
    {
        if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
            return new(ColliderKind.Box, box.Size, box.Offset, box.Density, box.Friction, box.Restitution,
                box.IsTrigger, 0, null);
        if (entity.TryGetComponent<CircleCollider2DComponent>(out var circle))
            return new(ColliderKind.Circle, new Vector2(circle.Radius, 0f), circle.Offset, circle.Density,
                circle.Friction, circle.Restitution, circle.IsTrigger, 0, null);
        if (entity.TryGetComponent<EdgeCollider2DComponent>(out var edge))
            return new(ColliderKind.Edge, default, default, edge.Density, edge.Friction, edge.Restitution,
                edge.IsTrigger, HashPoints(edge.Points), edge.Points);
        return new(ColliderKind.None, default, default, 0f, 0f, 0f, false, 0, null);
    }

    private static int HashPoints(List<Vector2> points)
    {
        var hash = new HashCode();
        foreach (var point in points)
        {
            hash.Add(point.X);
            hash.Add(point.Y);
        }
        return hash.ToHashCode();
    }

    private enum ColliderKind { None, Box, Circle, Edge }

    private readonly record struct ColliderInfo(
        ColliderKind Kind,
        Vector2 Size,
        Vector2 Offset,
        float Density,
        float Friction,
        float Restitution,
        bool IsTrigger,
        int PointsHash,
        List<Vector2>? Points);

    private readonly record struct PhysicsBodyIdentity(
        RigidBodyType BodyType,
        bool FixedRotation,
        float GravityScale,
        bool IsBullet,
        ColliderKind Kind,
        Vector2 Size,
        Vector2 Offset,
        Vector3 Scale,
        float Density,
        bool IsTrigger,
        int PointsHash);

    public void Dispose()
    {
        if (_disposed)
            return;

        physicsWorld.Dispose();
        _disposed = true;
    }
}
