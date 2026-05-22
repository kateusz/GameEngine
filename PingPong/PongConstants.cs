using System.Numerics;

namespace PingPong;

internal static class PongConstants
{
    public const float GoalX = 11.0f;
    public const float BallSpeed = 8.0f;
    public const float PaddleMoveSpeed = 8.0f;
    public const int MaxScore = 10;
    public const float MaxVerticalInfluence = 0.9f;
    public const float MinHorizontalComponent = 0.35f;
    public const float SeparationEpsilon = 0.01f;
    public const float AiVerticalDeadzone = 0.1f;
    public const float ScoreBarMaxWidth = 3.0f;
    public const float ScoreBarHeight = 0.25f;

    public static readonly Vector3 InitialBallPosition = Vector3.Zero;
    public static readonly Vector3 PaddleScale = new(0.7f, 4.0f, 1.0f);
    public static readonly Vector2 PaddleColliderHalfExtents = new(0.35f, 2.0f);
    public static readonly Vector3 BallScale = new(0.5f, 0.5f, 1.0f);
    public static readonly Vector2 BallColliderHalfExtents = new(0.25f, 0.25f);
}
