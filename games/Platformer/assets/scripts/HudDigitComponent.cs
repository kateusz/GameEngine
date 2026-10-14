using ECS;

namespace Platformer.assets.scripts;

/// <summary>One score digit. Place 0 is the leftmost (most significant) place.</summary>
[SerializableComponent]
public class HudDigitComponent : IGameComponent
{
    public int Place { get; set; }

    public IComponent Clone() => new HudDigitComponent { Place = Place };
}
