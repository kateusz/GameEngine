namespace Engine.Core;

public interface IFrameCompositor
{
    void BeginFrame(TimeSpan elapsed);
    void EndFrame();
}
