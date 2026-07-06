using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class ScriptableEntityTemplatesTests
{
    [Fact]
    public void Generate_IncludesScriptableEntityLifecycleHooks()
    {
        var source = ScriptableEntityTemplates.Generate("PlayerController");

        source.ShouldContain("class PlayerController : ScriptableEntity");
        source.ShouldContain("public override void OnCreate()");
        source.ShouldContain("public override void OnUpdate(TimeSpan ts)");
        source.ShouldContain("public override void OnDestroy()");
        source.ShouldContain("public override void OnKeyPressed(KeyCodes key)");
    }
}
