using ECS;

namespace FlappyBird.project.assets.scripts;

[SerializableComponent]
public class ScoreDigitComponent : IGameComponent
{
    public int Place { get; set; }

    public IComponent Clone() => new ScoreDigitComponent { Place = Place };
}
