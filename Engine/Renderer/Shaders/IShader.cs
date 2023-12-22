using OpenTK.Mathematics;

namespace Engine.Renderer.Shaders;

public interface IShader : IBindable
{
    void SetFloat3(string name, Vector3 data);
    void SetFloat4(string name, Vector4 data);
    void SetMat4(string name, Matrix4 data);
    void SetFloat(string name, float data);
    void SetInt(string name, int data);
}