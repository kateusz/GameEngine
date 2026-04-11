using System.Numerics;
using DoomGame.Map;
using Engine.Core.Input;

namespace DoomGame.Player;

public class LocalPlayer
{
    private const float MoveSpeed = 3.5f;
    private const float RotSpeed = 2.2f;
    private const float CollisionRadius = 0.25f;

    public Vector2 Position { get; private set; } = new(3.5f, 3.5f);
    public Vector2 Direction { get; private set; } = new(1f, 0f);
    public Vector2 CameraPlane { get; private set; } = new(0f, 0.66f);
    public float Angle { get; private set; }
    public int Health { get; private set; } = 100;
    public int Ammo { get; private set; } = 50;
    public bool JustShot { get; private set; }
    public float ShootCooldown { get; private set; }

    private readonly HashSet<KeyCodes> _heldKeys = [];

    public void OnKeyPressed(KeyCodes key) => _heldKeys.Add(key);
    public void OnKeyReleased(KeyCodes key) => _heldKeys.Remove(key);

    public void Update(TimeSpan deltaTime, GameMap map)
    {
        var dt = (float)deltaTime.TotalSeconds;
        JustShot = false;

        if (ShootCooldown > 0f)
            ShootCooldown -= dt;

        Rotate(dt);
        Move(dt, map);
        TryShoot();
    }

    public void TakeDamage(int damage)
    {
        Health = System.Math.Max(0, Health - damage);
    }

    private void Rotate(float dt)
    {
        float rotDir = 0f;
        if (_heldKeys.Contains(KeyCodes.Left) || _heldKeys.Contains(KeyCodes.A)) rotDir -= 1f;
        if (_heldKeys.Contains(KeyCodes.Right) || _heldKeys.Contains(KeyCodes.D)) rotDir += 1f;

        if (rotDir == 0f) return;

        float rot = -rotDir * RotSpeed * dt;
        Angle += rotDir * RotSpeed * dt;

        float cos = MathF.Cos(rot);
        float sin = MathF.Sin(rot);

        var oldDir = Direction;
        Direction = new Vector2(
            oldDir.X * cos - oldDir.Y * sin,
            oldDir.X * sin + oldDir.Y * cos);

        var oldPlane = CameraPlane;
        CameraPlane = new Vector2(
            oldPlane.X * cos - oldPlane.Y * sin,
            oldPlane.X * sin + oldPlane.Y * cos);
    }

    private void Move(float dt, GameMap map)
    {
        float moveDir = 0f;
        if (_heldKeys.Contains(KeyCodes.Up) || _heldKeys.Contains(KeyCodes.W)) moveDir += 1f;
        if (_heldKeys.Contains(KeyCodes.Down) || _heldKeys.Contains(KeyCodes.S)) moveDir -= 1f;

        if (moveDir != 0f)
        {
            float dx = Direction.X * moveDir * MoveSpeed * dt;
            float dy = Direction.Y * moveDir * MoveSpeed * dt;
            TryMoveX(Position.X + dx, map);
            TryMoveY(Position.Y + dy, map);
        }

        float strafeDir = 0f;
        if (_heldKeys.Contains(KeyCodes.Q)) strafeDir -= 1f;
        if (_heldKeys.Contains(KeyCodes.E)) strafeDir += 1f;

        if (strafeDir != 0f)
        {
            float dx = -Direction.Y * strafeDir * MoveSpeed * dt;
            float dy = Direction.X * strafeDir * MoveSpeed * dt;
            TryMoveX(Position.X + dx, map);
            TryMoveY(Position.Y + dy, map);
        }
    }

    private void TryMoveX(float newX, GameMap map)
    {
        float r = CollisionRadius;
        if (!map.IsWall((int)(newX + r), (int)Position.Y) &&
            !map.IsWall((int)(newX - r), (int)Position.Y))
        {
            Position = new Vector2(newX, Position.Y);
        }
    }

    private void TryMoveY(float newY, GameMap map)
    {
        float r = CollisionRadius;
        if (!map.IsWall((int)Position.X, (int)(newY + r)) &&
            !map.IsWall((int)Position.X, (int)(newY - r)))
        {
            Position = new Vector2(Position.X, newY);
        }
    }

    private void TryShoot()
    {
        if (_heldKeys.Contains(KeyCodes.Space) && ShootCooldown <= 0f && Ammo > 0)
        {
            JustShot = true;
            ShootCooldown = 0.4f;
            Ammo--;
        }
    }
}
