using System;
using System.IO;
using System.Numerics;
using ECS;
using Editor;
using Engine.Platform.SilkNet.Audio;
using Engine.Scene;
using Engine.Scene.Components;

public class BallController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float InitialSpeed = 0.5f;
    public float MaxSpeed = 2.0f;
    public float SpeedIncrease = 0.1f; // Speed increase per paddle hit
    
    public float BoundaryTop = 4.5f;
    public float BoundaryBottom = -4.5f;
    public float BoundaryLeft = -8.0f;
    public float BoundaryRight = 8.0f;
    
    // Private fields
    private TransformComponent _transformComponent;
    private RigidBody2DComponent _rigidBodyComponent;
    private Vector3 _startPosition;
    private Vector2 _velocity;
    private float _currentSpeed;
    private bool _isActive = false;
    
    // Game Manager reference
    private Entity _gameManagerEntity;
    private PingPongGameManager _gameManager;
    private Random _random = new Random();
    
    public override void OnCreate()
    {
        Console.WriteLine("[BallController] Ball created");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[BallController] ERROR: No TransformComponent found!");
            return;
        }
        
        _transformComponent = GetComponent<TransformComponent>();
        _startPosition = _transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            _rigidBodyComponent = GetComponent<RigidBody2DComponent>();
            Console.WriteLine("[BallController] Physics body found");
        }
        
        // Find game manager
        _gameManagerEntity = FindEntity("Game Manager");
        if (_gameManagerEntity != null)
        {
            var scriptComponent = _gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                _gameManager = manager;
                Console.WriteLine("[BallController] Connected to Game Manager");
            }
        }
        
        _currentSpeed = InitialSpeed;
        Console.WriteLine($"[BallController] Ball initialized at position: {_startPosition}");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        if (!_isActive) return;
        
        float deltaTime = (float)ts.TotalSeconds;
        
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            // Update using physics body
            UpdatePhysicsMovement(deltaTime);
        }
        else
        {
            // Update using manual movement
            UpdateManualMovement(deltaTime);
        }
        
        CheckBoundaryCollisions();
    }
    
    private void UpdatePhysicsMovement(float deltaTime)
    {
        var body = _rigidBodyComponent.RuntimeBody;
        var currentVel = body.GetLinearVelocity();
        
        // Maintain consistent speed
        if (currentVel.Length() > 0.1f)
        {
            var normalizedVel = Vector2.Normalize(currentVel);
            body.SetLinearVelocity(normalizedVel * _currentSpeed);
        }
    }
    
    private void UpdateManualMovement(float deltaTime)
    {
        var currentPos = _transformComponent.Translation;
        var newPos = currentPos + new Vector3(_velocity.X * deltaTime, _velocity.Y * deltaTime, 0);
        
        _transformComponent.Translation = newPos;
        SetComponent(_transformComponent);
    }
    
    private void CheckBoundaryCollisions()
    {
        var body = _rigidBodyComponent.RuntimeBody;
        var currentPos = body.GetPosition();
        bool bounced = false;
        
        // Top and bottom boundaries
        if (currentPos.Y > BoundaryTop)
        {
            _transformComponent.Translation = new Vector3(currentPos.X, BoundaryTop, 0);
            _velocity = new Vector2(_velocity.X, -Math.Abs(_velocity.Y)); // Bounce down
            bounced = true;
        }
        else if (currentPos.Y < BoundaryBottom)
        {
            _transformComponent.Translation = new Vector3(currentPos.X, BoundaryBottom,0);
            _velocity = new Vector2(_velocity.X, Math.Abs(_velocity.Y)); // Bounce up
            bounced = true;
        }
        
        // Left and right boundaries (scoring)
        if (currentPos.X < BoundaryLeft)
        {
            // Player 2 scores
            OnScore(false);
        }
        else if (currentPos.X > BoundaryRight)
        {
            // Player 1 scores
            OnScore(true);
        }
        
        SetComponent(_transformComponent);
        
        if (bounced)
        {
            // Update physics body if present
            if (_rigidBodyComponent?.RuntimeBody != null)
            {
                body.SetTransform(new Vector2(_transformComponent.Translation.X, _transformComponent.Translation.Y), 0);
                body.SetLinearVelocity(_velocity);
            }
        }
    }
    
    public override void OnCollisionBegin(Entity other)
    {
        // Check if we hit a paddle
        if (other.Name.Contains("Paddle"))
        {
            OnPaddleHit(other);
        }
    }
    
    private void OnPaddleHit(Entity paddle)
    {
        Console.WriteLine($"[BallController] Ball hit paddle: {paddle.Name}");
        
        var paddlePos = paddle.GetComponent<TransformComponent>().Translation;
        var ballPos = _transformComponent.Translation;
        
        // Calculate bounce angle based on where the ball hit the paddle
        float paddleHeight = paddle.GetComponent<TransformComponent>().Scale.Y;
        float hitPoint = (ballPos.Y - paddlePos.Y) / paddleHeight; // -0.5 to 0.5
        
        // Clamp hit point
        hitPoint = Math.Clamp(hitPoint, -0.5f, 0.5f);
        
        // Calculate new velocity
        float bounceAngle = hitPoint * 60.0f; // Max 60 degrees
        float bounceAngleRad = bounceAngle * (float)Math.PI / 180.0f;
        
        // Determine direction based on which paddle was hit
        bool isLeftPaddle = paddlePos.X < 0;
        float xDirection = isLeftPaddle ? 1.0f : -1.0f;
        
        // Increase speed
        _currentSpeed = Math.Min(_currentSpeed + SpeedIncrease, MaxSpeed);
        
        // Set new velocity
        _velocity = new Vector2(
            xDirection * _currentSpeed * (float)Math.Cos(bounceAngleRad),
            _currentSpeed * (float)Math.Sin(bounceAngleRad)
        );
        
        // Update physics body if present
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            _rigidBodyComponent.RuntimeBody.SetLinearVelocity(_velocity);
        }
        
        Console.WriteLine($"[BallController] New velocity: ({_velocity.X:F2}, {_velocity.Y:F2}), Speed: {_currentSpeed:F2}");
        AudioEngine.Instance.PlayOneShot(Path.Combine(AssetsManager.AssetsPath, "audio/swing.wav"), 0.5f);
    }
    
    private void OnScore(bool player1Scored)
    {
        Console.WriteLine($"[BallController] Score! Player {(player1Scored ? "1" : "2")} scored");
        
        _isActive = false;
        
        // Notify game manager
        if (_gameManager != null)
        {
            if (player1Scored)
                _gameManager.Player1Score();
            else
                _gameManager.Player2Score();
        }
    }
    
    public void Launch(bool towardsPlayer1 = true)
    {
        Console.WriteLine($"[BallController] Launching ball towards Player {(towardsPlayer1 ? "1" : "2")}");
        
        _isActive = true;
        _currentSpeed = InitialSpeed;
        
        // Random angle between -30 and 30 degrees
        float angle = (float)(_random.NextDouble() * 60.0 - 30.0) * (float)Math.PI / 180.0f;
        float xDirection = towardsPlayer1 ? -1.0f : 1.0f;
        
        _velocity = new Vector2(
            xDirection * _currentSpeed * (float)Math.Cos(angle),
            _currentSpeed * (float)Math.Sin(angle)
        );
        
        // Update physics body if present
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            _rigidBodyComponent.RuntimeBody.SetLinearVelocity(_velocity);
        }
        
        Console.WriteLine($"[BallController] Launch velocity: ({_velocity.X:F2}, {_velocity.Y:F2})");
    }
    
    public void ResetBall()
    {
        Console.WriteLine("[BallController] Resetting ball");
        
        _isActive = false;
        _velocity = Vector2.Zero;
        _currentSpeed = InitialSpeed;
        
        _transformComponent.Translation = _startPosition;
        SetComponent(_transformComponent);
        
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            var body = _rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(_startPosition.X, _startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
    }
    
    public void Stop()
    {
        _isActive = false;
        _velocity = Vector2.Zero;
        
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            _rigidBodyComponent.RuntimeBody.SetLinearVelocity(Vector2.Zero);
        }
    }
    
    // Public getters
    public Vector3 GetPosition()
    {
        return _transformComponent.Translation;
    }
    
    public Vector2 GetVelocity()
    {
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            return _rigidBodyComponent.RuntimeBody.GetLinearVelocity();
        }
        return _velocity;
    }
    
    public float GetSpeed()
    {
        return _currentSpeed;
    }
    
    public bool IsActive()
    {
        return _isActive;
    }
}