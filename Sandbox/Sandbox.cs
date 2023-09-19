using Engine;

namespace Sandbox;

public class Sandbox : Application
{
    public Sandbox()
    {
        PushLayer(new Sandbox2D("2D Layer"));
    }
}