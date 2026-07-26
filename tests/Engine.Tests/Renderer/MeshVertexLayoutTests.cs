using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshVertexLayoutTests
{
    [Fact]
    public void CreateVertexLayout_Stride_MatchesPackedVertexSize()
    {
        // Entity ID is a per-draw uniform in 3D shaders; Mesh.Vertex has no EntityId field.
        // If layout stride exceeds packed size, GL reads garbage positions (vertex explosion).
        Mesh.CreateVertexLayout().Stride.ShouldBe(Mesh.Vertex.GetSize());
    }

    [Fact]
    public void Vertex_GetSize_Is88Bytes_WithBoneAttrs()
    {
        // 14 floats (position/normal/uv/tangent/bitangent) + Float4 + Float4 = 56 + 16 + 16
        Mesh.Vertex.GetSize().ShouldBe(88);
        Mesh.CreateVertexLayout().Stride.ShouldBe(88);
    }

    [Fact]
    public void Vertex_FieldOffsets_MatchBufferLayout()
    {
        Unsafe.SizeOf<Mesh.Vertex>().ShouldBe(88);
        Unsafe.SizeOf<VertexLayoutMirror>().ShouldBe(88);

        var layout = Mesh.CreateVertexLayout();
        layout.Elements.First(e => e.Name == "a_BoneIndex").Offset
            .ShouldBe(Mesh.BoneIndexByteOffset);
        layout.Elements.First(e => e.Name == "a_BoneWeight").Offset
            .ShouldBe((int)Marshal.OffsetOf<VertexLayoutMirror>(nameof(VertexLayoutMirror.BoneWeight)));
    }

    [Fact]
    public void Vertex_BoneIndex_IsFloatAtLayoutOffset()
    {
        var v = new Mesh.Vertex(
            Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ,
            new Vector4(13, 7, 17, 0),
            new Vector4(0.858f, 0.139f, 0.003f, 0f));

        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        MemoryMarshal.Read<float>(bytes.Slice(Mesh.BoneIndexByteOffset, 4)).ShouldBe(13f);
        MemoryMarshal.Read<float>(bytes.Slice(Mesh.BoneIndexByteOffset + 4, 4)).ShouldBe(7f);
        MemoryMarshal.Read<float>(bytes.Slice(Mesh.BoneIndexByteOffset + 8, 4)).ShouldBe(17f);
        MemoryMarshal.Read<float>(bytes.Slice(Mesh.BoneIndexByteOffset + 12, 4)).ShouldBe(0f);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VertexLayoutMirror
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector4 BoneIndex;
        public Vector4 BoneWeight;
    }
}
