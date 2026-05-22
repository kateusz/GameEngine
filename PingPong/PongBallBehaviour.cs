using System.Numerics;
using ECS;
using SceneComponents;
using Scripting;

namespace PingPong;

public class PongBallBehaviour : ScriptableEntity
{
    public PongBallBehaviour(IComponentAccessor componentAccessor) : base(componentAccessor)
    {
    }

    public override void OnCollisionBegin(Entity other)
    {
        if (other.Name == PongEntityNames.LeftGoalWall)
        {
            PongScoreHandler.AiScored = true;
            ResetBall();
        }
        else if (other.Name == PongEntityNames.RightGoalWall)
        {
            PongScoreHandler.PlayerScored = true;
            ResetBall();
        }
    }

    private void ResetBall()
    {
        if (!HasComponent<TransformComponent>() || !HasComponent<BallComponent>())
            return;

        GetComponent<TransformComponent>().Translation = Vector3.Zero;
        GetComponent<BallComponent>().ReadyToLaunch = true;
        GetComponent<BallComponent>().LaunchDirection = PongScoreHandler.PlayerScored ? -1 : 1;
    }
}
