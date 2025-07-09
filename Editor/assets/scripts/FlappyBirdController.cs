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

    public bool hasLoggedComponents = false;

    public override void OnCreate()
    {
        Console.WriteLine("[FlappyBirdController] OnCreate() called");

        // Get required components
        rigidBodyComponent = GetComponent<RigidBody2DComponent>();
        transformComponent = GetComponent<TransformComponent>();
        colliderComponent = GetComponent<BoxCollider2DComponent>();

        // Debug component check
        Console.WriteLine("[FlappyBirdController] Component check:");
        Console.WriteLine($"  - RigidBody2D: {(rigidBodyComponent != null ? "✓" : "✗")}");
        Console.WriteLine($"  - Transform: {(transformComponent != null ? "✓" : "✗")}");
        Console.WriteLine($"  - BoxCollider2D: {(colliderComponent != null ? "✓" : "✗")}");

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
        if (isDead) return;

        if (rigidBodyComponent?.RuntimeBody == null)
        {
            if (!hasLoggedComponents)
            {
                Console.WriteLine(
                    "[FlappyBirdController] WARNING: RuntimeBody is null. Physics may not be initialized yet.");
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
                    Console.WriteLine(
                        $"[FlappyBirdController] Velocity: X={velocity.X:F2}, Y={velocity.Y:F2}, Pos: {position}");
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
        if (key == KeyCodes.Space && !isDead)
        {
            Flap();
        }
        else if (key == KeyCodes.R && isDead)
        {
            Restart();
        }
    }

    public override void OnMouseButtonPressed(int button)
    {
        // Left mouse button also triggers flap
        if (button == 0 && !isDead) // 0 = left mouse button
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
        if (transformComponent == null) 
            return;

        try
        {
            // Calculate target rotation based on velocity
            // Positive velocity (going up) = nose up (negative rotation)
            // Negative velocity (falling) = nose down (positive rotation)
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

    // Helper method for angle interpolation
    private static float LerpAngle(float from, float to, float t)
    {
        var difference = to - from;

        // Wrap difference to [-π, π] range
        while (difference > MathF.PI) difference -= 2 * MathF.PI;
        while (difference < -MathF.PI) difference += 2 * MathF.PI;

        return from + difference * MathF.Max(0, MathF.Min(1, t));
    }

    private void OnDeath()
    {
        if (isDead) return;

        isDead = true;

        // Stop bird movement - wrapped in try-catch to prevent crashes
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
                // Don't rethrow - we want the game to continue working
            }
        }
        else
        {
            Console.WriteLine(
                "[FlappyBirdController] RuntimeBody is null during death - physics may have been destroyed");
        }

        Console.WriteLine("[FlappyBirdController] Game Over! Press R to restart");
        // TODO: Play death sound, show game over UI
    }

    private void Restart()
    {
        Console.WriteLine("[FlappyBirdController] Restarting game...");

        isDead = false;

        // Reset position
        if (transformComponent != null)
        {
            try
            {
                transformComponent.Translation = new Vector3(0, 0, 0);
                transformComponent.Rotation = Vector3.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlappyBirdController] Error resetting transform: {ex.Message}");
            }
        }

        // Reset physics body - wrapped in try-catch to prevent crashes
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            try
            {
                var body = rigidBodyComponent.RuntimeBody;

                // Reset position in physics world
                body.SetTransform(new Vector2(0, 0), 0);

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
                // Don't rethrow - let the game continue
            }
        }
        else
        {
            Console.WriteLine("[FlappyBirdController] RuntimeBody is null during restart - cannot reset physics");
        }

        Console.WriteLine("[FlappyBirdController] Game Restarted!");
    }

    // Box2D Collision Event Handlers
    public override void OnCollisionBegin(Entity other)
    {
        // Don't process collisions if we're already dead or if physics is destroyed
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
        // Don't process collision end events if physics is destroyed
        if (rigidBodyComponent?.RuntimeBody == null) return;

        Console.WriteLine($"[FlappyBirdController] Collision ended with: {other?.Name ?? "Unknown"}");
    }

    // Trigger events (for score zones, power-ups, etc.)
    public override void OnTriggerEnter(Entity other)
    {
        // Don't process triggers if we're dead or if physics is destroyed
        if (isDead || rigidBodyComponent?.RuntimeBody == null) return;

        Console.WriteLine($"[FlappyBirdController] Entered trigger: {other?.Name ?? "Unknown"}");

        // Handle score triggers (invisible collision zones between pipes)
        if (other?.Name != null && (other.Name.Contains("ScoreZone") || other.Name.Contains("Score")))
        {
            Console.WriteLine("[FlappyBirdController] Score triggered!");
            // TODO: Notify game manager to increase score
        }
    }

    public override void OnTriggerExit(Entity other)
    {
        // Don't process trigger exit events if physics is destroyed
        if (rigidBodyComponent?.RuntimeBody == null) return;

        Console.WriteLine($"[FlappyBirdController] Exited trigger: {other?.Name ?? "Unknown"}");
    }

    // Handle cleanup when script is destroyed
    public override void OnDestroy()
    {
        Console.WriteLine("[FlappyBirdController] OnDestroy called - cleaning up");
        isDead = true; // Prevent further physics operations

        // Clear references to help prevent issues
        rigidBodyComponent = null;
        transformComponent = null;
        colliderComponent = null;

        base.OnDestroy();
    }
}