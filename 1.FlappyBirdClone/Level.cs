namespace _1.FlappyBirdClone;

public class Level
{
    public Level()
    {
        Player = new Player();
    }

    public Player Player { get; set; }

    public void Init()
    {
        Player.LoadAssets();
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
        Player.OnRender();
    }
}