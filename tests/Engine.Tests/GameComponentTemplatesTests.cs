using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class GameComponentTemplatesTests
{
    [Fact]
    public void ToClassName_AppendsComponentSuffix()
    {
        GameComponentTemplates.ToClassName("Health").ShouldBe("HealthComponent");
    }

    [Fact]
    public void Generate_IncludesSerializableAttributeAndClone()
    {
        var source = GameComponentTemplates.Generate("HealthComponent");

        source.ShouldContain("[SerializableComponent]");
        source.ShouldContain("class HealthComponent : IGameComponent");
        source.ShouldContain("public IComponent Clone() => new HealthComponent();");
    }
}
