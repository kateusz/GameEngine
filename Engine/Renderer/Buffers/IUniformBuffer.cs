namespace Engine.Renderer.Buffers;

public interface IUniformBuffer
{
    void SetData(CameraData cameraData, int dataSize, int offset = 0);
}