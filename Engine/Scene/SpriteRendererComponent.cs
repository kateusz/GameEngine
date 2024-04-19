using OpenTK.Mathematics;

namespace Engine.Scene;

public struct SpriteRendererComponent
{
    public Vector4 Color;

    public SpriteRendererComponent(Vector4 color)
    {
        Color = color;
    }
}