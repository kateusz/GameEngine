using Engine.Core;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Serializer;

public class PathBuilderTests : IDisposable
{
    public PathBuilderTests()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
    }

    [Fact]
    public void AssetsPath_before_initialization_throws()
    {
        PathBuilder.UseProjectContext(null!);

        var ex = Should.Throw<InvalidOperationException>(() => _ = PathBuilder.AssetsPath);
        ex.Message.ShouldContain("not initialized");
    }

    [Fact]
    public void AssetsPath_after_initialization_returns_context_path()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(@"C:\game\assets");
        PathBuilder.UseProjectContext(context);

        PathBuilder.AssetsPath.ShouldBe(@"C:\game\assets");
    }

    [Fact]
    public void Resolve_null_or_whitespace_returns_input()
    {
        PathBuilder.Resolve(null!).ShouldBeNull();
        PathBuilder.Resolve("").ShouldBe("");
        PathBuilder.Resolve("   ").ShouldBe("   ");
    }

    [Fact]
    public void Resolve_rooted_path_returns_normalized()
    {
        var result = PathBuilder.Resolve(@"C:\game\assets\texture.png");
        result.ShouldBe(@"C:\game\assets\texture.png");
    }

    [Fact]
    public void Resolve_relative_path_combines_with_assets()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(@"C:\game\assets");
        PathBuilder.UseProjectContext(context);

        var result = PathBuilder.Resolve(@"textures/player.png");

        result.ShouldBe(Path.GetFullPath(Path.Combine(@"C:\game\assets", @"textures/player.png")));
    }

    [Fact]
    public void Resolve_assets_prefix_stripped()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(@"C:\game\assets");
        PathBuilder.UseProjectContext(context);

        var result = PathBuilder.Resolve(@"assets\textures\player.png");

        var expected = Path.GetFullPath(Path.Combine(@"C:\game\assets", @"textures\player.png"));
        result.ShouldBe(expected);
    }

    [Fact]
    public void Build_delegates_to_Resolve()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(@"C:\game\assets");
        PathBuilder.UseProjectContext(context);

        PathBuilder.Build(@"sprites/icon.png").ShouldBe(PathBuilder.Resolve(@"sprites/icon.png"));
    }
}
