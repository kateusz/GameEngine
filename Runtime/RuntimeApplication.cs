using Engine.Core;

namespace Runtime;

public class RuntimeApplication : Application
{
    public RuntimeApplication() : base(null!,true)
    {
        PushLayer(new Runtime2DLayer("Runtime Layer"));
    }
}