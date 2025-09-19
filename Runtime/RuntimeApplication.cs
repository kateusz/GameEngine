using Engine.Core;

namespace Runtime;

public class RuntimeApplication : Application
{
    public RuntimeApplication() : base(null!,null!)
    {
        PushLayer(new Runtime2DLayer());
    }
}