# Code Examples

## Player Movement with Input

```csharp
public class PlayerController : ScriptableEntity
{
    public float MoveSpeed = 5.0f;
    public float JumpForce = 10.0f;

    private Vector3 _velocity = Vector3.Zero;
    private TransformComponent? _transform;

    public override void OnCreate()
    {
        _transform = GetComponent<TransformComponent>();
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;

        if (_velocity != Vector3.Zero)
        {
            _transform!.Translation += _velocity * MoveSpeed * deltaTime;
        }
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _velocity += Vector3.UnitY; break;
            case KeyCodes.S: _velocity -= Vector3.UnitY; break;
            case KeyCodes.A: _velocity -= Vector3.UnitX; break;
            case KeyCodes.D: _velocity += Vector3.UnitX; break;
            case KeyCodes.Space:
                if (HasComponent<RigidBody2DComponent>())
                {
                    var rb = GetComponent<RigidBody2DComponent>();
                    rb.ApplyLinearImpulse(new Vector2(0, JumpForce));
                }
                break;
        }
    }

    public override void OnKeyReleased(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _velocity -= Vector3.UnitY; break;
            case KeyCodes.S: _velocity += Vector3.UnitY; break;
            case KeyCodes.A: _velocity += Vector3.UnitX; break;
            case KeyCodes.D: _velocity -= Vector3.UnitX; break;
        }
    }
}
```

## Camera Follow Script

```csharp
public class CameraFollowTarget : ScriptableEntity
{
    public string TargetEntityName = "Player";
    public Vector3 Offset = new Vector3(0, 0, 10);
    public float SmoothSpeed = 5.0f;

    private Entity? _target;
    private TransformComponent? _cameraTransform;

    public override void OnCreate()
    {
        _cameraTransform = GetComponent<TransformComponent>();
        _target = FindEntity(TargetEntityName);
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (_target == null || !_target.Value.HasComponent<TransformComponent>())
            return;

        var targetTransform = _target.Value.GetComponent<TransformComponent>();
        var desiredPosition = targetTransform.Translation + Offset;

        float deltaTime = (float)ts.TotalSeconds;
        _cameraTransform!.Translation = Vector3.Lerp(
            _cameraTransform.Translation,
            desiredPosition,
            SmoothSpeed * deltaTime
        );
    }
}
```

## Spawning Projectiles

```csharp
public class ProjectileSpawner : ScriptableEntity
{
    public float ProjectileSpeed = 15.0f;
    public float FireRate = 0.5f; // Seconds between shots

    private float _timeSinceLastShot = 0.0f;

    public override void OnUpdate(TimeSpan ts)
    {
        _timeSinceLastShot += (float)ts.TotalSeconds;
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        if (keyCode == KeyCodes.Space && _timeSinceLastShot >= FireRate)
        {
            SpawnProjectile();
            _timeSinceLastShot = 0.0f;
        }
    }

    private void SpawnProjectile()
    {
        var position = GetPosition();
        var direction = GetForward();

        var projectile = CreateEntity("Projectile");

        var transform = projectile.AddComponent<TransformComponent>();
        transform.Translation = position;

        var sprite = projectile.AddComponent<Sprite2DComponent>();
        sprite.TexturePath = "assets/textures/projectile.png";

        var rb = projectile.AddComponent<RigidBody2DComponent>();
        rb.BodyType = BodyType.Dynamic;
        rb.LinearVelocity = new Vector2(direction.X, direction.Y) * ProjectileSpeed;

        // Add script to destroy after 3 seconds
        var lifetime = projectile.AddComponent<NativeScriptComponent>();
        lifetime.ScriptName = "TimedDestroy";
    }
}
```

## Physics Interactions

```csharp
public class CollisionHandler : ScriptableEntity
{
    public int Damage = 10;

    public override void OnCollisionBegin(Entity other)
    {
        if (other.Name == "Player")
        {
            // Damage player (would access health component)
            Logger.Info("Hit player for {Damage} damage", Damage);
        }
        else if (other.Name == "Wall")
        {
            // Bounce off wall
            if (HasComponent<RigidBody2DComponent>())
            {
                var rb = GetComponent<RigidBody2DComponent>();
                rb.LinearVelocity *= -0.8f; // Reverse with damping
            }
        }
    }

    public override void OnTriggerEnter(Entity other)
    {
        if (other.Name == "Pickup")
        {
            Logger.Info("Collected item!");
            DestroyEntity(other);
        }
    }
}
```

## Component Communication

```csharp
public class HealthSystem : ScriptableEntity
{
    public int MaxHealth = 100;
    private int _currentHealth;

    public override void OnCreate()
    {
        _currentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Play death animation
        if (HasComponent<AnimationComponent>())
        {
            var anim = GetComponent<AnimationComponent>();
            // Trigger death animation
        }

        // Notify other scripts
        var enemyAI = FindEntity("EnemyManager");
        // Could call method on manager script
    }
}

public class DamageDealer : ScriptableEntity
{
    public int DamageAmount = 25;

    public override void OnCollisionBegin(Entity other)
    {
        // Get the other entity's health script
        if (other.HasComponent<NativeScriptComponent>())
        {
            var scriptComponent = other.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity is HealthSystem healthSystem)
            {
                healthSystem.TakeDamage(DamageAmount);
            }
        }
    }
}
```

## State Machine Pattern

```csharp
public class EnemyAI : ScriptableEntity
{
    private enum State { Idle, Patrol, Chase, Attack }

    public float PatrolSpeed = 2.0f;
    public float ChaseSpeed = 5.0f;
    public float DetectionRange = 10.0f;

    private State _currentState = State.Idle;
    private Entity? _player;

    public override void OnCreate()
    {
        _player = FindEntity("Player");
        _currentState = State.Patrol;
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;

        switch (_currentState)
        {
            case State.Idle:
                UpdateIdle(deltaTime);
                break;
            case State.Patrol:
                UpdatePatrol(deltaTime);
                break;
            case State.Chase:
                UpdateChase(deltaTime);
                break;
            case State.Attack:
                UpdateAttack(deltaTime);
                break;
        }
    }

    private void UpdateIdle(float deltaTime)
    {
        // Transition to patrol after delay
    }

    private void UpdatePatrol(float deltaTime)
    {
        var distanceToPlayer = Vector3.Distance(GetPosition(), _player.Value.GetComponent<TransformComponent>().Translation);

        if (distanceToPlayer < DetectionRange)
        {
            _currentState = State.Chase;
        }
    }

    private void UpdateChase(float deltaTime)
    {
        var playerPos = _player.Value.GetComponent<TransformComponent>().Translation;
        var direction = Vector3.Normalize(playerPos - GetPosition());

        SetPosition(GetPosition() + direction * ChaseSpeed * deltaTime);
    }

    private void UpdateAttack(float deltaTime)
    {
        // Attack logic
    }
}
```

## Object Pooling

```csharp
public class BulletPool : ScriptableEntity
{
    public int PoolSize = 20;
    private List<Entity> _pooledBullets = new();

    public override void OnCreate()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var bullet = CreateEntity("PooledBullet");

            var transform = bullet.AddComponent<TransformComponent>();
            transform.Translation = new Vector3(0, -1000, 0); // Off-screen

            var sprite = bullet.AddComponent<Sprite2DComponent>();
            sprite.TexturePath = "assets/textures/bullet.png";

            _pooledBullets.Add(bullet);
        }
    }

    public Entity? GetBullet()
    {
        foreach (var bullet in _pooledBullets)
        {
            var transform = bullet.GetComponent<TransformComponent>();
            if (transform.Translation.Y < -100) // Is inactive
            {
                return bullet;
            }
        }
        return null; // Pool exhausted
    }

    public void ReturnBullet(Entity bullet)
    {
        var transform = bullet.GetComponent<TransformComponent>();
        transform.Translation = new Vector3(0, -1000, 0);
    }
}
```

## Timed Destruction

```csharp
public class TimedDestroy : ScriptableEntity
{
    public float Lifetime = 3.0f;
    private float _timeAlive = 0.0f;

    public override void OnUpdate(TimeSpan ts)
    {
        _timeAlive += (float)ts.TotalSeconds;

        if (_timeAlive >= Lifetime)
        {
            DestroyEntity(Entity);
        }
    }
}
```
