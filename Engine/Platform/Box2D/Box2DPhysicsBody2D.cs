using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using ECS;
using Engine.Physics;

namespace Engine.Platform.Box2D;

internal sealed class Box2DPhysicsBody2D(Body body) : IPhysicsBody2D
{
    public Entity? Entity { get; set; }

    public PhysicsBodyMotionType MotionType => body.Type() switch
    {
        BodyType.Static => PhysicsBodyMotionType.Static,
        BodyType.Dynamic => PhysicsBodyMotionType.Dynamic,
        BodyType.Kinematic => PhysicsBodyMotionType.Kinematic,
        _ => PhysicsBodyMotionType.Static
    };

    public Vector2 Position
    {
        get => body.GetPosition();
        set => body.SetTransform(value, body.GetAngle());
    }

    public float Angle
    {
        get => body.GetAngle();
        set => body.SetTransform(body.GetPosition(), value);
    }

    public Vector2 LinearVelocity
    {
        get => body.GetLinearVelocity();
        set => body.SetLinearVelocity(value);
    }

    public bool FixedRotation
    {
        set => body.SetFixedRotation(value);
    }

    public bool HasFixture => body.GetFixtureList() != null;

    public bool IsSensor => body.GetFixtureList()?.IsSensor() ?? false;

    public bool IsEnabled() => body.IsEnabled();

    public bool IsAwake() => body.IsAwake();

    public void CreateBoxFixture(in PhysicsBoxFixtureDef def)
    {
        var shape = new PolygonShape();
        shape.SetAsBox(def.HalfWidth, def.HalfHeight, def.CenterOffset, 0.0f);
        CreateFixture(shape, def.Density, def.Friction, def.Restitution, def.IsSensor);
    }

    public void CreateCircleFixture(in PhysicsCircleFixtureDef def)
    {
        if (def.Radius <= 0f)
            return;

        var shape = new CircleShape();
        shape.Center = def.CenterOffset;
        shape.Radius = def.Radius;
        CreateFixture(shape, def.Density, def.Friction, def.Restitution, def.IsSensor);
    }

    public void CreateEdgeFixture(in PhysicsEdgeFixtureDef def)
    {
        var points = def.Points;
        if (points is null || points.Length < 2)
            return;

        var chain = new ChainShape();
        var vertices = (Vector2[])points.Clone();
        var prev = vertices[0] - (vertices[1] - vertices[0]);
        var next = vertices[^1] + (vertices[^1] - vertices[^2]);
        chain.CreateChain(in vertices, in prev, in next);
        CreateFixture(chain, def.Density, def.Friction, def.Restitution, def.IsSensor);
    }

    private void CreateFixture(Shape shape, float density, float friction, float restitution, bool isSensor)
    {
        var fixtureDef = new FixtureDef
        {
            shape = shape,
            density = density,
            friction = friction,
            restitution = restitution,
            isSensor = isSensor
        };
        body.CreateFixture(fixtureDef);
    }

    public void UpdateFixtureMaterial(float density, float friction, float restitution)
    {
        var fixture = body.GetFixtureList();
        if (fixture == null)
            return;

        if (fixture.Density == density &&
            fixture.m_friction == friction &&
            fixture.Restitution == restitution)
            return;

        fixture.Density = density;
        fixture.m_friction = friction;
        fixture.Restitution = restitution;
    }

    internal Body NativeBody => body;
}
