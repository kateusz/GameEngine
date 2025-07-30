using Engine.Core;

namespace Runtime;

public class RuntimeApplication : Application
{
    public RuntimeApplication() : base(true)
    {
        PushLayer(new Runtime2DLayer("Runtime Layer"));
    }
}