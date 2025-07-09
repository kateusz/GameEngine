using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;
using Engine.Math;
using ECS;

public class FlappyBirdController : ScriptableEntity
{
    private float jumpForce = 1.0f;
    private bool isDead = false;
    private RigidBody2DComponent rigidBodyComponent;
    private TransformComponent transformComponent;
    private BoxCollider2DComponent colliderComponent;
    private float rotationSmoothness = 3.0f;
    private float maxRotationAngle = 30.0f; // degrees

    // Game Manager communication
    private Entity gameManagerEntity;
    private FlappyBirdGameManager gameManager;
    private bool hasConnectedToGameManager = false;

    public bool hasLoggedComponents = false;

    public override void OnCreate()
    {
        Console.WriteLine("[FlappyBirdController] OnCreate() called");

        // Find and connect to Game Manager
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is FlappyBirdGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
                Console.WriteLine("[FlappyBirdController] SUCCESS: Connected to Game Manager");
            }
            else
            {
                Console.WriteLine("[FlappyBirdController] WARNING: Game Manager entity found but no script");
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: Game Manager entity not found");
        }

        // Get required components
        rigidBodyComponent = GetComponent<RigidBody2DComponent>();
        transformComponent = GetComponent<TransformComponent>();
        colliderComponent = GetComponent<BoxCollider2DComponent>();

        // Debug component check
        Console.WriteLine("[FlappyBirdController] Component check:");
        Console.WriteLine($"  - RigidBody2D: {(rigidBodyComponent != null ? "✓" : "✗")}");
        Console.WriteLine($"  - Transform: {(transformComponent != null ? "✓" : "✗")}");
        Console.WriteLine($"  - BoxCollider2D: {(colliderComponent != null ? "✓" : "✗")}");
        Console.WriteLine($"  - Game Manager: {(gameManager != null ? "✓" : "✗")}");

        if (rigidBodyComponent == null)
        {
            Console.WriteLine("[FlappyBirdController] ERROR: RigidBody2DComponent required! Add it to bird entity.");
            return;
        }

        if (transformComponent == null)
        {
            Console.WriteLine("[FlappyBirdController] ERROR: TransformComponent required!");
            return;
        }

        // Ensure proper physics setup
        if (rigidBodyComponent.BodyType != RigidBodyType.Dynamic)
        {
            Console.WriteLine("[FlappyBirdController] WARNING: RigidBody should be Dynamic for physics!");
            rigidBodyComponent.BodyType = RigidBodyType.Dynamic;
        }

        Console.WriteLine("[FlappyBirdController] Successfully initialized with Box2D physics!");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        // Check if game manager says we should be dead/inactive
        if (gameManager != null && gameManager.IsGameOver())
        {
            // Game is over - don't process any bird input or movement
            return;
        }

        // If game manager exists but game isn't playing, don't handle input
        if (gameManager != null && !gameManager.IsGamePlaying() && !gameManager.IsInMenu())
        {
            return;
        }

        if (isDead) return;

        if (rigidBodyComponent?.RuntimeBody == null)
        {
            if (!hasLoggedComponents)
            {
                Console.WriteLine("[FlappyBirdController] WARNING: RuntimeBody is null. Physics may not be initialized yet.");
                hasLoggedComponents = true;
            }
            return;
        }

        try
        {
            // Get physics body velocity for rotation calculation
            var velocity = rigidBodyComponent.RuntimeBody.GetLinearVelocity();

            // Rotate bird based on physics velocity (visual feedback)
            UpdateBirdRotation(velocity.Y, (float)ts.TotalSeconds);

            // Check for out-of-bounds death (Y limits)
            if (transformComponent != null)
            {
                var position = transformComponent.Translation;
                if (position.Y < -5.0f || position.Y > 10.0f)
                {
                    Console.WriteLine($"[FlappyBirdController] Bird out of bounds at Y: {position.Y}");
                    OnDeath();
                }

                // Debug velocity logging (every 2 seconds)
                if ((DateTime.Now.Millisecond % 2000) < 50)
                {
                    Console.WriteLine($"[FlappyBirdController] Velocity: X={velocity.X:F2}, Y={velocity.Y:F2}, Pos: {position}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FlappyBirdController] Error in OnUpdate: {ex.Message}");
        }
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        // Let game manager handle menu/restart keys
        if (gameManager != null)
        {
            if (gameManager.IsInMenu() || gameManager.IsGameOver())
            {
                // Don't handle bird controls in menu or game over - let game manager handle it
                return;
            }
        }

        if (key == KeyCodes.Space && !isDead && (gameManager == null || gameManager.IsGamePlaying()))
        {
            Flap();
        }
    }

    public override void OnMouseButtonPressed(int button)
    {
        // Same logic as keyboard input
        if (gameManager != null)
        {
            if (gameManager.IsInMenu() || gameManager.IsGameOver())
            {
                return;
            }
        }

        // Left mouse button also triggers flap
        if (button == 0 && !isDead && (gameManager == null || gameManager.IsGamePlaying()))
        {
            Flap();
        }
    }

    private void Flap()
    {
        if (rigidBodyComponent?.RuntimeBody == null)
        {
            Console.WriteLine("[FlappyBirdController] Cannot flap: RuntimeBody is null!");
            return;
        }

        try
        {
            var body = rigidBodyComponent.RuntimeBody;

            // Reset Y velocity to 0 first, then apply upward impulse
            var currentVelocity = body.GetLinearVelocity();
            body.SetLinearVelocity(new Vector2(currentVelocity.X, 0));

            // Apply upward impulse (Box2D uses impulse for instant velocity change)
            var impulse = new Vector2(0, jumpForce);
            body.ApplyLinearImpulse(impulse, body.GetWorldCenter(), true);

            Console.WriteLine($"[FlappyBirdController] Flap! Applied impulse: {impulse}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FlappyBirdController] Error during flap: {ex.Message}");
        }
    }

    private void UpdateBirdRotation(float velocityY, float deltaTime)
    {
        if (transformComponent == null) return;

        try
        {
            // Calculate target rotation based on velocity
            var targetRotationDegrees = MathF.Max(-maxRotationAngle,
                MathF.Min(maxRotationAngle, -velocityY * 15.0f));

            var targetRotationRadians = MathHelpers.DegreesToRadians(targetRotationDegrees);

            // Smoothly interpolate to target rotation
            var currentRotation = transformComponent.Rotation;
            var newRotationZ = LerpAngle(currentRotation.Z, targetRotationRadians,
                rotationSmoothness * deltaTime);

            transformComponent.Rotation = new Vector3(currentRotation.X, currentRotation.Y, newRotationZ);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FlappyBirdController] Error updating rotation: {ex.Message}");
        }
    }

    private static float LerpAngle(float from, float to, float t)
    {
        var difference = to - from;
        while (difference > MathF.PI) difference -= 2 * MathF.PI;
        while (difference < -MathF.PI) difference += 2 * MathF.PI;
        return from + difference * MathF.Max(0, MathF.Min(1, t));
    }

    private void OnDeath()
    {
        if (isDead) return;

        isDead = true;
        Console.WriteLine("[FlappyBirdController] Bird died!");

        // IMPORTANT: Notify the Game Manager that the bird died
        if (gameManager != null && hasConnectedToGameManager)
        {
            Console.WriteLine("[FlappyBirdController] Notifying Game Manager of bird death");
            gameManager.TriggerGameOver();
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] WARNING: No Game Manager connected - cannot trigger game over");
        }

        // Stop bird movement
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                rigidBodyComponent.RuntimeBody.SetLinearVelocity(Vector2.Zero);
                rigidBodyComponent.RuntimeBody.SetAngularVelocity(0);
                Console.WriteLine("[FlappyBirdController] Bird physics stopped successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error stopping bird physics: {ex.Message}");
            }
        }

        Console.WriteLine("[FlappyBirdController] Game Over! Game Manager will handle restart");
    }

    // Public method for Game Manager to reset the bird
    public void ResetBird()
    {
        Console.WriteLine("[FlappyBirdController] ResetBird() called by Game Manager");

        isDead = false;

        // Reset position
        if (transformComponent != null)
        {
            try
            {
                transformComponent.Translation = new Vector3(0.0f, 4.71f, 0.0f);
                transformComponent.Rotation = Vector3.Zero;
                Console.WriteLine($"[FlappyBirdController] Bird position reset to: {transformComponent.Translation}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error resetting transform: {ex.Message}");
            }
        }

        // Reset physics body
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                var body = rigidBodyComponent.RuntimeBody;

                // Reset position in physics world
                body.SetTransform(new Vector2(-2.0f, 0.0f), 0);

                // Reset velocities
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);

                // Re-enable physics if it was disabled
                body.SetEnabled(true);

                Console.WriteLine("[FlappyBirdController] Physics body reset successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error resetting physics body: {ex.Message}");
            }
        }

        Console.WriteLine("[FlappyBirdController] Bird reset completed!");
    }

    // Box2D Collision Event Handlers
    public override void OnCollisionBegin(Entity other)
    {
        if (isDead || rigidBodyComponent?.RuntimeBody == null) return;

        Console.WriteLine($"[FlappyBirdController] Collision detected with: {other?.Name ?? "Unknown"}");

        // Handle collision with pipes, floor, or any solid objects
        if (other?.Name != null && (other.Name.Contains("Pipe") ||
                                    other.Name.Contains("Floor") ||
                                    other.Name.Contains("Ground") ||
                                    other.Name.Contains("Wall")))
        {
            Console.WriteLine($"[FlappyBirdController] Fatal collision with: {other.Name}");
            OnDeath();
        }
    }

    public override void OnCollisionEnd(Entity other)
    {
        if (rigidBodyComponent?.RuntimeBody == null) return;
        Console.WriteLine($"[FlappyBirdController] Collision ended with: {other?.Name ?? "Unknown"}");
    }

    public override void OnTriggerEnter(Entity other)
    {
        if (isDead || rigidBodyComponent?.RuntimeBody == null) return;

        Console.WriteLine($"[FlappyBirdController] Entered trigger: {other?.Name ?? "Unknown"}");

        // Handle score triggers
        if (other?.Name != null && (other.Name.Contains("ScoreZone") || other.Name.Contains("Score")))
        {
            Console.WriteLine("[FlappyBirdController] Score triggered!");
            if (gameManager != null)
            {
                gameManager.IncrementScore();
            }
        }
    }

    public override void OnTriggerExit(Entity other)
    {
        if (rigidBodyComponent?.RuntimeBody == null) return;
        Console.WriteLine($"[FlappyBirdController] Exited trigger: {other?.Name ?? "Unknown"}");
    }

    public override void OnDestroy()
    {
        Console.WriteLine("[FlappyBirdController] OnDestroy called - cleaning up");
        isDead = true;

        // Clear references
        rigidBodyComponent = null;
        transformComponent = null;
        colliderComponent = null;
        gameManager = null;
        gameManagerEntity = null;

        base.OnDestroy();
    }

    // Public getter for Game Manager to check bird status
    public bool IsBirdDead()
    {
        return isDead;
    }
}