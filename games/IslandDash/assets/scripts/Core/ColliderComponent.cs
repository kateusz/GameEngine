using ECS;

namespace IslandDash;

/// <summary>
/// Axis-aligned box collider for the game-side collision world (the engine only ships 2D Box2D physics).
/// Rotation of the owning transform is ignored.
/// </summary>
public sealed class ColliderComponent : IGameComponent
{
    public Vector3 HalfExtents { get; set; } = new(0.5f);

    /// <summary>Box center relative to the entity's translation.</summary>
    public Vector3 Offset { get; set; }

    /// <summary>Blocks movement (ground, walls, crates). Non-solid colliders are only used for overlap queries.</summary>
    public bool Solid { get; set; } = true;

    public bool Enabled { get; set; } = true;

    /// <summary>Spring-arm camera collides with this box.</summary>
    public bool BlocksCamera { get; set; } = true;

    /// <summary>World-space movement applied this frame; riders standing on top are carried by it.</summary>
    public Vector3 FrameDelta { get; set; }

    public IComponent Clone() => new ColliderComponent
    {
        HalfExtents = HalfExtents,
        Offset = Offset,
        Solid = Solid,
        Enabled = Enabled,
        BlocksCamera = BlocksCamera,
        FrameDelta = FrameDelta
    };
}
