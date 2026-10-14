using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class HudDigitComponent : IGameComponent
{
    /// <summary>0 = tens, 1 = ones.</summary>
    public int Place { get; set; }

    public IComponent Clone() => new HudDigitComponent { Place = Place };
}
