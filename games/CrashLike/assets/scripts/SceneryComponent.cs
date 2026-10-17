using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// Parallax prop. Factor 1 stays locked to the level; lower values drift with the camera.
/// </summary>
[SerializableComponent]
public class SceneryComponent : IGameComponent
{
    public float HomeX { get; set; }
    public float HomeY { get; set; }
    public float Factor { get; set; } = 1f;

    public IComponent Clone() => new SceneryComponent
    {
        HomeX = HomeX,
        HomeY = HomeY,
        Factor = Factor
    };
}
