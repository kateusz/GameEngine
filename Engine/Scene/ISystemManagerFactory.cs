using ECS.Systems;

namespace Engine.Scene;

public interface ISystemManagerFactory
{
    ISystemManager Create();
}
