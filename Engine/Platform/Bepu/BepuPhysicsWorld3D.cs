using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using ECS;
using Engine.Physics;
using Scripting;

namespace Engine.Platform.Bepu;

internal sealed class BepuPhysicsWorld3D : IPhysicsWorld3D
{
    private readonly BufferPool _bufferPool;
    private readonly Simulation _simulation;
    private readonly Dictionary<int, BepuPhysicsBody3D> _dynamics = [];
    private readonly Dictionary<int, BepuPhysicsBody3D> _statics = [];
    private readonly HashSet<(int, int)> _activePairs = [];
    private readonly HashSet<(int, int)> _previousPairs = [];
    private readonly HashSet<(int, int)> _triggerPairs = [];
    private readonly HashSet<(int, int)> _previousTriggerPairs = [];
    private IPhysicsContactListener3D? _contactListener3D;
    private bool _disposed;

    internal Simulation Simulation => _simulation;

    public BepuPhysicsWorld3D(Vector3 gravity)
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(this),
            new PoseIntegratorCallbacks(this, gravity),
            new SolveDescription(8, 1));
    }

    public void Step(float timeStep, int velocityIterations, int positionIterations)
    {
        ThrowIfDisposed();
        _activePairs.Clear();
        _triggerPairs.Clear();
        _simulation.Timestep(timeStep);
        FlushContacts();
    }

    public IPhysicsBody3D CreateBody(in PhysicsBodyDef3D def)
    {
        ThrowIfDisposed();
        return new BepuPhysicsBody3D(this, def);
    }

    public void DestroyBody(IPhysicsBody3D body)
    {
        ThrowIfDisposed();
        if (body is BepuPhysicsBody3D bepuBody)
            bepuBody.RemoveFromSimulation();
    }

    public void SetContactListener(IPhysicsContactListener3D? listener)
    {
        ThrowIfDisposed();
        _contactListener3D = listener;
    }

    public RaycastHit3D? Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false)
    {
        ThrowIfDisposed();
        if (maxDistance <= 0f
            || !float.IsFinite(maxDistance)
            || !IsFinite(origin)
            || !IsFinite(direction)
            || direction.LengthSquared() <= float.Epsilon)
            return null;

        var handler = new ClosestRayHandler(this, ignoreEntity, includeTriggers);
        var normalized = Vector3.Normalize(direction);
        _simulation.RayCast(origin, normalized, maxDistance, ref handler);
        return handler.Hit;
    }

    public RaycastHit3D? OverlapSphere(
        Vector3 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false)
    {
        ThrowIfDisposed();
        if (radius <= 0f || !float.IsFinite(radius) || !IsFinite(center))
            return null;

        var min = center - new Vector3(radius);
        var max = center + new Vector3(radius);
        var enumerator = new FirstOverlapEnumerator(this, center, radius, ignoreEntity, includeTriggers);
        _simulation.BroadPhase.GetOverlaps(min, max, ref enumerator);
        return enumerator.Hit;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _simulation.Dispose();
        _bufferPool.Clear();
        GC.SuppressFinalize(this);
    }

    internal void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BepuPhysicsWorld3D));
    }

    internal void RegisterBody(BepuPhysicsBody3D body)
    {
        if (body.IsStaticBody)
            _statics[body.StaticHandle.Value] = body;
        else
            _dynamics[body.BodyHandle.Value] = body;
    }

    internal void UnregisterBody(BepuPhysicsBody3D body)
    {
        if (body.IsStaticBody)
            _statics.Remove(body.StaticHandle.Value);
        else
            _dynamics.Remove(body.BodyHandle.Value);
    }

    internal void WriteMaterial(BepuPhysicsBody3D body)
    {
        // Materials are read from the body wrapper on each contact; nothing extra to store.
    }

    internal bool TryGetBody(CollidableReference collidable, out BepuPhysicsBody3D body)
    {
        if (collidable.Mobility == CollidableMobility.Static)
            return _statics.TryGetValue(collidable.StaticHandle.Value, out body!);
        return _dynamics.TryGetValue(collidable.BodyHandle.Value, out body!);
    }

    internal float GetGravityScale(BodyHandle handle) =>
        _dynamics.TryGetValue(handle.Value, out var body) ? body.GravityScale : 1f;

    internal void RecordPair(CollidableReference a, CollidableReference b, bool isTrigger)
    {
        var key = PairKey(a, b);
        _activePairs.Add(key);
        if (isTrigger)
            _triggerPairs.Add(key);
    }

    private void FlushContacts()
    {
        if (_contactListener3D is not null)
        {
            foreach (var pair in _activePairs)
            {
                if (_previousPairs.Contains(pair))
                    continue;
                if (!TryResolvePair(pair, out var a, out var b))
                    continue;
                _contactListener3D.OnContactBegin(a, b, _triggerPairs.Contains(pair));
            }

            foreach (var pair in _previousPairs)
            {
                if (_activePairs.Contains(pair))
                    continue;
                if (!TryResolvePair(pair, out var a, out var b))
                    continue;
                _contactListener3D.OnContactEnd(a, b, _previousTriggerPairs.Contains(pair));
            }
        }

        _previousPairs.Clear();
        _previousTriggerPairs.Clear();
        foreach (var pair in _activePairs)
            _previousPairs.Add(pair);
        foreach (var pair in _triggerPairs)
            _previousTriggerPairs.Add(pair);
    }

    private bool TryResolvePair((int Left, int Right) key, out BepuPhysicsBody3D a, out BepuPhysicsBody3D b)
    {
        a = null!;
        b = null!;
        return TryGetPacked(key.Left, out a) && TryGetPacked(key.Right, out b);
    }

    private bool TryGetPacked(int packed, out BepuPhysicsBody3D body)
    {
        var isStatic = packed < 0;
        var handle = packed & int.MaxValue;
        if (isStatic)
            return _statics.TryGetValue(handle, out body!);
        return _dynamics.TryGetValue(handle, out body!);
    }

    private static (int, int) PairKey(CollidableReference a, CollidableReference b)
    {
        var left = Pack(a);
        var right = Pack(b);
        return left <= right ? (left, right) : (right, left);
    }

    private static int Pack(CollidableReference collidable)
    {
        var handle = collidable.Mobility == CollidableMobility.Static
            ? collidable.StaticHandle.Value
            : collidable.BodyHandle.Value;
        var packed = handle & int.MaxValue;
        return collidable.Mobility == CollidableMobility.Static ? packed | int.MinValue : packed;
    }

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private struct ClosestRayHandler(BepuPhysicsWorld3D world, Entity? ignoreEntity, bool includeTriggers) : IRayHitHandler
    {
        public RaycastHit3D? Hit;
        private float _bestT = float.MaxValue;

        public bool AllowTest(CollidableReference collidable) => true;
        public bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (!world.TryGetBody(collidable, out var body) || body.Entity is not { } entity)
                return;
            if (ignoreEntity is not null && entity.Id == ignoreEntity.Id)
                return;
            if (body.IsSensor && !includeTriggers)
                return;
            if (t >= _bestT)
                return;

            _bestT = t;
            maximumT = t;
            Hit = new RaycastHit3D(entity, ray.Origin + ray.Direction * t, normal, t, body.IsSensor);
        }
    }

    private struct FirstOverlapEnumerator(
        BepuPhysicsWorld3D world,
        Vector3 center,
        float radius,
        Entity? ignoreEntity,
        bool includeTriggers) : IBreakableForEach<CollidableReference>
    {
        public RaycastHit3D? Hit;

        public bool LoopBody(CollidableReference collidable)
        {
            if (Hit is not null)
                return false;
            if (!world.TryGetBody(collidable, out var body) || body.Entity is not { } entity)
                return true;
            if (ignoreEntity is not null && entity.Id == ignoreEntity.Id)
                return true;
            if (body.IsSensor && !includeTriggers)
                return true;

            var delta = body.Position - center;
            if (delta.LengthSquared() > radius * radius)
                return true;

            Hit = new RaycastHit3D(entity, body.Position, Vector3.Zero, delta.Length(), body.IsSensor);
            return false;
        }
    }

    private struct NarrowPhaseCallbacks(BepuPhysicsWorld3D world) : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation) { }
        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(
            int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties(0.5f, 2f, new SpringSettings(30, 1));
            if (!world.TryGetBody(pair.A, out var bodyA) || !world.TryGetBody(pair.B, out var bodyB))
                return false;
            if (bodyA.Entity is null || bodyB.Entity is null)
                return false;

            var friction = bodyA.Friction * bodyB.Friction;
            var restitution = float.Clamp(bodyA.Restitution * bodyB.Restitution, 0f, 1f);
            pairMaterial = new PairMaterialProperties(friction, 2f, new SpringSettings(30, 1f - restitution));
            var isTrigger = bodyA.IsSensor || bodyB.IsSensor;
            world.RecordPair(pair.A, pair.B, isTrigger);
            return !isTrigger;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(
            int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            => true;
    }

    private struct PoseIntegratorCallbacks(BepuPhysicsWorld3D world, Vector3 gravity) : IPoseIntegratorCallbacks
    {
        private Vector3Wide _gravityWideDt;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt)
            => _gravityWideDt = Vector3Wide.Broadcast(gravity * dt);

        public void IntegrateVelocity(
            Vector<int> bodyIndices,
            Vector3Wide position,
            QuaternionWide orientation,
            BodyInertiaWide localInertia,
            Vector<int> integrationMask,
            int workerIndex,
            Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            Span<float> scales = stackalloc float[Vector<float>.Count];
            for (var i = 0; i < Vector<float>.Count; i++)
            {
                if (integrationMask[i] == 0)
                {
                    scales[i] = 0f;
                    continue;
                }

                var index = bodyIndices[i];
                var handle = world.Simulation.Bodies.ActiveSet.IndexToHandle[index];
                scales[i] = world.GetGravityScale(handle);
            }

            var scaleWide = new Vector<float>(scales);
            velocity.Linear += _gravityWideDt * scaleWide;
        }
    }
}
