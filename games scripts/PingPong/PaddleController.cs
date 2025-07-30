using System;
using System.Numerics;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class PaddleController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float PaddleSpeed = 8.0f;
    public bool IsPlayerOne = true; // true = left paddle (WASD), false = right paddle (arrows)
    public float BoundaryTop = 4.0f;
    public float BoundaryBottom = -4.0f;
    
    // Private fields
    private TransformComponent _transformComponent;
    private RigidBody2DComponent _rigidBodyComponent;
    private Vector3 _startPosition;
    
    public override void OnCreate()
    {
        Console.WriteLine($"[PaddleController] Paddle created - Player {(IsPlayerOne ? "One" : "Two")}");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[PaddleController] ERROR: No TransformComponent found!");
            return;
        }
        
        _transformComponent = GetComponent<TransformComponent>();
        _startPosition = _transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            _rigidBodyComponent = GetComponent<RigidBody2DComponent>();
            Console.WriteLine("[PaddleController] Physics body found");
        }
        
        Console.WriteLine($"[PaddleController] Paddle initialized at position: {_startPosition}");
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
        
        if (IsPlayerOne)
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
            float moveAmount = moveDirection * PaddleSpeed * deltaTime;
            
            if (_rigidBodyComponent?.RuntimeBody != null)
            {
                // Use physics movement
                var body = _rigidBodyComponent.RuntimeBody;
                var currentPos = body.GetPosition();
                var newY = currentPos.Y + moveAmount;
                
                // Clamp to boundaries
                newY = Math.Clamp(newY, BoundaryBottom, BoundaryTop);
                
                body.SetTransform(new Vector2(currentPos.X, newY), body.GetAngle());
            }
            else
            {
                // Use direct transform movement
                var currentPos = _transformComponent.Translation;
                var newY = currentPos.Y + moveAmount;
                
                // Clamp to boundaries
                newY = Math.Clamp(newY, BoundaryBottom, BoundaryTop);
                
                _transformComponent.Translation = new Vector3(currentPos.X, newY, currentPos.Z);
            }
        }
    }
    
    private void EnforceBoundaries()
    {
        var currentPos = _transformComponent.Translation;
        
        if (currentPos.Y > BoundaryTop || currentPos.Y < BoundaryBottom)
        {
            var clampedY = Math.Clamp(currentPos.Y, BoundaryBottom, BoundaryTop);
            _transformComponent.Translation = new Vector3(currentPos.X, clampedY, currentPos.Z);
            
            // Also update physics body if present
            if (_rigidBodyComponent?.RuntimeBody != null)
            {
                var body = _rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(currentPos.X, clampedY), body.GetAngle());
            }
        }
    }
    
    public void ResetPosition()
    {
        Console.WriteLine($"[PaddleController] Resetting paddle to start position: {_startPosition}");
        
        _transformComponent.Translation = _startPosition;
        
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            var body = _rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(_startPosition.X, _startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
    }
}