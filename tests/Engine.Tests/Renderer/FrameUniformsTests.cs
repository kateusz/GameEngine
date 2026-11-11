using System.Runtime.CompilerServices;
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
}
