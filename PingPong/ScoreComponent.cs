using ECS;

namespace PingPong;

public sealed class ScoreComponent : IGameComponent
{
    public int PlayerScore { get; set; }
    public int AiScore { get; set; }
    public int MaxScore { get; set; } = 10;
    public bool IsGameOver { get; set; }

    public IComponent Clone()
    {
        return new ScoreComponent
        {
            PlayerScore = PlayerScore,
            AiScore = AiScore,
            MaxScore = MaxScore,
            IsGameOver = IsGameOver
        };
    }
}
