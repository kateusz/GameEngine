using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using SceneComponents;
using SceneComponents.Physics;
using Serilog;

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
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    private float _physicsAccumulator;
    private bool _disposed;
    private readonly Dictionary<int, PhysicsBodyIdentity> _identities = [];

    private const int MaxPhysicsStepsPerFrame = 5;

    public int Priority => SystemPriorities.PhysicsSimulationSystem;

    public void OnInit()
    {
        _physicsAccumulator = 0f;
        EnsureBodiesCreated();
        Logger.Debug("PhysicsSimulationSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        const int velocityIterations = 6;
        const int positionIterations = 2;
        var deltaSeconds = (float)deltaTime.TotalSeconds;

        _physicsAccumulator += deltaSeconds;

        EnsureBodiesCreated();
        CleanupOrphanedBodies();

        var stepCount = 0;
        while (_physicsAccumulator >= PhysicsConstants.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            SyncKinematicTransformsToBodies();
            SyncVelocitiesToBodies();
            physicsWorld.Step(PhysicsConstants.PhysicsTimestep, velocityIterations, positionIterations);
            _physicsAccumulator -= PhysicsConstants.PhysicsTimestep;
            stepCount++;
        }

        if (_physicsAccumulator >= PhysicsConstants.PhysicsTimestep)
            _physicsAccumulator = PhysicsConstants.PhysicsTimestep * 0.5f;

        foreach (var (entity, component, transform) in
                 context.View<RigidBody2DComponent, TransformComponent>())
        {
            if (GetColliderMaterial(entity) is not { } material)
                continue;
            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.UpdateFixtureMaterial(material.Density, material.Friction, material.Restitution);

            var position = body.Position;
            transform.Translation = new Vector3(position.X, position.Y, 0);
            transform.Rotation = transform.Rotation with { Z = body.Angle };

            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
                component.Velocity = body.LinearVelocity;
        }
    }

    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem shutting down - cleaning up physics bodies");

        foreach (var id in bodyStore.Snapshot().Keys.ToList())
            DropBody(id);

        Logger.Debug("PhysicsSimulationSystem shut down - all physics bodies destroyed");
    }

    private void EnsureBodiesCreated()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody2DComponent, TransformComponent>())
        {
            var identity = CaptureIdentity(entity, component, transform);
            if (bodyStore.TryGet(entity.Id, out _))
            {
                if (_identities.TryGetValue(entity.Id, out var baked) && baked == identity)
                    continue;
                DropBody(entity.Id);
            }

            var body = physicsWorld.CreateBody(new PhysicsBodyDef(
                new Vector2(transform.Translation.X, transform.Translation.Y),
                transform.Rotation.Z,
                ToMotionType(component.BodyType),
                component.FixedRotation,
                component.GravityScale));

            body.Entity = entity;
            bodyStore.Set(entity.Id, body);
            _identities[entity.Id] = identity;

            AttachFixture(entity, body, transform);
        }
    }

    // One fixture per body: Box > Circle > Edge.
    private static void AttachFixture(Entity entity, IPhysicsBody2D body, TransformComponent transform)
    {
        var scale = transform.Scale;

        if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
        {
            body.CreateBoxFixture(new PhysicsBoxFixtureDef(
                box.Size.X * scale.X,
                box.Size.Y * scale.Y,
                new Vector2(box.Offset.X * scale.X, box.Offset.Y * scale.Y),
                box.Density,
                box.Friction,
                box.Restitution,
                box.IsTrigger));
            return;
        }

        if (entity.TryGetComponent<CircleCollider2DComponent>(out var circle))
        {
            var radiusScale = (MathF.Abs(scale.X) + MathF.Abs(scale.Y)) * 0.5f;
            body.CreateCircleFixture(new PhysicsCircleFixtureDef(
                circle.Radius * radiusScale,
                new Vector2(circle.Offset.X * scale.X, circle.Offset.Y * scale.Y),
                circle.Density,
                circle.Friction,
                circle.Restitution,
                circle.IsTrigger));
            return;
        }

        if (entity.TryGetComponent<EdgeCollider2DComponent>(out var edge))
        {
            body.CreateEdgeFixture(new PhysicsEdgeFixtureDef(
                ScalePoints(edge.Points, scale),
                edge.Density,
                edge.Friction,
                edge.Restitution,
                edge.IsTrigger));
        }
    }

    private static Vector2[] ScalePoints(List<Vector2> points, Vector3 scale)
    {
        var scaled = new Vector2[points.Count];
        for (var i = 0; i < points.Count; i++)
            scaled[i] = new Vector2(points[i].X * scale.X, points[i].Y * scale.Y);
        return scaled;
    }

    // Same priority as AttachFixture: Box > Circle > Edge.
    private static (float Density, float Friction, float Restitution)? GetColliderMaterial(Entity entity)
    {
        if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
            return (box.Density, box.Friction, box.Restitution);
        if (entity.TryGetComponent<CircleCollider2DComponent>(out var circle))
            return (circle.Density, circle.Friction, circle.Restitution);
        if (entity.TryGetComponent<EdgeCollider2DComponent>(out var edge))
            return (edge.Density, edge.Friction, edge.Restitution);
        return null;
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

    private void CleanupOrphanedBodies()
    {
        var activeEntityIds = context.View<RigidBody2DComponent>().Select(v => v.Entity.Id).ToHashSet();
        var staleEntityIds = bodyStore.Snapshot().Keys.Where(id => !activeEntityIds.Contains(id)).ToList();
        foreach (var staleEntityId in staleEntityIds)
            DropBody(staleEntityId);
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
        Entity entity, RigidBody2DComponent component, TransformComponent transform)
    {
        if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
            return new(component.BodyType, component.FixedRotation, component.GravityScale,
                ColliderKind.Box, box.Size, box.Offset, transform.Scale, box.Density, box.IsTrigger, 0);
        if (entity.TryGetComponent<CircleCollider2DComponent>(out var circle))
            return new(component.BodyType, component.FixedRotation, component.GravityScale,
                ColliderKind.Circle, new Vector2(circle.Radius, 0f), circle.Offset, transform.Scale,
                circle.Density, circle.IsTrigger, 0);
        if (entity.TryGetComponent<EdgeCollider2DComponent>(out var edge))
            return new(component.BodyType, component.FixedRotation, component.GravityScale,
                ColliderKind.Edge, default, default, transform.Scale, edge.Density, edge.IsTrigger,
                HashPoints(edge.Points));
        return new(component.BodyType, component.FixedRotation, component.GravityScale,
            ColliderKind.None, default, default, transform.Scale, 0f, false, 0);
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

    private readonly record struct PhysicsBodyIdentity(
        RigidBodyType BodyType,
        bool FixedRotation,
        float GravityScale,
        ColliderKind Kind,
        Vector2 Size,
        Vector2 Offset,
        Vector3 Scale,
        float Density,
        bool IsTrigger,
        int PointsHash);

    private static PhysicsBodyMotionType ToMotionType(RigidBodyType bodyType) =>
        bodyType switch
        {
            RigidBodyType.Static => PhysicsBodyMotionType.Static,
            RigidBodyType.Dynamic => PhysicsBodyMotionType.Dynamic,
            RigidBodyType.Kinematic => PhysicsBodyMotionType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(bodyType), bodyType, null)
        };

    public void Dispose()
    {
        if (_disposed)
            return;

        physicsWorld.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("PhysicsSimulationSystem disposed");
    }
}
