using ArenaShooter.assets.scripts;
using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class ScriptEngineLoadTests
{
    [Fact]
    public void LoadGameAssemblyFromFile_LoadsAssembly()
    {
        var engine = new ScriptEngine();
        engine.LoadGameAssemblyFromFile(typeof(ArenaSystem).Assembly.Location);

        engine.GetLoadedGameAssembly().ShouldNotBeNull();
        engine.GetLoadedGameAssembly()!.GetName().Name.ShouldBe("ArenaShooter");
    }
}
