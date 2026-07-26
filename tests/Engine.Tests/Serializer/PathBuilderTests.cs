using Engine.Core;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Serializer;

[Collection("PathBuilder")]
public class PathBuilderTests : IDisposable
{
    private static string GameAssets =>
        OperatingSystem.IsWindows() ? @"C:\game\assets" : "/game/assets";

    private static string RootedTexturePath =>
        OperatingSystem.IsWindows() ? @"C:\game\assets\texture.png" : "/game/assets/texture.png";

    private static string AssetsPrefixedRelativePath =>
        OperatingSystem.IsWindows() ? @"assets\textures\player.png" : "assets/textures/player.png";

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
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        PathBuilder.AssetsPath.ShouldBe(GameAssets);
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
        var result = PathBuilder.Resolve(RootedTexturePath);
        result.ShouldBe(RootedTexturePath);
    }

    [Fact]
    public void Resolve_relative_path_combines_with_assets()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        var result = PathBuilder.Resolve("textures/player.png");

        result.ShouldBe(Path.GetFullPath(Path.Combine(GameAssets, "textures/player.png")));
    }

    [Fact]
    public void Resolve_assets_prefix_stripped()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        var result = PathBuilder.Resolve(AssetsPrefixedRelativePath);

        var expected = Path.GetFullPath(Path.Combine(GameAssets, "textures/player.png"));
        result.ShouldBe(expected);
    }

    [Fact]
    public void Build_delegates_to_Resolve()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        PathBuilder.Build("sprites/icon.png").ShouldBe(PathBuilder.Resolve("sprites/icon.png"));
    }

    [Fact]
    public void ToAssetRelativePath_nested_path_uses_forward_slashes_on_every_os()
    {
        // Cooked .mesh/.skel/.anim3d files ship texture/asset paths across machines —
        // a Windows cook must not embed backslashes (Path.Combine there produces '\').
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        var absolute = Path.Combine(GameAssets, "models", "textures", "skin.png");
        var relative = PathBuilder.ToAssetRelativePath(absolute);

        relative.ShouldBe("models/textures/skin.png");
    }

    [Fact]
    public void IsUnderAssets_true_for_path_inside_assets()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        PathBuilder.IsUnderAssets(RootedTexturePath).ShouldBeTrue();
    }

    [Fact]
    public void IsUnderAssets_false_for_path_outside_assets()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);

        var outside = OperatingSystem.IsWindows() ? @"C:\other\secret.png" : "/other/secret.png";
        PathBuilder.IsUnderAssets(outside).ShouldBeFalse();
    }
}
