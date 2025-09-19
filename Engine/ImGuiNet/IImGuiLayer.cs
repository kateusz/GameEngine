using Engine.Core;

namespace Engine.ImGuiNet;

public interface IImGuiLayer : ILayer
{
    void Begin(TimeSpan timeSpan);
    void End();
}