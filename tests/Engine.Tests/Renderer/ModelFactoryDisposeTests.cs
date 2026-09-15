using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class ModelFactoryDisposeTests
{
    [Fact]
    public void Clear_DisposesCachedModelMeshes_AndAllowsCreateAgain()
    {
        var path = Path.GetTempFileName();
        try
        {
            var vao = Substitute.For<IVertexArray>();
            var factory = CreateFactory(path, vao);

            factory.Create(path).ShouldNotBeNull();
            factory.Clear();
            vao.Received(1).Dispose();

            factory.Create(path).ShouldNotBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_DisposesCachedModels_AndRejectsFurtherCreate()
    {
        var path = Path.GetTempFileName();
        try
        {
            var vao = Substitute.For<IVertexArray>();
            var factory = CreateFactory(path, vao);

            factory.Create(path).ShouldNotBeNull();
            factory.Dispose();

            vao.Received(1).Dispose();
            Should.Throw<ObjectDisposedException>(() => factory.Create(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryGet_AfterCreate_ReturnsCachedModel_WithoutImport()
    {
        var path = Path.GetTempFileName();
        var imports = 0;
        try
        {
            var factory = CreateFactory(path, Substitute.For<IVertexArray>(), _ =>
            {
                imports++;
                return ([NewMesh()], [MeshMaterial.Default], null);
            });

            factory.TryGet(path, out _).ShouldBeFalse();
            factory.Create(path).ShouldNotBeNull();
            factory.TryGet(path, out var model).ShouldBeTrue();
            model.ShouldNotBeNull();
            factory.Create(path);
            imports.ShouldBe(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Create_MissingFile_RetriesWhenFileAppears()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        var imports = 0;
        var factory = CreateFactory(path, Substitute.For<IVertexArray>(), _ =>
        {
            imports++;
            return ([NewMesh()], [MeshMaterial.Default], null);
        });

        factory.Create(path).ShouldBeNull();
        imports.ShouldBe(0);

        File.WriteAllBytes(path, [1]);
        try
        {
            factory.Create(path).ShouldNotBeNull();
            imports.ShouldBe(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Create_PartialInitializeFailure_DisposesAllMeshes_AndReturnsNull()
    {
        var path = Path.GetTempFileName();
        try
        {
            var vaoFactory = Substitute.For<IVertexArrayFactory>();
            vaoFactory.Create().Returns(
                _ => throw new InvalidOperationException("gpu"),
                _ => Substitute.For<IVertexArray>());
            var factory = new ModelFactory(
                _ => ([NewMesh(), NewMesh()], [MeshMaterial.Default, MeshMaterial.Default], null),
                vaoFactory,
                Substitute.For<IVertexBufferFactory>(),
                Substitute.For<IIndexBufferFactory>());

            factory.Create(path).ShouldBeNull();
            factory.TryGet(path, out _).ShouldBeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ModelFactory CreateFactory(
        string path,
        IVertexArray vao,
        Func<string, (IReadOnlyList<Mesh> Submeshes, IReadOnlyList<MeshMaterial> Materials, ModelSceneNode? SceneGraph)>? import = null)
    {
        var vbo = Substitute.For<IVertexBuffer>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(3);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(vbo);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        return new ModelFactory(
            import ?? (_ => ([NewMesh()], [MeshMaterial.Default], null)),
            vaoFactory,
            vboFactory,
            iboFactory);
    }

    private static Mesh NewMesh()
    {
        var mesh = new Mesh("m");
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Indices.AddRange([0u, 1u, 2u]);
        return mesh;
    }
}
