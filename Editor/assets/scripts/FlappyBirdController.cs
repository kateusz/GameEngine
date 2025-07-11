using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class FlappyBirdController : ScriptableEntity
{
    // Fixed bird position on screen
    private const float BIRD_X_POSITION = 0.0f; // Bird stays at this X coordinate
    private const float BIRD_START_Y = 5.0f; // Starting Y position

    // Movement settings
    private float jumpForce = 0.5f;
    private float gravity = -1.0f;//-12.0f;
    private float maxFallSpeed = -1.0f;//-8.0f;

    // Current state
    private float velocityY = 0.0f;
    private bool isDead = false;
    private bool canJump = true;

    // Component references
    private TransformComponent transformComponent;
    private RigidBody2DComponent rigidBodyComponent;

    // Game Manager reference
    private Entity gameManagerEntity;
    private FlappyBirdGameManager gameManager;
    private bool hasConnectedToGameManager = false;

    // Debug settings
    private float debugLogTimer = 0.0f;
    private bool enableDebugLogs = false;

    public override void OnCreate()
    {
        Console.WriteLine("[FlappyBirdController] Simplified bird controller initialized!");

        // Get required components
        transformComponent = GetComponent<TransformComponent>();
        rigidBodyComponent = GetComponent<RigidBody2DComponent>();

        if (transformComponent == null)
        {
            Console.WriteLine("[FlappyBirdController] ERROR: No TransformComponent found!");
            return;
        }

        // Set bird to fixed X position
        ResetBird();

        // Find and connect to Game Manager
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity is FlappyBirdGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
                Console.WriteLine("[FlappyBirdController] Successfully connected to Game Manager");
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: Game Manager not found!");
        }

        Console.WriteLine($"[FlappyBirdController] Bird positioned at fixed X: {BIRD_X_POSITION}");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (isDead) return;

        float deltaTime = (float)ts.TotalSeconds;
        debugLogTimer += deltaTime;

        // Check if we should respond to input (only during gameplay)
        bool canControl = gameManager?.GetGameState() == GameState.Playing;

        if (canControl)
        {
            UpdateMovement(deltaTime);
            // Mouse input for jump (optional: move to OnMouseButtonPressed)
            // if (canJump && InputState.Instance.Mouse.IsMouseButtonPressed(0))
            // {
            //     Jump();
            // }
        }
        else if (gameManager?.GetGameState() == GameState.Menu)
        {
            // Mouse input for start (optional: move to OnMouseButtonPressed)
            if (InputState.Instance.Mouse.IsMouseButtonPressed(0))
            {
                if (gameManager != null)
                {
                    gameManager.StartGame();
                }
            }
        }

        // Out-of-bounds check (recommended)
        var position = transformComponent.Translation;
        if (position.Y < -6.0f || position.Y > 8.0f)
            TriggerDeath();

        // Debug logging
        if (enableDebugLogs && debugLogTimer >= 1.0f)
        {
            var pos = transformComponent.Translation;
            Console.WriteLine(
                $"[FlappyBirdController] Pos: ({pos.X:F2}, {pos.Y:F2}), VelY: {velocityY:F2}, State: {gameManager?.GetGameState()}");
            debugLogTimer = 0.0f;
        }
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        if (isDead) return;
        bool canControl = gameManager?.GetGameState() == GameState.Playing;
        if (canControl)
        {
            if (canJump && keyCode == KeyCodes.Space)
            {
                Jump();
            }
        }
        else if (gameManager?.GetGameState() == GameState.Menu)
        {
            if (keyCode == KeyCodes.Space && gameManager != null)
            {
                gameManager.StartGame();
            }
        }
    }

    public override void OnCollisionBegin(Entity other)
    {
        Console.WriteLine($"[FlappyBirdController] OnCollisionBegin called with {other.Name}");
        Console.WriteLine($"[FlappyBirdController] Bird isDead: {isDead}");
        
        // If already dead, ignore further collisions
        if (isDead) 
        {
            Console.WriteLine($"[FlappyBirdController] Bird is already dead, ignoring collision");
            return;
        }

        Console.WriteLine($"[FlappyBirdController] COLLISION DETECTED with {other.Name}");
        Console.WriteLine($"[FlappyBirdController] Bird position: {GetBirdPosition()}");
        
        if (other.HasComponent<TransformComponent>())
        {
            var otherTransform = other.GetComponent<TransformComponent>();
            Console.WriteLine($"[FlappyBirdController] Other entity position: {otherTransform.Translation}");
        }

        // Check if collided with a pipe or ground
        // You may want to refine this check based on your naming or tagging convention
        if (other.Name.Contains("Pipe"))
        {
            Console.WriteLine($"[FlappyBirdController] Collision with {other.Name}, triggering death.");
            TriggerDeath();
        }
        else
        {
            Console.WriteLine($"[FlappyBirdController] Collision with non-pipe entity: {other.Name}");
        }
    }

    private void Jump()
    {
        velocityY = jumpForce;
        Console.WriteLine($"[FlappyBirdController] Bird jumped! New velocity Y: {velocityY}");

        // If using physics, apply impulse
        if (rigidBodyComponent?.RuntimeBody == null)
            return;

        try
        {
            // Reset Y velocity and apply upward impulse
            var body = rigidBodyComponent.RuntimeBody;
            var currentVel = body.GetLinearVelocity();
            body.SetLinearVelocity(new Vector2(0, velocityY)); // No X movement!
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FlappyBirdController] Physics jump error: {ex.Message}");
        }
    }

    private void UpdateMovement(float deltaTime)
    {
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            // Using physics - just ensure X stays fixed and apply gravity
            try
            {
                var body = rigidBodyComponent.RuntimeBody;
                var vel = body.GetLinearVelocity();

                // Force X velocity to 0 and update Y with gravity
                velocityY = vel.Y + gravity * deltaTime;
                velocityY = Math.Max(velocityY, maxFallSpeed);

                body.SetLinearVelocity(new Vector2(0, velocityY));

                // Ensure X position stays fixed
                var pos = body.GetPosition();
                if (Math.Abs(pos.X - BIRD_X_POSITION) > 0.1f)
                {
                    body.SetTransform(new Vector2(BIRD_X_POSITION, pos.Y), body.GetAngle());
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
            velocityY += gravity * deltaTime;
            velocityY = Math.Max(velocityY, maxFallSpeed);

            var currentPos = transformComponent.Translation;
            transformComponent.Translation = new Vector3(
                BIRD_X_POSITION, // X always stays the same!
                currentPos.Y + velocityY * deltaTime,
                currentPos.Z
            );
        }
    }

    private void TriggerDeath()
    {
        Console.WriteLine("[FlappyBirdController] TriggerDeath() called");
        
        if (isDead) 
        {
            Console.WriteLine("[FlappyBirdController] Bird is already dead, ignoring TriggerDeath");
            return;
        }

        Console.WriteLine("[FlappyBirdController] ====== BIRD DEATH ======");
        isDead = true;
        canJump = false;

        Console.WriteLine("[FlappyBirdController] Bird state set to dead");

        // Stop movement
        velocityY = 0;
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                rigidBodyComponent.RuntimeBody.SetLinearVelocity(Vector2.Zero);
                rigidBodyComponent.RuntimeBody.SetAngularVelocity(0);
                Console.WriteLine("[FlappyBirdController] Physics body stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error stopping physics: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] No physics body to stop");
        }

        // Notify Game Manager
        Console.WriteLine($"[FlappyBirdController] hasConnectedToGameManager: {hasConnectedToGameManager}");
        if (hasConnectedToGameManager)
        {
            Console.WriteLine("[FlappyBirdController] Notifying Game Manager of death");
            try
            {
                gameManager.TriggerGameOver();
                Console.WriteLine("[FlappyBirdController] Game Manager notified successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error notifying Game Manager: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: Cannot notify Game Manager");
        }
        
        Console.WriteLine("[FlappyBirdController] ====== BIRD DEATH COMPLETE ======");
    }

    // Public methods for Game Manager
    public void ResetBird()
    {
        Console.WriteLine("[FlappyBirdController] Resetting bird to initial state");

        isDead = false;
        canJump = true;
        velocityY = 0;

        // Reset to fixed position

        transformComponent.Translation = new Vector3(BIRD_X_POSITION, BIRD_START_Y, 0.0f);
        transformComponent.Rotation = Vector3.Zero;
        Console.WriteLine($"[FlappyBirdController] Bird reset to position: ({BIRD_X_POSITION}, {BIRD_START_Y})");

        // Reset physics
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                var body = rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(BIRD_X_POSITION, BIRD_START_Y), 0);
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
        return isDead;
    }

    public Vector3 GetBirdPosition()
    {
        return transformComponent?.Translation ?? Vector3.Zero;
    }

    // Since bird X is fixed, this is just a constant
    public float GetBirdX()
    {
        return BIRD_X_POSITION;
    }
}