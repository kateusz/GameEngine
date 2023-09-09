using OpenTK.Mathematics;

namespace Engine.Renderer;

public interface IShader : IBindable
{
    // TODO: below methods are opengl specific - it should be refactored
    void UploadUniformFloat3(string name, Vector3 data);
    void UploadUniformFloat4(string name, Vector4 data);
    void UploadUniformMatrix4(string name, Matrix4 data);
    void SetFloat(string name, float data);
    void UploadUniformInt(string name, int data);
}