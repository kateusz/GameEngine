using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using ECS;
using Engine.Physics;

namespace Engine.Platform.Bepu;

internal sealed class BepuPhysicsBody3D : IPhysicsBody3D
{
    private readonly BepuPhysicsWorld3D _world;
    private readonly PhysicsBodyDef3D _def;
    private BodyHandle _bodyHandle;
    private StaticHandle _staticHandle;
    private readonly bool _isStatic;
    private bool _added;
    private bool _isSensor;
    private bool _fixedRotation;
    private float _friction = 0.5f;
    private float _restitution;
    private Vector3 _localOffset;

    public BepuPhysicsBody3D(BepuPhysicsWorld3D world, PhysicsBodyDef3D def)
    {
        _world = world;
        _def = def;
        _fixedRotation = def.FixedRotation;
        GravityScale = def.GravityScale;
        _isStatic = def.MotionType == PhysicsBodyMotionType.Static;
    }

    public Entity? Entity { get; set; }

    public PhysicsBodyMotionType MotionType => _def.MotionType;

    internal BodyHandle BodyHandle => _bodyHandle;
    internal StaticHandle StaticHandle => _staticHandle;
    internal bool IsStaticBody => _isStatic;
    internal float GravityScale { get; }

    internal float Friction => _friction;
    internal float Restitution => _restitution;

    public Vector3 Position
    {
        get
        {
            var pose = Pose;
            return pose.Position - Vector3.Transform(_localOffset, pose.Orientation);
        }
        set => SetPose(value + Vector3.Transform(_localOffset, Pose.Orientation), Pose.Orientation);
    }

    public Quaternion Orientation
    {
        get => Pose.Orientation;
        set
        {
            var entityPos = Position;
            SetPose(entityPos + Vector3.Transform(_localOffset, value), value);
        }
    }

    public Vector3 LinearVelocity
    {
        get => _added && !_isStatic ? _world.Simulation.Bodies[_bodyHandle].Velocity.Linear : Vector3.Zero;
        set
        {
            if (!_added || _isStatic)
                return;

            var native = _world.Simulation.Bodies[_bodyHandle];
            native.Velocity.Linear = value;
            native.Awake = true;
        }
    }

    public bool FixedRotation
    {
        set => _fixedRotation = value;
    }

    public bool HasFixture => _added;
    public bool IsSensor => _isSensor;

    public bool IsEnabled() => _added;
    public bool IsAwake() => _added && !_isStatic && _world.Simulation.Bodies[_bodyHandle].Awake;

    public void CreateBoxFixture(in PhysicsBoxFixtureDef3D def)
    {
        var size = def.HalfExtents * 2f;
        if (size.X <= 0f || size.Y <= 0f || size.Z <= 0f)
            return;

        var box = new Box(size.X, size.Y, size.Z);
        AddShape(box, box.ComputeInertia(MassFromDensity(def.Density, size.X * size.Y * size.Z)), def.CenterOffset, def.Friction, def.Restitution, def.IsSensor);
    }

    public void CreateSphereFixture(in PhysicsSphereFixtureDef3D def)
    {
        if (def.Radius <= 0f)
            return;

        var sphere = new Sphere(def.Radius);
        var volume = 4f / 3f * MathF.PI * def.Radius * def.Radius * def.Radius;
        AddShape(sphere, sphere.ComputeInertia(MassFromDensity(def.Density, volume)), def.CenterOffset, def.Friction, def.Restitution, def.IsSensor);
    }

    public void CreateCapsuleFixture(in PhysicsCapsuleFixtureDef3D def)
    {
        if (def.Radius <= 0f || def.Length < 0f)
            return;

        var capsule = new Capsule(def.Radius, def.Length);
        var volume = MathF.PI * def.Radius * def.Radius * def.Length
                     + 4f / 3f * MathF.PI * def.Radius * def.Radius * def.Radius;
        AddShape(capsule, capsule.ComputeInertia(MassFromDensity(def.Density, volume)), def.CenterOffset, def.Friction, def.Restitution, def.IsSensor);
    }

    public void UpdateFixtureMaterial(float density, float friction, float restitution)
    {
        _friction = friction;
        _restitution = restitution;
        if (_added)
            _world.WriteMaterial(this);
    }

    public void RemoveFromSimulation()
    {
        if (!_added)
            return;

        _world.UnregisterBody(this);
        if (_isStatic)
            _world.Simulation.Statics.Remove(_staticHandle);
        else
            _world.Simulation.Bodies.Remove(_bodyHandle);

        _added = false;
        _localOffset = Vector3.Zero;
        Entity = null;
    }

    private void AddShape<TShape>(TShape shape, BodyInertia inertia, Vector3 localOffset, float friction, float restitution, bool isSensor)
        where TShape : unmanaged, IConvexShape
    {
        _world.ThrowIfDisposed();
        if (_added)
            return;

        _isSensor = isSensor;
        _friction = friction;
        _restitution = restitution;
        _localOffset = localOffset;
        var shapeIndex = _world.Simulation.Shapes.Add(shape);
        var worldOffset = Vector3.Transform(localOffset, _def.Orientation);
        var pose = new RigidPose(_def.Position + worldOffset, _def.Orientation);

        if (_isStatic)
        {
            _staticHandle = _world.Simulation.Statics.Add(new StaticDescription(pose, shapeIndex));
        }
        else if (_def.MotionType == PhysicsBodyMotionType.Kinematic)
        {
            _bodyHandle = _world.Simulation.Bodies.Add(
                BodyDescription.CreateKinematic(pose, shapeIndex, 0.01f));
        }
        else
        {
            if (_fixedRotation)
                inertia.InverseInertiaTensor = default;
            _bodyHandle = _world.Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(pose, inertia, shapeIndex, 0.01f));
        }

        _added = true;
        _world.RegisterBody(this);
    }

    private RigidPose Pose
    {
        get
        {
            if (!_added)
                return new RigidPose(_def.Position, _def.Orientation);
            return _isStatic
                ? _world.Simulation.Statics[_staticHandle].Pose
                : _world.Simulation.Bodies[_bodyHandle].Pose;
        }
    }

    private void SetPose(Vector3 position, Quaternion orientation)
    {
        if (!_added)
            return;

        var pose = new RigidPose(position, orientation);
        if (_isStatic)
            _world.Simulation.Statics[_staticHandle].Pose = pose;
        else
            _world.Simulation.Bodies[_bodyHandle].Pose = pose;
    }

    private static float MassFromDensity(float density, float volume) =>
        MathF.Max(density * volume, 1e-4f);
}
