using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class FlappyBirdController : ScriptableEntity
{
    // Fixed bird position on screen
    private const float BirdXPosition = 0.0f; // Bird stays at this X coordinate
    private const float BirdStartY = 5.0f; // Starting Y position

    // Movement settings
    private float _jumpForce = 0.5f;
    private float _gravity = -1.0f;//-12.0f;
    private float _maxFallSpeed = -1.0f;//-8.0f;

    // Current state
    private float _velocityY = 0.0f;
    private bool _isDead = false;
    private bool _canJump = true;

    // Component references
    private TransformComponent _transformComponent;
    private RigidBody2DComponent _rigidBodyComponent;

    // Game Manager reference
    private Entity _gameManagerEntity;
    private FlappyBirdGameManager _gameManager;
    private bool _hasConnectedToGameManager = false;

    // Debug settings
    private float _debugLogTimer = 0.0f;
    private bool _enableDebugLogs = false;

    public override void OnCreate()
    {
        Console.WriteLine("[FlappyBirdController] OnCreate called");

        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[FlappyBirdController] ERROR: No TransformComponent found!");
            return;
        }

        if (!HasComponent<RigidBody2DComponent>())
        {
            Console.WriteLine("[FlappyBirdController] ERROR: No RigidBody2DComponent found!");
            return;
        }

        _transformComponent = GetComponent<TransformComponent>();
        _rigidBodyComponent = GetComponent<RigidBody2DComponent>();

        // Set bird to fixed X position
        ResetBird();

        // Find and connect to Game Manager
        _gameManagerEntity = FindEntity("Game Manager");
        if (_gameManagerEntity != null)
        {
            var scriptComponent = _gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity is FlappyBirdGameManager manager)
            {
                _gameManager = manager;
                _hasConnectedToGameManager = true;
                Console.WriteLine("[FlappyBirdController] Successfully connected to Game Manager");
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: Game Manager not found!");
        }

        Console.WriteLine($"[FlappyBirdController] Bird positioned at fixed X: {BirdXPosition}");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (_isDead) return;

        float deltaTime = (float)ts.TotalSeconds;
        _debugLogTimer += deltaTime;

        // Check if we should respond to input (only during gameplay)
        bool canControl = _gameManager?.GetGameState() == GameState.Playing;

        if (canControl)
        {
            UpdateMovement(deltaTime);
            // Mouse input for jump (optional: move to OnMouseButtonPressed)
            // if (canJump && InputState.Instance.Mouse.IsMouseButtonPressed(0))
            // {
            //     Jump();
            // }
        }
        else if (_gameManager?.GetGameState() == GameState.Menu)
        {
            // Mouse input for start (optional: move to OnMouseButtonPressed)
            if (InputState.Instance.Mouse.IsMouseButtonPressed(0))
            {
                if (_gameManager != null)
                {
                    _gameManager.StartGame();
                }
            }
        }

        // Out-of-bounds check (recommended)
        var position = _transformComponent.Translation;
        if (position.Y < -6.0f || position.Y > 8.0f)
            TriggerDeath();

        // Debug logging
        if (_enableDebugLogs && _debugLogTimer >= 1.0f)
        {
            var pos = _transformComponent.Translation;
            Console.WriteLine(
                $"[FlappyBirdController] Pos: ({pos.X:F2}, {pos.Y:F2}), VelY: {_velocityY:F2}, State: {_gameManager?.GetGameState()}");
            _debugLogTimer = 0.0f;
        }
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        if (_isDead) return;
        bool canControl = _gameManager?.GetGameState() == GameState.Playing;
        if (canControl)
        {
            if (_canJump && keyCode == KeyCodes.Space)
            {
                Jump();
            }
        }
        else if (_gameManager?.GetGameState() == GameState.Menu)
        {
            if (keyCode == KeyCodes.Space && _gameManager != null)
            {
                _gameManager.StartGame();
            }
        }
    }

    public override void OnCollisionBegin(Entity other)
    {
        // If already dead, ignore further collisions
        if (_isDead) return;

        // Check if collided with a pipe or ground
        // You may want to refine this check based on your naming or tagging convention
        if (other.Name.Contains("Pipe"))
        {
            Console.WriteLine($"[FlappyBirdController] Collision with {other.Name}, triggering death.");
            TriggerDeath();
        }
    }

    private void Jump()
    {
        _velocityY = _jumpForce;
        Console.WriteLine($"[FlappyBirdController] Bird jumped! New velocity Y: {_velocityY}");

        // If using physics, apply impulse
        if (_rigidBodyComponent?.RuntimeBody == null)
            return;

        try
        {
            // Reset Y velocity and apply upward impulse
            var body = _rigidBodyComponent.RuntimeBody;
            var currentVel = body.GetLinearVelocity();
            body.SetLinearVelocity(new Vector2(0, _velocityY)); // No X movement!
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FlappyBirdController] Physics jump error: {ex.Message}");
        }
    }

    private void UpdateMovement(float deltaTime)
    {
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            // Using physics - just ensure X stays fixed and apply gravity
            try
            {
                var body = _rigidBodyComponent.RuntimeBody;
                var vel = body.GetLinearVelocity();

                // Force X velocity to 0 and update Y with gravity
                _velocityY = vel.Y + _gravity * deltaTime;
                _velocityY = Math.Max(_velocityY, _maxFallSpeed);

                body.SetLinearVelocity(new Vector2(0, _velocityY));

                // Ensure X position stays fixed
                var pos = body.GetPosition();
                if (Math.Abs(pos.X - BirdXPosition) > 0.1f)
                {
                    body.SetTransform(new Vector2(BirdXPosition, pos.Y), body.GetAngle());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Physics update error: {ex.Message}");
            }
        }
        else
        {
            // Manual movement - apply gravity and update position
            _velocityY += _gravity * deltaTime;
            _velocityY = Math.Max(_velocityY, _maxFallSpeed);

            var currentPos = _transformComponent.Translation;
            _transformComponent.Translation = new Vector3(
                BirdXPosition, // X always stays the same!
                currentPos.Y + _velocityY * deltaTime,
                currentPos.Z
            );
        }
    }

    private void TriggerDeath()
    {
        if (_isDead) return;

        Console.WriteLine("[FlappyBirdController] ====== BIRD DEATH ======");
        _isDead = true;
        _canJump = false;

        // Stop movement
        _velocityY = 0;
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                _rigidBodyComponent.RuntimeBody.SetLinearVelocity(Vector2.Zero);
                _rigidBodyComponent.RuntimeBody.SetAngularVelocity(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error stopping physics: {ex.Message}");
            }
        }

        // Notify Game Manager
        if (_hasConnectedToGameManager)
        {
            Console.WriteLine("[FlappyBirdController] Notifying Game Manager of death");
            _gameManager.TriggerGameOver();
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: Cannot notify Game Manager");
        }
    }

    // Public methods for Game Manager
    public void ResetBird()
    {
        Console.WriteLine("[FlappyBirdController] Resetting bird to initial state");

        _isDead = false;
        _canJump = true;
        _velocityY = 0;

        // Reset to fixed position

        _transformComponent.Translation = new Vector3(BirdXPosition, BirdStartY, 0.0f);
        _transformComponent.Rotation = Vector3.Zero;
        Console.WriteLine($"[FlappyBirdController] Bird reset to position: ({BirdXPosition}, {BirdStartY})");

        // Reset physics
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                var body = _rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(BirdXPosition, BirdStartY), 0);
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetEnabled(true);
                Console.WriteLine("[FlappyBirdController] Physics body reset");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Physics reset error: {ex.Message}");
            }
        }
    }

    public bool IsBirdDead()
    {
        return _isDead;
    }

    public Vector3 GetBirdPosition()
    {
        return _transformComponent.Translation;
    }

    // Since bird X is fixed, this is just a constant
    public float GetBirdX()
    {
        return BirdXPosition;
    }
}