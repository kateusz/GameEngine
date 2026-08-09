using System.Numerics;

namespace Engine.Renderer.Shaders;

public interface IShader : IBindable
{
    void SetFloat3(string name, Vector3 data);
    void SetFloat4(string name, Vector4 data);
    void SetMat4(string name, Matrix4x4 data);
    void SetMat4Array(string name, Matrix4x4[] values, uint count);
    void SetFloat(string name, float data);
    void SetInt(string name, int data);
    void SetIntArray(string name, int[] values, uint count);
    void UploadUniformIntArray(string name, int[] values, uint count);

    /// <summary>Optional backend diagnostic; no-op unless the implementation supports it.</summary>
    void LogUniformInventory(string label) { }
}