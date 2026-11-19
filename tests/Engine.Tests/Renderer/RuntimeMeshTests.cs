using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Models.RuntimeMesh;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class RuntimeMeshTests
{
    [Fact]
    public void Serialize_IsDeterministic_ForSameCpuModel()
    {
        var model = SampleModel();
        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);

        var first = RuntimeMeshWriter.Serialize(model, stamp);
        var second = RuntimeMeshWriter.Serialize(model, stamp);

        first.ShouldBe(second);
    }

    [Fact]
    public void RoundTrip_PreservesGeometryAndGraph()
    {
        var model = SampleModel();
        var stamp = new RuntimeMeshStamp(42, Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(),
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);
        var bytes = RuntimeMeshWriter.Serialize(model, stamp);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read.ShouldNotBeNull();
        read!.Submeshes.Count.ShouldBe(1);
        read.Submeshes[0].Vertices.Count.ShouldBe(3);
        read.Submeshes[0].Indices.ShouldBe([0u, 1u, 2u]);
        read.Nodes.Count.ShouldBe(2);
        read.Nodes[0].ChildIndices.ShouldBe([1]);
        read.Nodes[1].MeshIndices.ShouldBe([0]);
    }

    [Fact]
    public void RoundTrip_KeepsDistinctVertices_WhenOnlyUvDiffers()
    {
        var model = SampleModel();
        var adjusted = model.Submeshes[0].Vertices[1];
        adjusted.TexCoord = new Vector2(1f, 0f);
        model.Submeshes[0].Vertices[1] = adjusted;
        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);
        var bytes = RuntimeMeshWriter.Serialize(model, stamp);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].Vertices[0].TexCoord.ShouldBe(Vector2.Zero);
        read.Submeshes[0].Vertices[1].TexCoord.ShouldBe(new Vector2(1f, 0f));
    }

    [Fact]
    public void RoundTrip_PreservesAlphaCutout()
    {
        var model = SampleModel();
        model.Submeshes[0].AlphaCutout = true;
        model.Submeshes[0].DoubleSided = true;
        model.Submeshes[0].AlphaCutoff = 0.35f;
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].AlphaCutout.ShouldBeTrue();
        read.Submeshes[0].DoubleSided.ShouldBeTrue();
        read.Submeshes[0].AlphaCutoff.ShouldBe(0.35f, 0.001f);
    }

    [Fact]
    public void RoundTrip_PreservesEmissive()
    {
        var model = SampleModel();
        model.Submeshes[0].Emissive = new Vector3(1f, 2f, 3f);
        model.Submeshes[0].EmissivePath = "textures/emissive.png";
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].Emissive.ShouldBe(new Vector3(1f, 2f, 3f));
        read.Submeshes[0].EmissivePath.ShouldBe("textures/emissive.png");
    }

    [Fact]
    public void TryRead_RejectsTruncatedFile()
    {
        var model = SampleModel();
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));
        RuntimeMeshReader.TryRead(bytes.AsSpan(0, 64), out _).ShouldBeFalse();
    }

    [Fact]
    public void TryRead_RejectsBadIndex()
    {
        var model = SampleModel();
        model.Submeshes[0].Indices[2] = 99;
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));
        RuntimeMeshReader.TryRead(bytes, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryRead_Accepts_MaxUshortIndex()
    {
        var vertices = new List<Mesh.Vertex>(65536);
        for (var i = 0; i < 65536; i++)
            vertices.Add(new Mesh.Vertex { Position = new Vector3(i, 0f, 0f) });

        var submesh = new SourceSubmesh
        {
            Name = "big",
            BoundsMin = new Vector3(0f, 0f, 0f),
            BoundsMax = new Vector3(65535f, 0f, 0f)
        };
        submesh.Vertices.AddRange(vertices);
        submesh.Indices.AddRange([0u, 1u, 65535u]);

        var model = new SourceModel(
            [submesh],
            [new SourceNode { Name = "root" }],
            []);
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));

        RuntimeMeshReader.TryRead(bytes, out _).ShouldBeTrue();
    }

    [Fact]
    public void TryRead_Version1_LoadsBaseIndicesAndNoExtraLevel()
    {
        var model = SampleModel();
        model.Submeshes[0].LodIndices.Add([0u, 1u, 2u]);
        model.Submeshes[0].LodErrors.Add(0.5f);
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags), version: 1);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].LodIndices.ShouldBeEmpty();
        var mesh = ToMesh(read.Submeshes[0]);
        mesh.Indices.ShouldBe([0u, 1u, 2u]);
        mesh.LodCount.ShouldBe(1);
    }

    [Fact]
    public void TryRead_Version2WithTruncatedLevelBlock_KeepsBaseIndices()
    {
        var model = SampleModel();
        model.Submeshes[0].LodIndices.Add([0u, 1u, 2u]);
        model.Submeshes[0].LodErrors.Add(0.137f);
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));

        RuntimeMeshReader.TryRead(bytes, out var roundTrip).ShouldBeTrue();
        roundTrip!.Submeshes[0].LodIndices.Count.ShouldBe(1);
        roundTrip.Submeshes[0].LodErrors[0].ShouldBe(0.137f, 0.0001f);

        var error = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(error, BitConverter.SingleToInt32Bits(0.137f));
        var offset = bytes.AsSpan().IndexOf(error);
        offset.ShouldBeGreaterThanOrEqualTo(0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), 1_000_000);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].Indices.ShouldBe([0u, 1u, 2u]);
        ToMesh(read.Submeshes[0]).LodCount.ShouldBe(1);
    }

    [Fact]
    public void TryRead_OtherVersion_FailsTheFile()
    {
        var bytes = RuntimeMeshWriter.Serialize(SampleModel(), new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(RuntimeMeshFormat.Magic.Length), 3);

        RuntimeMeshReader.TryRead(bytes, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryWrite_ConcurrentSamePath_LeavesReadableMesh()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mesh-race-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "x.mesh");
        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion,
            RuntimeMeshFormat.AssimpPostProcessFlags);
        var start = new ManualResetEventSlim(false);
        Exception? e1 = null;
        Exception? e2 = null;
        var ok1 = false;
        var ok2 = false;

        var t1 = new Thread(() =>
        {
            start.Wait();
            try { ok1 = RuntimeMeshWriter.TryWrite(path, SampleModel(), stamp); }
            catch (Exception ex) { e1 = ex; }
        });
        var t2 = new Thread(() =>
        {
            start.Wait();
            try { ok2 = RuntimeMeshWriter.TryWrite(path, SampleModel(), stamp); }
            catch (Exception ex) { e2 = ex; }
        });

        try
        {
            t1.Start();
            t2.Start();
            start.Set();
            t1.Join();
            t2.Join();

            e1.ShouldBeNull();
            e2.ShouldBeNull();
            // Windows may fail one of two concurrent Move-to-same-path; content must still be a full write.
            (ok1 || ok2).ShouldBeTrue();
            File.Exists(path).ShouldBeTrue();
            RuntimeMeshReader.TryRead(File.ReadAllBytes(path), out var read).ShouldBeTrue();
            read.ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void TryEnsureUpToDate_MatchingSiblingStamp_SkipsImport()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mesh-fresh-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            var source = Path.Combine(dir, "model.glb");
            File.WriteAllBytes(source, new byte[64]);
            var sibling = RuntimeMeshPaths.SiblingPath(source);
            var stamp = new RuntimeMeshStamp(64, new byte[32], RuntimeMeshFormat.ImporterVersion,
                RuntimeMeshFormat.AssimpPostProcessFlags);
            RuntimeMeshWriter.TryWrite(sibling, SampleModel(), stamp).ShouldBeTrue();
            File.SetLastWriteTimeUtc(sibling, File.GetLastWriteTimeUtc(source).AddSeconds(1));

            // Source bytes are not a real GLB: reaching Assimp would fail, so success proves the fast path.
            RuntimeMeshBuilder.TryEnsureUpToDate(source).ShouldBeTrue();

            File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddMinutes(1));
            RuntimeMeshBuilder.TryEnsureUpToDate(source).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static Mesh ToMesh(SourceSubmesh submesh)
    {
        var mesh = new Mesh(submesh.Name);
        mesh.Indices.AddRange(submesh.Indices);
        foreach (var level in submesh.LodIndices)
            mesh.LodIndices.Add(level);
        mesh.LodErrors.AddRange(submesh.LodErrors);
        return mesh;
    }

    private static SourceModel SampleModel()
    {
        var submesh = new SourceSubmesh
        {
            Name = "tri",
            BoundsMin = Vector3.Zero,
            BoundsMax = new Vector3(1f, 1f, 0f),
            Metallic = 0.25f,
            Roughness = 0.5f,
            BaseColorFactor = Vector3.One
        };
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.Zero });
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.UnitX });
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.UnitY });
        submesh.Indices.AddRange([0u, 1u, 2u]);

        return new SourceModel(
            [submesh],
            [
                new SourceNode { Name = "root", ChildIndices = { 1 } },
                new SourceNode { Name = "mesh", MeshIndices = { 0 } }
            ],
            []);
    }
}
