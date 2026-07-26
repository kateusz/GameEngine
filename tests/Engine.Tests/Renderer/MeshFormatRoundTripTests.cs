using System.Numerics;
using System.Text;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshFormatRoundTripTests
{
    [Fact]
    public void RoundTrip_SingleSubmesh_PreservesVertexStrideIndicesPbrAndTexturePaths()
    {
        var vertex = new Mesh.Vertex(
            new Vector3(1, 2, 3),
            new Vector3(0, 1, 0),
            new Vector2(0.5f, 0.25f),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1));

        var mesh = new Mesh("Cube");
        mesh.Vertices.Add(vertex);
        mesh.Vertices.Add(vertex with { Position = new Vector3(4, 5, 6) });
        mesh.Vertices.Add(vertex with { Position = new Vector3(7, 8, 9) });
        mesh.Indices.AddRange([0u, 1u, 2u]);

        var material = new MeshMaterial
        {
            Metallic = 0.75f,
            Roughness = 0.35f,
            AlbedoTexturePath = "models/cube/albedo.png",
            MetallicRoughnessTexturePath = "models/cube/mr.png",
            NormalTexturePath = "models/cube/normal.png"
        };

        var model = new Model([new ModelSubmesh(mesh, material)]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        loaded.Submeshes.Count.ShouldBe(1);
        var sub = loaded.Submeshes[0];
        sub.Mesh.Name.ShouldBe("Cube");
        Mesh.Vertex.GetSize().ShouldBe(56);
        sub.Mesh.Vertices.Count.ShouldBe(3);
        sub.Mesh.Vertices[0].ShouldBe(vertex);
        sub.Mesh.Vertices[1].Position.ShouldBe(new Vector3(4, 5, 6));
        sub.Mesh.Indices.ShouldBe([0u, 1u, 2u]);
        sub.Material.Metallic.ShouldBe(0.75f);
        sub.Material.Roughness.ShouldBe(0.35f);
        sub.Material.AlbedoTexturePath.ShouldBe("models/cube/albedo.png");
        sub.Material.MetallicRoughnessTexturePath.ShouldBe("models/cube/mr.png");
        sub.Material.NormalTexturePath.ShouldBe("models/cube/normal.png");
        sub.Material.AlbedoTexture.ShouldBeNull();
        sub.Material.MetallicRoughnessTexture.ShouldBeNull();
        sub.Material.NormalTexture.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_MultiSubmesh_PreservesNamesAndIndependentMaterials()
    {
        var meshA = new Mesh("Body");
        meshA.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        meshA.Indices.Add(0);

        var meshB = new Mesh("Lid");
        meshB.Vertices.Add(new Mesh.Vertex(Vector3.One, Vector3.UnitY, Vector2.One, Vector3.UnitX, Vector3.UnitZ));
        meshB.Indices.AddRange([0u, 0u, 0u]);

        var model = new Model([
            new ModelSubmesh(meshA, new MeshMaterial
            {
                Metallic = 0.1f,
                Roughness = 0.9f,
                AlbedoTexturePath = "models/box/body_albedo.png"
            }),
            new ModelSubmesh(meshB, new MeshMaterial
            {
                Metallic = 1f,
                Roughness = 0.05f,
                NormalTexturePath = "models/box/lid_normal.png"
            })
        ]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        loaded.Submeshes.Count.ShouldBe(2);
        loaded.Submeshes[0].Mesh.Name.ShouldBe("Body");
        loaded.Submeshes[0].Material.Metallic.ShouldBe(0.1f);
        loaded.Submeshes[0].Material.Roughness.ShouldBe(0.9f);
        loaded.Submeshes[0].Material.AlbedoTexturePath.ShouldBe("models/box/body_albedo.png");
        loaded.Submeshes[0].Material.NormalTexturePath.ShouldBeNull();

        loaded.Submeshes[1].Mesh.Name.ShouldBe("Lid");
        loaded.Submeshes[1].Material.Metallic.ShouldBe(1f);
        loaded.Submeshes[1].Material.Roughness.ShouldBe(0.05f);
        loaded.Submeshes[1].Material.NormalTexturePath.ShouldBe("models/box/lid_normal.png");
        loaded.Submeshes[1].Material.AlbedoTexturePath.ShouldBeNull();
        loaded.Submeshes[1].Mesh.Indices.ShouldBe([0u, 0u, 0u]);
    }

    [Fact]
    public void RoundTrip_EmptyTexturePath_IsAbsent()
    {
        var mesh = new Mesh("EmptyPaths");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.Add(0);

        var model = new Model([
            new ModelSubmesh(mesh, new MeshMaterial
            {
                AlbedoTexturePath = "",
                MetallicRoughnessTexturePath = "",
                NormalTexturePath = ""
            })
        ]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        var mat = loaded.Submeshes[0].Material;
        mat.AlbedoTexturePath.ShouldBeNull();
        mat.MetallicRoughnessTexturePath.ShouldBeNull();
        mat.NormalTexturePath.ShouldBeNull();
    }

    [Fact]
    public void Read_UnknownMagic_Throws()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("XXXX"));
            writer.Write(1u);
            writer.Write(0u);
        }

        stream.Position = 0;
        Should.Throw<InvalidDataException>(() => MeshReader.Read(stream));
    }

    [Fact]
    public void Read_UnsupportedVersion_Throws()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("GEMH"));
            writer.Write(2u);
            writer.Write(0u);
        }

        stream.Position = 0;
        Should.Throw<NotSupportedException>(() => MeshReader.Read(stream));
    }

    [Fact]
    public void Write_ProducesLittleEndianMagicAndVersionHeader()
    {
        var mesh = new Mesh("Header");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.Add(0);
        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);

        var bytes = stream.ToArray();
        bytes.Length.ShouldBeGreaterThanOrEqualTo(8);
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("GEMH");
        bytes[4].ShouldBe((byte)1);
        bytes[5].ShouldBe((byte)0);
        bytes[6].ShouldBe((byte)0);
        bytes[7].ShouldBe((byte)0);
    }

    [Fact]
    public void Read_IndexOutOfRange_Throws()
    {
        using var hostile = new MemoryStream();
        using (var writer = new BinaryWriter(hostile, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("GEMH"));
            writer.Write(1u);
            writer.Write(1u);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write(1u);
            for (var i = 0; i < 14; i++)
                writer.Write(0f);
            writer.Write(5u);
            writer.Write(0f);
            writer.Write(0.5f);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
        }

        hostile.Position = 0;
        Should.Throw<InvalidDataException>(() => MeshReader.Read(hostile))
            .Message.ShouldContain("out of range");
    }

    [Fact]
    public void Read_SubmeshCountExceedsMax_Throws()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("GEMH"));
            writer.Write(1u);
            writer.Write(MeshReader.MaxSubmeshes + 1);
        }

        stream.Position = 0;
        Should.Throw<InvalidDataException>(() => MeshReader.Read(stream))
            .Message.ShouldContain("SUBMESH_COUNT");
    }
}
