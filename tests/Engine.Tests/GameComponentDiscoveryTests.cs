using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class GameComponentDiscoveryTests
{
    [Fact]
    public void DiscoverFromScriptsDir_FindsIGameComponentClasses()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ge-components-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "HealthComponent.cs"), """
                using ECS;

                public class HealthComponent : IGameComponent
                {
                    public IComponent Clone() => new HealthComponent();
                }
                """);

            File.WriteAllText(Path.Combine(dir, "PlayerScript.cs"), """
                using Scripting;

                public class PlayerScript : ScriptableEntity
                {
                    public PlayerScript(IComponentAccessor a) : base(a) {}
                }
                """);

            var names = GameComponentDiscovery.DiscoverFromScriptsDir(dir);

            names.ShouldBe(["HealthComponent"]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
