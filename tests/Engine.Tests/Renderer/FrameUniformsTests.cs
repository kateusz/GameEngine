using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class FrameUniformsTests
{
    [Fact]
    public void SizeOf_Is688()
    {
        Unsafe.SizeOf<FrameUniforms>().ShouldBe(FrameUniforms.ByteSize);
        FrameUniforms.ByteSize.ShouldBe(688);
    }

    [Fact]
    public void FieldOffsets_MatchStd140Tail()
    {
        Marshal.OffsetOf<FrameUniforms>(nameof(FrameUniforms.PointLightCount)).ToInt32().ShouldBe(640);
        Marshal.OffsetOf<FrameUniforms>(nameof(FrameUniforms.PointShadowMask)).ToInt32().ShouldBe(672);
        Marshal.OffsetOf<FrameUniforms>(nameof(FrameUniforms.Ibl)).ToInt32().ShouldBe(676);
    }
}
