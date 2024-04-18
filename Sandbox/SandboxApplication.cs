using Engine.Core;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication()
    {
        PushLayer(new Sandbox2DLayer("Sandbox 2D Layer"));
    }
}