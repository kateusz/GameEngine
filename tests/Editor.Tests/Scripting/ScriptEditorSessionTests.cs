using Editor.Features.Scripting;
using Shouldly;

namespace Editor.Tests.Scripting;

public class ScriptEditorSessionTests
{
    [Fact]
    public void TryLoad_MissingPath_DoesNotOpen()
    {
        var session = new ScriptEditorSession();

        session.TryLoad("Foo", path: null, text: "class Foo {}").ShouldBeFalse();
        session.IsOpen.ShouldBeFalse();
        session.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void TryLoad_ThenEdit_IsDirty_DiscardRestores()
    {
        var session = new ScriptEditorSession();
        session.TryLoad("Foo", @"C:\game\assets\scripts\Foo.cs", "class Foo {}").ShouldBeTrue();
        session.IsDirty.ShouldBeFalse();

        session.Text = "class Foo { void Bar() {} }";
        session.IsDirty.ShouldBeTrue();

        session.Discard();
        session.Text.ShouldBe("class Foo {}");
        session.IsDirty.ShouldBeFalse();
        session.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ApplySave_CompileOk_ClearsDirtyAndErrors()
    {
        var session = new ScriptEditorSession();
        session.TryLoad("Foo", "Foo.cs", "old");
        session.Text = "new";

        session.ApplySave(persisted: true, compileOk: true, ["should not stick"]);

        session.IsDirty.ShouldBeFalse();
        session.Snapshot.ShouldBe("new");
        session.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ApplySave_CompileFailButPersisted_NotDirty_KeepsErrors()
    {
        var session = new ScriptEditorSession();
        session.TryLoad("Foo", "Foo.cs", "old");
        session.Text = "broken";

        session.ApplySave(persisted: true, compileOk: false, ["error CS1002"]);

        session.IsDirty.ShouldBeFalse();
        session.Errors.ShouldBe(["error CS1002"]);
    }

    [Fact]
    public void ApplySave_IoFail_StaysDirty()
    {
        var session = new ScriptEditorSession();
        session.TryLoad("Foo", "Foo.cs", "old");
        session.Text = "new";

        session.ApplySave(persisted: false, compileOk: false, ["access denied"]);

        session.IsDirty.ShouldBeTrue();
        session.Errors.ShouldBe(["access denied"]);
        session.Snapshot.ShouldBe("old");
    }
}
