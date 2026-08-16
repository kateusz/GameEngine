using Engine.Core;
using Engine.Renderer;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

/// <summary>
/// Group 8 gap: TextureRelocator was only covered indirectly via MeshCreator texture cooks.
/// </summary>
[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class TextureRelocatorTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _assetsRoot;

    public TextureRelocatorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "TextureRelocatorTests_" + Guid.NewGuid().ToString("N"));
        _assetsRoot = Path.Combine(_tempRoot, "assets");
        Directory.CreateDirectory(_assetsRoot);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsRoot);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ponytail: temp cleanup best-effort
        }
    }

    [Fact]
    public void Relocate_MissingSource_ClearsPath()
    {
        var material = new MeshMaterial { AlbedoTexturePath = Path.Combine(_tempRoot, "gone.png") };
        var submeshes = new List<ModelSubmesh> { new(new Mesh("m"), material) };

        TextureRelocator.Relocate(submeshes, _assetsRoot);

        material.AlbedoTexturePath.ShouldBeNull();
    }

    [Fact]
    public void Relocate_CopiesUnderModels_AndRewritesRelative()
    {
        var sourceTex = Path.Combine(_tempRoot, "albedo.png");
        File.WriteAllBytes(sourceTex, [1, 2, 3, 4]);

        var material = new MeshMaterial { AlbedoTexturePath = sourceTex };
        var submeshes = new List<ModelSubmesh> { new(new Mesh("m"), material) };

        TextureRelocator.Relocate(submeshes, _assetsRoot);

        material.AlbedoTexturePath.ShouldBe("models/textures/albedo.png");
        File.Exists(Path.Combine(_assetsRoot, "models", "textures", "albedo.png")).ShouldBeTrue();
    }

    [Fact]
    public void Relocate_SameSource_DedupesToSingleCopy()
    {
        var sourceTex = Path.Combine(_tempRoot, "shared.png");
        File.WriteAllBytes(sourceTex, [9]);

        var matA = new MeshMaterial { AlbedoTexturePath = sourceTex };
        var matB = new MeshMaterial { NormalTexturePath = sourceTex };
        var submeshes = new List<ModelSubmesh>
        {
            new(new Mesh("a"), matA),
            new(new Mesh("b"), matB)
        };

        TextureRelocator.Relocate(submeshes, _assetsRoot);

        matA.AlbedoTexturePath.ShouldBe("models/textures/shared.png");
        matB.NormalTexturePath.ShouldBe("models/textures/shared.png");
        Directory.GetFiles(Path.Combine(_assetsRoot, "models", "textures")).Length.ShouldBe(1);
    }

    [Fact]
    public void Relocate_NameCollision_OverwritesExisting()
    {
        var texturesDir = Path.Combine(_assetsRoot, "models", "textures");
        Directory.CreateDirectory(texturesDir);
        File.WriteAllBytes(Path.Combine(texturesDir, "albedo.png"), [0]);

        var sourceTex = Path.Combine(_tempRoot, "albedo.png");
        File.WriteAllBytes(sourceTex, [1]);

        var material = new MeshMaterial { AlbedoTexturePath = sourceTex };
        TextureRelocator.Relocate([new ModelSubmesh(new Mesh("m"), material)], _assetsRoot);

        material.AlbedoTexturePath.ShouldBe("models/textures/albedo.png");
        File.ReadAllBytes(Path.Combine(texturesDir, "albedo.png")).ShouldBe(new byte[] { 1 });
        Directory.GetFiles(texturesDir).Length.ShouldBe(1);
    }

    [Fact]
    public void Relocate_SourceAlreadyAtDestination_RewritesWithoutCopyingOntoSelf()
    {
        var texturesDir = Path.Combine(_assetsRoot, "models", "textures");
        Directory.CreateDirectory(texturesDir);
        var sourceTex = Path.Combine(texturesDir, "albedo.png");
        File.WriteAllBytes(sourceTex, [1, 2, 3]);

        var material = new MeshMaterial { AlbedoTexturePath = sourceTex };
        TextureRelocator.Relocate([new ModelSubmesh(new Mesh("m"), material)], _assetsRoot);

        material.AlbedoTexturePath.ShouldBe("models/textures/albedo.png");
        File.ReadAllBytes(sourceTex).ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public void Relocate_SourceAlreadyAtDestination_DifferentDirectoryCase_RewritesWithoutCopyingOntoSelf()
    {
        var onDiskDir = Path.Combine(_assetsRoot, "models", "Textures");
        Directory.CreateDirectory(onDiskDir);
        var sourceTex = Path.Combine(onDiskDir, "albedo.png");
        File.WriteAllBytes(sourceTex, [7]);

        var material = new MeshMaterial { AlbedoTexturePath = sourceTex };
        TextureRelocator.Relocate([new ModelSubmesh(new Mesh("m"), material)], _assetsRoot);

        material.AlbedoTexturePath.ShouldBe("models/textures/albedo.png");
        File.ReadAllBytes(sourceTex).ShouldBe(new byte[] { 7 });
    }
}
