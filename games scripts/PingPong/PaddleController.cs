using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class PaddleController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float paddleSpeed = 8.0f;
    public bool isPlayerOne = true; // true = left paddle (WASD), false = right paddle (arrows)
    public float boundaryTop = 4.0f;
    public float boundaryBottom = -4.0f;
    
    // Private fields
    private TransformComponent transformComponent;
    private RigidBody2DComponent rigidBodyComponent;
    private Vector3 startPosition;
    
    public override void OnCreate()
    {
        Console.WriteLine($"[PaddleController] Paddle created - Player {(isPlayerOne ? "One" : "Two")}");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[PaddleController] ERROR: No TransformComponent found!");
            return;
        }
        
        transformComponent = GetComponent<TransformComponent>();
        startPosition = transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            rigidBodyComponent = GetComponent<RigidBody2DComponent>();
            Console.WriteLine("[PaddleController] Physics body found");
        }
        
        Console.WriteLine($"[PaddleController] Paddle initialized at position: {startPosition}");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        HandleInput(deltaTime);
        EnforceBoundaries();
    }

    private void HandleInput(float deltaTime)
    {
        float moveDirection = 0f;
        
        if (isPlayerOne)
        {
            // Player 1 controls (W/S keys)
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.W))
                moveDirection = 1f; // Move up
            else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.S))
                moveDirection = -1f; // Move down
        }
        else
        {
            // Player 2 controls (Arrow keys)
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Up))
                moveDirection = 1f; // Move up
            else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Down))
                moveDirection = -1f; // Move down
        }
        
        // Apply movement
        if (Math.Abs(moveDirection) > 0.01f)
        {
            float moveAmount = moveDirection * paddleSpeed * deltaTime;
            
            if (rigidBodyComponent?.RuntimeBody != null)
            {
                // Use physics movement
                var body = rigidBodyComponent.RuntimeBody;
                var currentPos = body.GetPosition();
                var newY = currentPos.Y + moveAmount;
                
                // Clamp to boundaries
                newY = Math.Clamp(newY, boundaryBottom, boundaryTop);
                
                body.SetTransform(new Vector2(currentPos.X, newY), body.GetAngle());
            }
            else
            {
                // Use direct transform movement
                var currentPos = transformComponent.Translation;
                var newY = currentPos.Y + moveAmount;
                
                // Clamp to boundaries
                newY = Math.Clamp(newY, boundaryBottom, boundaryTop);
                
                transformComponent.Translation = new Vector3(currentPos.X, newY, currentPos.Z);
                SetComponent(transformComponent);
            }
        }
    }
    
    private void EnforceBoundaries()
    {
        var currentPos = transformComponent.Translation;
        
        if (currentPos.Y > boundaryTop || currentPos.Y < boundaryBottom)
        {
            var clampedY = Math.Clamp(currentPos.Y, boundaryBottom, boundaryTop);
            transformComponent.Translation = new Vector3(currentPos.X, clampedY, currentPos.Z);
            SetComponent(transformComponent);
            
            // Also update physics body if present
            if (rigidBodyComponent?.RuntimeBody != null)
            {
                var body = rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(currentPos.X, clampedY), body.GetAngle());
            }
        }
    }
    
    public void ResetPosition()
    {
        Console.WriteLine($"[PaddleController] Resetting paddle to start position: {startPosition}");
        
        transformComponent.Translation = startPosition;
        SetComponent(transformComponent);
        
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            var body = rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(startPosition.X, startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
    }
    
    public Vector3 GetPosition()
    {
        return transformComponent.Translation;
    }
    
    public float GetHeight()
    {
        // Assuming paddle height is determined by scale
        return transformComponent.Scale.Y;
    }
}