using Engine.Platform.OpenGL;
using Engine.Renderer.Buffers.FrameBuffer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class FrameBufferFactorySceneTargetTests
{
    [Fact]
    public void SceneTarget_UsesFloatColor_EntityId_AndDepth()
    {
        var spec = FrameBufferFactory.SceneTarget(64, 48);
        var formats = spec.AttachmentsSpec.Attachments.Select(attachment => attachment.TextureFormat).ToArray();

        formats.ShouldBe([
            FrameBufferTextureFormat.RGBA16F,
            FrameBufferTextureFormat.RED_INTEGER,
            FrameBufferTextureFormat.Depth
        ]);
        spec.AttachmentsSpec.Attachments[0].Filter.ShouldBe(FrameBufferTextureFilter.Linear);
        spec.AttachmentsSpec.Attachments[0].Wrap.ShouldBe(FrameBufferTextureWrap.ClampToEdge);
    }
}
