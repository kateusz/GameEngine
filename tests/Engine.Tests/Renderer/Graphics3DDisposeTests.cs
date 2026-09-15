using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class Graphics3DDisposeTests
{
    [Fact]
    public void Dispose_DoesNotDisposeFactoryShadersOrCubeMesh()
    {
        var shader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(shader);

        var mesh = new Mesh("cube");
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(mesh);

        var graphics = new Graphics3D(
            Substitute.For<IRendererAPI>(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>());
        graphics.Init();
        graphics.Dispose();

        shader.DidNotReceive().Dispose();
        MeshDisposed(mesh).ShouldBeFalse();
    }

    private static bool MeshDisposed(Mesh mesh)
    {
        var field = typeof(Mesh).GetField("_disposed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)field!.GetValue(mesh)!;
    }
}
