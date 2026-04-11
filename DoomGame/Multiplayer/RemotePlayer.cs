using System.Numerics;

namespace DoomGame.Multiplayer;

public class RemotePlayer
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public float Angle { get; set; }
    public int Health { get; set; }
    public int Ammo { get; set; }
    public double LastUpdateTime { get; set; }

    public Vector2 Direction =>
        new(MathF.Cos(Angle), MathF.Sin(Angle));
}
