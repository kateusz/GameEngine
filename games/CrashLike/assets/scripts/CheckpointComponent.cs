using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class CheckpointComponent : IGameComponent
{
    public int Id { get; set; }
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public float SpawnZ { get; set; }
    public bool Activated { get; set; }
    public float HalfWidth { get; set; } = 0.35f;
    public float HalfHeight { get; set; } = 0.8f;
    public float HalfDepth { get; set; } = 0.45f;
    public string IdleTexture { get; set; } = "textures/items/checkpoint.png";
    public string ActiveTexture { get; set; } = "textures/items/checkpoint_on.png";

    public IComponent Clone() => new CheckpointComponent
    {
        Id = Id,
        SpawnX = SpawnX,
        SpawnY = SpawnY,
        SpawnZ = SpawnZ,
        Activated = Activated,
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight,
        HalfDepth = HalfDepth,
        IdleTexture = IdleTexture,
        ActiveTexture = ActiveTexture
    };
}
