using Engine.Renderer.Textures;
using OpenTK.Mathematics;
using Renderer2D = Engine.Renderer.Renderer2D;

namespace _1.FlappyBirdClone;

public class Pillar
{
    public Vector3 TopPosition = new(0.0f, 10.0f, 0.0f);
    public Vector2 TopScale = new(15.0f, 20.0f);

    public Vector3 BottomPosition = new (10.0f, 10.0f, 0.0f);
    public Vector2 BottomScale = new (15.0f, 20.0f);
};

public class Level
{
    private Texture2D _triangleTexture;
    private List<Pillar> _pillars;

    public Level()
    {
        Player = new Player();
    }
    
    public Player Player { get; set; }
    
    public void Init()
    {
        _triangleTexture = TextureFactory.Create("assets/textures/Triangle.png");
        Player.LoadAssets();

        _pillars = new List<Pillar>();
        
        for (int i = 0; i < 5; i++)
            CreatePillar(i, i * 10.0f);
    }

    public bool IsGameOver()
    {
        return false;
    }

    public void OnUpdate(TimeSpan ts)
    {
        Player.OnUpdate(ts);
    }

    public void OnRender()
    {
        var color = new Vector4(1.0f, 0.2f, 0.5f, 1.0f);
        foreach (var pillar in _pillars)
        {
            
            Renderer2D.Instance.DrawRotatedQuad(new Vector2(-0.5f, 0.0f), new Vector2(0.8f, 0.8f),
                MathHelper.DegreesToRadians(180.0f), _triangleTexture, 1.0f, color);
            
            Renderer2D.Instance.DrawRotatedQuad(pillar.TopPosition, pillar.TopScale, MathHelper.DegreesToRadians(180.0f), _triangleTexture, 1.0f, color);
            Renderer2D.Instance.DrawRotatedQuad(pillar.BottomPosition, pillar.BottomScale, 0.0f, _triangleTexture, 1.0f, color);
        }
        
        Player.OnRender();
    }
    
    public void CreatePillar(int index, float offset)
    {
        var pillar = new Pillar();
        pillar.TopPosition.X = offset;
        pillar.BottomPosition.X = offset;
        pillar.TopPosition.Z = index * 0.1f - 0.5f;
        pillar.BottomPosition.Z = index * 0.1f - 0.5f + 0.05f;

        var random = new Random(5);
        float center = random.NextSingle() * 35.0f - 17.5f;
        float gap = 2.0f + random.NextSingle() * 5.0f;

        pillar.TopPosition.Y = 10.0f - ((10.0f - center) * 0.2f) + gap * 0.5f;
        pillar.BottomPosition.Y = -10.0f - ((-10.0f - center) * 0.2f) - gap * 0.5f;
        
        _pillars.Add(pillar);
    }
}