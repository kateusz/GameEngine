using Editor.AssetPicker;
using Engine.Project;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Assets;

public class AssetPickSessionTests : IDisposable
{
    private readonly string _root;
    private readonly string _assets;

    public AssetPickSessionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ge-asset-pick-" + Guid.NewGuid().ToString("N"));
        _assets = Path.Combine(_root, "assets");
        Directory.CreateDirectory(Path.Combine(_assets, "textures"));
        File.WriteAllText(Path.Combine(_assets, "textures", "a.png"), "x");
        File.WriteAllText(Path.Combine(_assets, "textures", "b.png"), "x");
        File.WriteAllText(Path.Combine(_assets, "textures", "c.png"), "x");

        var context = Substitute.For<IProjectContext>();
        context.Root.Returns(_root);
        context.AssetsPath.Returns(_assets);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }

    [Fact]
    public void Open_with_current_path_selects_that_asset()
    {
        var project = Substitute.For<IProjectContext>();
        project.Root.Returns(_root);
        project.AssetsPath.Returns(_assets);
        var session = new AssetPickSession(project);

        session.Open(AssetKind.Texture, _ => { }, "textures/b.png");

        session.SelectedIndex.ShouldBeGreaterThanOrEqualTo(0);
        session.Filtered[session.SelectedIndex].DisplayPath.ShouldBe("assets/textures/b.png");
    }

    [Fact]
    public void Open_without_current_path_has_no_selection()
    {
        var project = Substitute.For<IProjectContext>();
        project.Root.Returns(_root);
        project.AssetsPath.Returns(_assets);
        var session = new AssetPickSession(project);

        session.Open(AssetKind.Texture, _ => { });

        session.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SetSearch_keeps_current_path_selected_when_still_visible()
    {
        var project = Substitute.For<IProjectContext>();
        project.Root.Returns(_root);
        project.AssetsPath.Returns(_assets);
        var session = new AssetPickSession(project);

        session.Open(AssetKind.Texture, _ => { }, "textures/b.png");
        session.SetSearch("b");

        session.SelectedIndex.ShouldBe(0);
        session.Filtered[0].DisplayPath.ShouldBe("assets/textures/b.png");
    }

    [Fact]
    public void SetSearch_clears_selection_when_current_path_filtered_out()
    {
        var project = Substitute.For<IProjectContext>();
        project.Root.Returns(_root);
        project.AssetsPath.Returns(_assets);
        var session = new AssetPickSession(project);

        session.Open(AssetKind.Texture, _ => { }, "textures/b.png");
        session.SetSearch("zzz");

        session.SelectedIndex.ShouldBe(-1);
    }
}
