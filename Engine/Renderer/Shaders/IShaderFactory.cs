namespace Engine.Renderer.Shaders;

/// <summary>Factory owns cached shaders; callers must not dispose them.</summary>
public interface IShaderFactory : IDisposable
{
    IShader Create(ShaderId shader);
}
