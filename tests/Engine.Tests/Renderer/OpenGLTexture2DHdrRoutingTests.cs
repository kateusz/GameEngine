using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.Tests.Renderer;

public class OpenGLTexture2DHdrRoutingTests
{
    [Fact]
    public void Create_MissingHdr_ShouldThrowFileNotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
            OpenGLTexture2D.Create(Path.Combine(AppContext.BaseDirectory, "nope.hdr")));
    }
}
