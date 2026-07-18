using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;
using Silk.NET.Assimp;

namespace Engine.Tests.Components;

public class ModelRendererComponentTests
{
    [Fact]
    public void ModelRendererComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var component = new ModelRendererComponent();

        component.Color.ShouldBe(Vector4.One);
        component.ModelPath.ShouldBeNull();
    }

    [Fact]
    public void ModelRendererComponent_Clone_ShouldCopyAllProperties()
    {
        var original = new ModelRendererComponent(new Vector4(0.2f, 0.4f, 0.6f, 0.8f))
        {
            ModelPath = "models/crate.fbx"
        };

        var clone = (ModelRendererComponent)original.Clone();
        clone.Color = Vector4.Zero;

        clone.ShouldNotBeSameAs(original);
        clone.ModelPath.ShouldBe("models/crate.fbx");
        clone.Color.ShouldBe(Vector4.Zero);
        original.Color.ShouldBe(new Vector4(0.2f, 0.4f, 0.6f, 0.8f));
    }
}

public class AssimpModelImporterTests : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();
    private readonly string _objPath;
    private readonly string _gltfPath;

    public AssimpModelImporterTests()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "TestAssets");
        Directory.CreateDirectory(assetsDir);
        EnsureGltfBinary(assetsDir);
        _objPath = Path.Combine(assetsDir, "triangle.obj");
        _gltfPath = Path.Combine(assetsDir, "triangle.gltf");
    }

    private static void EnsureGltfBinary(string assetsDir)
    {
        var binPath = Path.Combine(assetsDir, "triangle.bin");
        using var stream = System.IO.File.Create(binPath);
        using var writer = new BinaryWriter(stream);
        writer.Write(0f); writer.Write(0f); writer.Write(0f);
        writer.Write(1f); writer.Write(0f); writer.Write(0f);
        writer.Write(0f); writer.Write(1f); writer.Write(0f);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)2);
    }

    [Fact]
    public void Import_ObjTriangle_ShouldProduceOneSubmeshWithGeometry()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(_objPath);

        submeshes.Count.ShouldBe(1);
        submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
        submeshes[0].Mesh.Indices.Count.ShouldBe(3);
        submeshes[0].Material.Shininess.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Import_GltfTriangle_ShouldProduceOneSubmeshWithGeometry()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(_gltfPath);

        submeshes.Count.ShouldBe(1);
        submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
        submeshes[0].Mesh.Indices.Count.ShouldBe(3);
    }

    [Fact]
    public void Import_MissingFile_ShouldReturnEmpty()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj"));

        submeshes.ShouldBeEmpty();
    }

    public void Dispose() => _assimp.Dispose();
}

public class ModelFactoryTests : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();

    [Fact]
    public void Create_MissingPath_ShouldReturnNull()
    {
        var factory = CreateFactory();

        factory.Create(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj"))
            .ShouldBeNull();
    }

    [Fact]
    public void Create_InvalidPath_ShouldNotCacheSuccess()
    {
        var factory = CreateFactory();
        var missing = Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj");

        factory.Create(missing).ShouldBeNull();
        factory.Create(missing).ShouldBeNull();
    }

    private ModelFactory CreateFactory()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        textureFactory.GetWhiteTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetBlackTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetFlatNormalTexture().Returns(Substitute.For<Texture2D>());

        return new ModelFactory(
            textureFactory,
            Substitute.For<Engine.Renderer.Buffers.VertexArray.IVertexArrayFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IVertexBufferFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IIndexBufferFactory>(),
            _assimp);
    }

    public void Dispose() => _assimp.Dispose();
}
