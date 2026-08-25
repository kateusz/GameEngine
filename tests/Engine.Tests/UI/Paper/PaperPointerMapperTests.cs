using System.Numerics;
using Engine.Core.Window;
using Engine.UI.Paper;
using Shouldly;

namespace Engine.Tests.UI.Paper;

public class PaperPointerMapperTests
{
  private sealed class StubSurface(Vector2 origin, Vector2 size) : IPointerSurface
  {
    public Vector2 Origin { get; } = origin;
    public Vector2 Size { get; } = size;

    public void Set(Vector2 origin, Vector2 size) { }

    public bool Contains(Vector2 windowPosition) =>
      windowPosition.X >= Origin.X
      && windowPosition.Y >= Origin.Y
      && windowPosition.X < Origin.X + Size.X
      && windowPosition.Y < Origin.Y + Size.Y;
  }

  [Fact]
  public void Map_InsideViewport_ScalesByContentScale()
  {
    var surface = new StubSurface(new Vector2(100, 50), new Vector2(200, 100));
    var mapped = PaperPointerMapper.Map(new Vector2(150, 100), surface, 2f);

    mapped.IsInside.ShouldBeTrue();
    mapped.X.ShouldBe(100f);
    mapped.Y.ShouldBe(100f);
  }

  [Fact]
  public void Map_OutsideViewport_ReturnsOutside()
  {
    var surface = new StubSurface(new Vector2(100, 50), new Vector2(200, 100));
    var mapped = PaperPointerMapper.Map(new Vector2(50, 100), surface, 2f);

    mapped.IsInside.ShouldBeFalse();
  }
}
