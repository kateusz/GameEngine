using Editor.Features.Scripting;
using Shouldly;

namespace Editor.Tests.Scripting;

public class ScriptNameHintsTests
{
    [Fact]
    public void IdentifierPrefix_TakesWordBeforeColumn()
    {
        ScriptNameHints.IdentifierPrefix("    Scriptable", "    Scriptable".Length).ShouldBe("Scriptable");
        ScriptNameHints.IdentifierPrefix("GetComponent<Scr", "GetComponent<Scr".Length).ShouldBe("Scr");
        ScriptNameHints.IdentifierPrefix("x.OnUp", "x.OnUp".Length).ShouldBe("OnUp");
        ScriptNameHints.IdentifierPrefix("\tScr", 7).ShouldBe("Scr");
    }

    [Fact]
    public void Match_RequiresTwoCharacters()
    {
        ScriptNameHints.Match("S").ShouldBeEmpty();
        ScriptNameHints.Match("OnUp").ShouldContain("OnUpdate");
        ScriptNameHints.Match("GetCo").ShouldContain("GetComponent");
    }

    [Fact]
    public void Match_IncludesExtraNames()
    {
        ScriptNameHints.Match("Scr", ["Script_001"]).ShouldContain("Script_001");
    }
}
