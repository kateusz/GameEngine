using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Serialization;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class ModelFactoryMeshTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _assetsDir;
    private readonly ITextureFactory _textureFactory;
    private readonly ModelFactory _factory;

    public ModelFactoryMeshTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ModelFactoryMeshTests_" + Guid.NewGuid().ToString("N"));
        _assetsDir = Path.Combine(_tempDir, "assets");
        Directory.CreateDirectory(_assetsDir);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsDir);
        PathBuilder.UseProjectContext(context);

        _textureFactory = Substitute.For<ITextureFactory>();
        _textureFactory.Create(Arg.Any<string>(), Arg.Any<bool>()).Returns(Substitute.For<Texture2D>());
        _textureFactory.GetWhiteTexture().Returns(Substitute.For<Texture2D>());
        _textureFactory.GetBlackTexture().Returns(Substitute.For<Texture2D>());
        _textureFactory.GetFlatNormalTexture().Returns(Substitute.For<Texture2D>());

        _factory = new ModelFactory(
            _textureFactory,
            CreateVertexArrayFactory(),
            CreateVertexBufferFactory(),
            CreateIndexBufferFactory());
    }

    public void Dispose()
    {
        _factory.Dispose();
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Create_WriterProducedMesh_ReturnsNonNullModel()
    {
        var meshPath = WriteMeshFixture("triangle.mesh", albedoPath: null);

        var model = _factory.Create(meshPath);

        model.ShouldNotBeNull();
        model!.Submeshes.Count.ShouldBe(1);
        model.Submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
    }

    [Fact]
    public void Create_SameMeshPath_ReturnsCachedInstance()
    {
        var meshPath = WriteMeshFixture("cached.mesh", albedoPath: null);

        var first = _factory.Create(meshPath);
        var second = _factory.Create(meshPath);

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void Evict_ForcesReloadFromDisk()
    {
        var meshPath = WriteMeshFixture("evict.mesh", albedoPath: null);
        var first = _factory.Create(meshPath);
        first.ShouldNotBeNull();
        first!.Submeshes[0].Mesh.Vertices.Count.ShouldBe(3);

        using (var stream = File.Create(meshPath))
            MeshWriter.Write(stream, CreateQuadModel());

        _factory.Evict(meshPath);
        var reloaded = _factory.Create(meshPath);
        reloaded.ShouldNotBeNull();
        reloaded.ShouldNotBeSameAs(first);
        reloaded!.Submeshes[0].Mesh.Vertices.Count.ShouldBe(4);
    }

    [Fact]
    public void Create_GlbExtension_ReturnsNullWithoutAssimp()
    {
        var path = Path.Combine(_tempDir, "raw.glb");
        File.WriteAllBytes(path, [0x67, 0x6C, 0x54, 0x46]);

        _factory.Create(path).ShouldBeNull();
    }

    [Theory]
    [InlineData("raw.fbx")]
    [InlineData("raw.gltf")]
    public void Create_FbxOrGltfExtension_ReturnsNull(string fileName)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, "not-a-mesh");

        _factory.Create(path).ShouldBeNull();
    }

    [Fact]
    public void Create_MissingMesh_ReturnsNull()
    {
        var missing = Path.Combine(_tempDir, "does-not-exist.mesh");

        _factory.Create(missing).ShouldBeNull();
    }

    [Fact]
    public void Create_CorruptMagicMesh_ReturnsNull()
    {
        var path = Path.Combine(_tempDir, "corrupt.mesh");
        File.WriteAllBytes(path, "XXXX\x01\0\0\0\0\0\0\0"u8.ToArray());

        _factory.Create(path).ShouldBeNull();
    }

    [Fact]
    public void Create_RelativeMaterialPaths_AreResolvedBeforeTextureCreate()
    {
        const string relativeAlbedo = "models/cube/albedo.png";
        var meshPath = WriteMeshFixture("textured.mesh", albedoPath: relativeAlbedo);
        var expectedResolved = PathBuilder.Resolve(relativeAlbedo);

        var model = _factory.Create(meshPath);

        model.ShouldNotBeNull();
        _textureFactory.Received().Create(expectedResolved, sRgb: true);
        model!.Submeshes[0].Material.AlbedoTexture.ShouldNotBeNull();
    }

    private string WriteMeshFixture(string fileName, string? albedoPath)
    {
        var mesh = new Mesh("Triangle");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.UnitX, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.UnitY, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.AddRange([0u, 1u, 2u]);

        var material = new MeshMaterial
        {
            Metallic = 0f,
            Roughness = 0.5f,
            AlbedoTexturePath = albedoPath
        };

        var model = new Model([new ModelSubmesh(mesh, material)]);
        var path = Path.Combine(_tempDir, fileName);
        using (var stream = File.Create(path))
            MeshWriter.Write(stream, model);

        return path;
    }

    private static Model CreateQuadModel()
    {
        var mesh = new Mesh("Quad");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.UnitX, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.UnitY, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.UnitZ, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.AddRange([0u, 1u, 2u, 0u, 2u, 3u]);
        return new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
    }

    private static IVertexArrayFactory CreateVertexArrayFactory()
    {
        var factory = Substitute.For<IVertexArrayFactory>();
        factory.Create().Returns(_ => Substitute.For<IVertexArray>());
        return factory;
    }

    private static IVertexBufferFactory CreateVertexBufferFactory()
    {
        var factory = Substitute.For<IVertexBufferFactory>();
        factory.Create(Arg.Any<uint>()).Returns(_ => Substitute.For<IVertexBuffer>());
        return factory;
    }

    private static IIndexBufferFactory CreateIndexBufferFactory()
    {
        var factory = Substitute.For<IIndexBufferFactory>();
        factory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(_ => Substitute.For<IIndexBuffer>());
        return factory;
    }
}
