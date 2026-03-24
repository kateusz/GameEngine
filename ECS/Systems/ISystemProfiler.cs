namespace ECS.Systems;

public interface ISystemProfiler
{
    IDisposable BeginSystemScope(string systemName);
}
