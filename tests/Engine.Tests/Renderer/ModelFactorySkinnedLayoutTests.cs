using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class ModelFactorySkinnedLayoutTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _assetsDir;
    private readonly ModelFactory _factory;
    private readonly IVertexBuffer _capturedBuffer;

    public ModelFactorySkinnedLayoutTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ModelFactorySkinnedLayoutTests_" + Guid.NewGuid().ToString("N"));
        _assetsDir = Path.Combine(_tempDir, "assets");
        Directory.CreateDirectory(_assetsDir);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsDir);
        PathBuilder.UseProjectContext(context);

        var textureFactory = Substitute.For<ITextureFactory>();
        textureFactory.Create(Arg.Any<string>(), Arg.Any<bool>()).Returns(Substitute.For<Texture2D>());

        _capturedBuffer = Substitute.For<IVertexBuffer>();
        var vertexBufferFactory = Substitute.For<IVertexBufferFactory>();
        vertexBufferFactory.Create(Arg.Any<uint>()).Returns(_capturedBuffer);

        var vertexArrayFactory = Substitute.For<IVertexArrayFactory>();
        vertexArrayFactory.Create().Returns(Substitute.For<IVertexArray>());

        var indexBufferFactory = Substitute.For<IIndexBufferFactory>();
        indexBufferFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(Substitute.For<IIndexBuffer>());

        _factory = new ModelFactory(textureFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
    }

    public void Dispose()
    {
        _factory.Dispose();
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Create_V2MeshWithBoneAttrs_UploadsExtendedLayoutAndStride()
    {
        var meshPath = WriteSkinnedMeshFixture("skinned.mesh");

        var model = _factory.Create(meshPath);

        model.ShouldNotBeNull();
        _capturedBuffer.Received(1).SetLayout(Arg.Is<BufferLayout>(layout =>
            layout.Stride == Mesh.Vertex.GetSize()
            && layout.Stride == 88
            && layout.Elements.Any(e => e.Name == "a_BoneIndex" && e.Type == Engine.Renderer.Shaders.ShaderDataType.Float4)
            && layout.Elements.Any(e => e.Name == "a_BoneWeight" && e.Type == Engine.Renderer.Shaders.ShaderDataType.Float4)));
        _capturedBuffer.Received(1).SetMeshData(
            Arg.Is<List<Mesh.Vertex>>(verts =>
                verts.Count == 1
                && verts[0].BoneIndex == new Vector4(1, 2, 0, 0)
                && verts[0].BoneWeight == new Vector4(0.6f, 0.4f, 0f, 0f)),
            88);
    }

    [Fact]
    public void Create_SameSkinnedMeshPath_ReturnsSameMeshInstance()
    {
        var meshPath = WriteSkinnedMeshFixture("cached-skinned.mesh");

        var first = _factory.Create(meshPath);
        var second = _factory.Create(meshPath);

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
        second!.Submeshes[0].Mesh.ShouldBeSameAs(first!.Submeshes[0].Mesh);
    }

    private string WriteSkinnedMeshFixture(string fileName)
    {
        var mesh = new Mesh("Skinned");
        mesh.Vertices.Add(new Mesh.Vertex(
            Vector3.Zero,
            Vector3.UnitY,
            Vector2.Zero,
            Vector3.UnitX,
            Vector3.UnitZ,
            new Vector4(1, 2, 0, 0),
            new Vector4(0.6f, 0.4f, 0f, 0f)));
        mesh.Indices.AddRange([0u, 0u, 0u]);

        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
        var path = Path.Combine(_tempDir, fileName);
        using (var stream = File.Create(path))
            MeshWriter.Write(stream, model);

        return path;
    }
}
