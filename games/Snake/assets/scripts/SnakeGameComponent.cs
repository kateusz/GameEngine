using ECS;

namespace Snake.assets.scripts;

[SerializableComponent]
public class SnakeGameComponent : IGameComponent
{
    public const int Up = 0;
    public const int Right = 1;
    public const int Down = 2;
    public const int Left = 3;

    public int GridWidth { get; set; } = 16;
    public int GridHeight { get; set; } = 12;
    public int[] Body { get; set; } = [];
    public int FoodIndex { get; set; }
    public int Direction { get; set; } = Right;
    public int PendingDirection { get; set; } = Right;
    public int Score { get; set; }
    public bool GameOver { get; set; }
    public double TickAccumulator { get; set; }
    public double TickInterval { get; set; } = 0.12;

    public int CellCount => GridWidth * GridHeight;

    public void Reset()
    {
        var center = GridHeight / 2 * GridWidth + GridWidth / 2;
        Body = [center + 2, center + 1, center];
        Direction = Right;
        PendingDirection = Right;
        Score = 0;
        GameOver = false;
        TickAccumulator = 0;
        FoodIndex = -1;
    }

    public IComponent Clone() => new SnakeGameComponent
    {
        GridWidth = GridWidth,
        GridHeight = GridHeight,
        Body = (int[])Body.Clone(),
        FoodIndex = FoodIndex,
        Direction = Direction,
        PendingDirection = PendingDirection,
        Score = Score,
        GameOver = GameOver,
        TickAccumulator = TickAccumulator,
        TickInterval = TickInterval
    };
}
