using Engine.Core.Input;
using Engine.Renderer.Textures;
using OpenTK.Mathematics;
using Renderer2D = Engine.Renderer.Renderer2D;

namespace _1.FlappyBirdClone;

public class Player
{
    private Texture2D _shipTexture = null!;
    private TimeSpan _time;

    public Vector2 Position { get; private set; } = new(-1.0f, 0.0f);

    public void LoadAssets()
    {
        _shipTexture = TextureFactory.Create("assets/textures/Ship.png");
    }

    public void OnUpdate(TimeSpan ts)
    {
        _time += ts;

        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Space))
        {
            var velocity = new Vector2(5, 5);
            Position += velocity * (float)ts.TotalSeconds;
        }
    }

    public void OnRender()
    {
        Renderer2D.Instance.DrawRotatedQuad(new Vector3(Position.X, Position.Y, 0.5f), new Vector2(1f, 1f), 270f, _shipTexture, 1.0f);
    }
}