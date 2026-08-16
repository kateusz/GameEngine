using ECS;

namespace FlappyBird.assets.scripts;

[SerializableComponent]
public class FlappyBirdGameComponent : IGameComponent
{
    public const int Ready = 0;
    public const int Playing = 1;
    public const int Dead = 2;

    // Runtime state (persisted in scene JSON via System.Text.Json, so arrays round-trip).
    public int Phase { get; set; } = Ready;
    public float BirdY { get; set; } = 0.3f;
    public float BirdVelocity { get; set; }
    public float BirdX { get; set; } = -1.2f;
    public float[] PipeX { get; set; } = new float[3];
    public float[] PipeGapY { get; set; } = new float[3];
    public bool[] PipeScored { get; set; } = new bool[3];
    public int Score { get; set; }
    public float GroundScroll { get; set; }
    public float FlapAnimT { get; set; }
    public float BobT { get; set; }

    // Tunables (shown in the editor inspector).
    public float Gravity { get; set; } = 14f;
    public float FlapVelocity { get; set; } = 2.6f;
    public float PipeSpeed { get; set; } = 1.35f;
    public float PipeGap { get; set; } = 1.6f;
    public float PipeSpacing { get; set; } = 3.2f;
    public float GroundTopY { get; set; } = -2.5f;
    public float CeilingY { get; set; } = 2.4f;
    public float FirstPipeX { get; set; } = 2.5f;

    public IComponent Clone() => new FlappyBirdGameComponent
    {
        Phase = Phase,
        BirdY = BirdY,
        BirdVelocity = BirdVelocity,
        BirdX = BirdX,
        PipeX = (float[])PipeX.Clone(),
        PipeGapY = (float[])PipeGapY.Clone(),
        PipeScored = (bool[])PipeScored.Clone(),
        Score = Score,
        GroundScroll = GroundScroll,
        FlapAnimT = FlapAnimT,
        BobT = BobT,
        Gravity = Gravity,
        FlapVelocity = FlapVelocity,
        PipeSpeed = PipeSpeed,
        PipeGap = PipeGap,
        PipeSpacing = PipeSpacing,
        GroundTopY = GroundTopY,
        CeilingY = CeilingY,
        FirstPipeX = FirstPipeX
    };
}
