using System.Numerics;
using System.Text;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
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
            NormalTexturePath = "models/cube/normal.png",
            EmissiveTexturePath = "models/cube/emissive.png",
            BaseColorFactor = new Vector4(0.9f, 0.8f, 0.7f, 0.6f),
            EmissiveFactor = new Vector3(1f, 0.5f, 0.25f),
            AlphaMode = MaterialAlphaMode.Mask,
            AlphaCutoff = 0.42f,
            DoubleSided = true
        };

        var model = new Model([new ModelSubmesh(mesh, material)]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        loaded.Submeshes.Count.ShouldBe(1);
        var sub = loaded.Submeshes[0];
        sub.Mesh.Name.ShouldBe("Cube");
        Mesh.Vertex.GetSize().ShouldBe(88);
        sub.Mesh.Vertices.Count.ShouldBe(3);
        sub.Mesh.Vertices[0].ShouldBe(vertex);
        sub.Mesh.Vertices[1].Position.ShouldBe(new Vector3(4, 5, 6));
        sub.Mesh.Indices.ShouldBe([0u, 1u, 2u]);
        sub.Material.Metallic.ShouldBe(0.75f);
        sub.Material.Roughness.ShouldBe(0.35f);
        sub.Material.AlbedoTexturePath.ShouldBe("models/cube/albedo.png");
        sub.Material.MetallicRoughnessTexturePath.ShouldBe("models/cube/mr.png");
        sub.Material.NormalTexturePath.ShouldBe("models/cube/normal.png");
        sub.Material.EmissiveTexturePath.ShouldBe("models/cube/emissive.png");
        sub.Material.BaseColorFactor.ShouldBe(new Vector4(0.9f, 0.8f, 0.7f, 0.6f));
        sub.Material.EmissiveFactor.ShouldBe(new Vector3(1f, 0.5f, 0.25f));
        sub.Material.AlphaMode.ShouldBe(MaterialAlphaMode.Mask);
        sub.Material.AlphaCutoff.ShouldBe(0.42f);
        sub.Material.DoubleSided.ShouldBeTrue();
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
            writer.Write("KULA"u8.ToArray());
            writer.Write(99u);
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
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("KULA");
        bytes[4].ShouldBe((byte)3);
        bytes[5].ShouldBe((byte)0);
        bytes[6].ShouldBe((byte)0);
        bytes[7].ShouldBe((byte)0);
    }

    [Fact]
    public void Read_Version1_Mesh_UsesMaterialDefaultsForNewFields()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("KULA"u8.ToArray());
            writer.Write(1u);
            writer.Write(1u);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write(1u);
            for (var i = 0; i < 14; i++)
                writer.Write(0f);
            writer.Write(0u);
            writer.Write(0.25f);
            writer.Write(0.5f);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
        }

        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        var mat = loaded.Submeshes[0].Material;
        mat.BaseColorFactor.ShouldBe(Vector4.One);
        mat.EmissiveFactor.ShouldBe(Vector3.Zero);
        mat.EmissiveTexturePath.ShouldBeNull();
        mat.AlphaMode.ShouldBe(MaterialAlphaMode.Opaque);
        mat.AlphaCutoff.ShouldBe(0.5f);
        mat.DoubleSided.ShouldBeFalse();
    }

    [Fact]
    public void Read_IndexOutOfRange_Throws()
    {
        using var hostile = new MemoryStream();
        using (var writer = new BinaryWriter(hostile, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("KULA"u8.ToArray());
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
            writer.Write("KULA"u8.ToArray());
            writer.Write(1u);
            writer.Write(MeshReader.MaxSubmeshes + 1);
        }

        stream.Position = 0;
        Should.Throw<InvalidDataException>(() => MeshReader.Read(stream))
            .Message.ShouldContain("SUBMESH_COUNT");
    }

    [Fact]
    public void RoundTrip_Skinned_PreservesWeightsSkeletonAndClip()
    {
        var vertex = new Mesh.Vertex(
            new Vector3(1, 0, 0),
            Vector3.UnitY,
            Vector2.Zero,
            Vector3.UnitX,
            Vector3.UnitZ,
            0, 1, -1, -1,
            new Vector4(0.75f, 0.25f, 0, 0));

        var mesh = new Mesh("Skinned");
        mesh.Vertices.Add(vertex);
        mesh.Indices.Add(0);

        var inverseBind = Matrix4x4.CreateTranslation(0, 1, 0);
        var model = new Model(
            [new ModelSubmesh(mesh, new MeshMaterial { Metallic = 0.2f })],
            [
                new SkeletonBone("root", -1, Matrix4x4.Identity),
                new SkeletonBone("child", 0, inverseBind)
            ],
            [
                new AnimationClip("walk", 1f,
                [
                    new BoneChannel(
                        0,
                        [new VectorKey(0f, Vector3.Zero), new VectorKey(1f, Vector3.UnitX)],
                        [new RotationKey(0f, Quaternion.Identity)],
                        [new VectorKey(0f, Vector3.One)])
                ])
            ]);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, model);
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        loaded.HasSkeleton.ShouldBeTrue();
        loaded.Bones.Count.ShouldBe(2);
        loaded.Bones[0].Name.ShouldBe("root");
        loaded.Bones[0].ParentIndex.ShouldBe(-1);
        loaded.Bones[1].Name.ShouldBe("child");
        loaded.Bones[1].ParentIndex.ShouldBe(0);
        loaded.Bones[1].InverseBind.Translation.ShouldBe(new Vector3(0, 1, 0));

        loaded.Clips.Count.ShouldBe(1);
        loaded.Clips[0].Name.ShouldBe("walk");
        loaded.Clips[0].Duration.ShouldBe(1f);
        loaded.Clips[0].Channels.Count.ShouldBe(1);
        loaded.Clips[0].Channels[0].BoneIndex.ShouldBe(0);
        loaded.Clips[0].Channels[0].Positions[1].Value.ShouldBe(Vector3.UnitX);

        var loadedVertex = loaded.Submeshes[0].Mesh.Vertices[0];
        loadedVertex.BoneId0.ShouldBe(0f);
        loadedVertex.BoneId1.ShouldBe(1f);
        loadedVertex.BoneId2.ShouldBe(-1f);
        loadedVertex.Weights.X.ShouldBe(0.75f);
        loadedVertex.Weights.Y.ShouldBe(0.25f);
    }

    [Fact]
    public void RoundTrip_Static_HasNoSkeletonAndUnskinnedVertices()
    {
        var mesh = new Mesh("Static");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.Add(0);

        using var stream = new MemoryStream();
        MeshWriter.Write(stream, new Model([new ModelSubmesh(mesh, new MeshMaterial())]));
        stream.Position = 0;
        var loaded = MeshReader.Read(stream);

        loaded.HasSkeleton.ShouldBeFalse();
        loaded.Bones.Count.ShouldBe(0);
        loaded.Clips.Count.ShouldBe(0);
        loaded.Submeshes[0].Mesh.Vertices[0].BoneId0.ShouldBe(-1f);
        loaded.Submeshes[0].Mesh.Vertices[0].Weights.ShouldBe(Vector4.Zero);
    }
}
