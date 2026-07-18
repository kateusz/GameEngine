using System.Numerics;

namespace Engine.Core.Window;

public sealed class PointerSurface : IPointerSurface
{
    public Vector2 Origin { get; private set; }
    public Vector2 Size { get; private set; }

    public void Set(Vector2 origin, Vector2 size)
    {
        Origin = origin;
        Size = size;
    }

    public bool Contains(Vector2 windowPosition)
    {
        if (Size.X <= 0f || Size.Y <= 0f)
            return false;

        return windowPosition.X >= Origin.X
               && windowPosition.Y >= Origin.Y
               && windowPosition.X < Origin.X + Size.X
               && windowPosition.Y < Origin.Y + Size.Y;
    }
}
