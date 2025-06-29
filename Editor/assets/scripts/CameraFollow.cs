using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class CameraFollow : ScriptableEntity
{
    private Entity targetEntity;
    private Vector3 offset = new Vector3(3.0f, 0.0f, 0.0f); // Camera offset from target
    private float smoothSpeed = 2.0f; // How smooth the camera follows
    private bool followOnlyX = true; // Only follow X axis for side-scrolling
    private float debugLogTimer = 0.0f; // To prevent spamming console
    private bool hasLoggedComponentCheck = false;
    
    public override void OnCreate()
    {
        Console.WriteLine("[CameraFollow] OnCreate() called");
        
        // Debug: List all entities in scene
        Console.WriteLine("[CameraFollow] Available entities in scene:");
        foreach (var entity in CurrentScene.Entities)
        {
            Console.WriteLine($"  - Entity: {entity.Name} (ID: {entity.Id})");
        }
        
        // Find the bird entity to follow
        targetEntity = FindEntity("Bird");
        
        if (targetEntity == null)
        {
            Console.WriteLine("[CameraFollow] ERROR: Camera target (Bird) not found!");
            
            // Try alternative names
            targetEntity = FindEntity("Bird Entity") ?? FindEntity("bird");
            if (targetEntity != null)
            {
                Console.WriteLine($"[CameraFollow] Found bird with alternative name: {targetEntity.Name}");
            }
        }
        else
        {
            Console.WriteLine($"[CameraFollow] SUCCESS: Found target entity '{targetEntity.Name}' (ID: {targetEntity.Id})");
        }
        
        // Check if this entity has the required components
        var cameraTransform = GetComponent<TransformComponent>();
        if (cameraTransform == null)
        {
            Console.WriteLine("[CameraFollow] ERROR: This entity missing TransformComponent!");
        }
        else
        {
            Console.WriteLine($"[CameraFollow] Camera initial position: {cameraTransform.Translation}");
        }
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        debugLogTimer += (float)ts.TotalSeconds;
        
        if (targetEntity == null) 
        {
            if (debugLogTimer > 2.0f) // Log every 2 seconds if target is missing
            {
                Console.WriteLine("[CameraFollow] ERROR: Target entity is null, cannot follow");
                debugLogTimer = 0.0f;
            }
            return;
        }
        
        var cameraTransform = GetComponent<TransformComponent>();
        var targetTransform = targetEntity.GetComponent<TransformComponent>();
        
        // One-time component check logging
        if (!hasLoggedComponentCheck)
        {
            Console.WriteLine($"[CameraFollow] Component check:");
            Console.WriteLine($"  - Camera transform: {(cameraTransform != null ? "✓" : "✗")}");
            Console.WriteLine($"  - Target transform: {(targetTransform != null ? "✓" : "✗")}");
            hasLoggedComponentCheck = true;
        }
        
        if (cameraTransform == null)
        {
            Console.WriteLine("[CameraFollow] ERROR: Camera entity missing TransformComponent!");
            return;
        }
        
        if (targetTransform == null)
        {
            Console.WriteLine("[CameraFollow] ERROR: Target entity missing TransformComponent!");
            return;
        }
        
        // Calculate desired camera position
        var targetPosition = targetTransform.Translation + offset;
        var currentPosition = cameraTransform.Translation;
        
        Vector3 desiredPosition;
        
        if (followOnlyX)
        {
            // Only follow on X axis, keep Y and Z constant
            desiredPosition = new Vector3(
                targetPosition.X,
                currentPosition.Y, // Keep current Y
                currentPosition.Z  // Keep current Z
            );
        }
        else
        {
            desiredPosition = targetPosition;
        }
        
        // Smoothly move camera towards target
        var lerpFactor = smoothSpeed * (float)ts.TotalSeconds;
        var newPosition = Vector3.Lerp(currentPosition, desiredPosition, lerpFactor);
        
        // Debug logging every 1 second
        if (debugLogTimer > 1.0f)
        {
            Console.WriteLine($"[CameraFollow] Position update:");
            Console.WriteLine($"  - Target pos: {targetTransform.Translation:F2}");
            Console.WriteLine($"  - Current cam: {currentPosition:F2}");
            Console.WriteLine($"  - Desired cam: {desiredPosition:F2}");
            Console.WriteLine($"  - New cam pos: {newPosition:F2}");
            Console.WriteLine($"  - Lerp factor: {lerpFactor:F3}");
            Console.WriteLine($"  - Distance to target: {Vector3.Distance(currentPosition, desiredPosition):F2}");
            debugLogTimer = 0.0f;
        }
        
        cameraTransform.Translation = newPosition;
    }
    
    // Method to set a new target entity
    public void SetTarget(string entityName)
    {
        Console.WriteLine($"[CameraFollow] Attempting to set target to: {entityName}");
        targetEntity = FindEntity(entityName);
        if (targetEntity != null)
        {
            Console.WriteLine($"[CameraFollow] SUCCESS: Camera target set to: {entityName} (ID: {targetEntity.Id})");
            hasLoggedComponentCheck = false; // Reset component check for new target
        }
        else
        {
            Console.WriteLine($"[CameraFollow] ERROR: Could not find entity named: {entityName}");
        }
    }
    
    // Method to adjust camera offset
    public void SetOffset(Vector3 newOffset)
    {
        Console.WriteLine($"[CameraFollow] Offset changed from {offset} to {newOffset}");
        offset = newOffset;
    }
    
    // Method to adjust follow behavior
    public void SetFollowMode(bool xOnly)
    {
        Console.WriteLine($"[CameraFollow] Follow mode changed: X-only = {xOnly}");
        followOnlyX = xOnly;
    }
    
    // Method to adjust smoothing speed
    public void SetSmoothSpeed(float newSpeed)
    {
        Console.WriteLine($"[CameraFollow] Smooth speed changed from {smoothSpeed} to {newSpeed}");
        smoothSpeed = newSpeed;
    }
    
    // Debug method to manually trigger position logging
    public void LogCurrentState()
    {
        Console.WriteLine($"[CameraFollow] === CURRENT STATE ===");
        Console.WriteLine($"Target entity: {(targetEntity != null ? targetEntity.Name : "NULL")}");
        Console.WriteLine($"Offset: {offset}");
        Console.WriteLine($"Smooth speed: {smoothSpeed}");
        Console.WriteLine($"Follow X-only: {followOnlyX}");
        
        if (targetEntity != null)
        {
            var targetTransform = targetEntity.GetComponent<TransformComponent>();
            var cameraTransform = GetComponent<TransformComponent>();
            
            if (targetTransform != null && cameraTransform != null)
            {
                Console.WriteLine($"Target position: {targetTransform.Translation}");
                Console.WriteLine($"Camera position: {cameraTransform.Translation}");
                Console.WriteLine($"Distance: {Vector3.Distance(cameraTransform.Translation, targetTransform.Translation)}");
            }
        }
        Console.WriteLine($"========================");
    }
}