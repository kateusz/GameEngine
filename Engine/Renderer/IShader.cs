using OpenTK.Mathematics;

namespace Engine.Renderer;

public interface IShader : IBindable
{
    void SetVector3(string name, Vector3 data);
    void SetMatrix4(string name, Matrix4 data);
    void SetFloat(string name, float data);
    void SetInt(string name, int data);
}