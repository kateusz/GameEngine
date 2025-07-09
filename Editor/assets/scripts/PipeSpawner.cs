using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class PipeSpawner : ScriptableEntity
{
    public float spawnTimer = 0.0f;
    public float spawnInterval = 2.0f; // Spawn pipe every 2 seconds
    public float pipeSpeed = 3.0f;
    public float pipeGap = 4.0f; // Gap between top and bottom pipes
    public float spawnX = 15.0f; // Spawn pipes off-screen to the right
    private Random random = new Random();
    private Entity gameManagerEntity;
    private FlappyBirdGameManager gameManager;
    private bool hasConnectedToGameManager = false;
    
    public override void OnCreate()
    {
        Console.WriteLine("Pipe Spawner initialized!");
        
        // Find the game manager entity
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is FlappyBirdGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
                Console.WriteLine("[PipeSpawner] SUCCESS: Found and connected to Game Manager");
            }
            else
            {
                Console.WriteLine("[PipeSpawner] WARNING: Game Manager entity found but no FlappyBirdGameManager script");
            }
        }
        else
        {
            Console.WriteLine("[PipeSpawner] WARNING: Game Manager entity not found - pipes will spawn unconditionally");
        }
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        // Check game state - only spawn and move pipes when playing
        if (gameManager != null)
        {
            var gameState = gameManager.GetGameState();
            if (gameState != GameState.Playing)
            {
                // Don't spawn or move pipes when not playing
                return;
            }
        }
        
        spawnTimer += (float)ts.TotalSeconds;
        
        if (spawnTimer >= spawnInterval)
        {
            SpawnPipePair();
            spawnTimer = 0.0f;
        }
        
        // Move existing pipes and clean up old ones
        MovePipes(ts);
        CleanupOldPipes();
    }
    
    private void SpawnPipePair()
    {
        // Double-check game state before spawning
        if (gameManager != null && gameManager.GetGameState() != GameState.Playing)
        {
            Console.WriteLine("[PipeSpawner] Spawn cancelled - game not in Playing state");
            return;
        }
        
        // Random Y position for the gap center
        float gapCenterY = random.Next(-2, 3); // Random between -2 and 2
        
        // Create bottom pipe
        var bottomPipe = CreateEntity($"BottomPipe_{DateTime.Now.Ticks}");
        var bottomTransform = new TransformComponent
        {
            Translation = new Vector3(spawnX, gapCenterY - pipeGap/2 - 2.0f, 0),
            Scale = new Vector3(1, 4, 1)
        };

        bottomPipe.AddComponent(bottomTransform);

        var bottomSprite = new SpriteRendererComponent
        {
            // TODO: Load pipe texture
            Color = Vector4.One
        };

        bottomPipe.AddComponent(bottomSprite);

        var bottomRigidBody = new RigidBody2DComponent
        {
            BodyType = RigidBodyType.Static
        };
        bottomPipe.AddComponent(bottomRigidBody);

        var bottomCollider = new BoxCollider2DComponent
        {
            Size = new Vector2(0.5f, 2.0f)
        };
        
        bottomPipe.AddComponent(bottomCollider);
        
        // Create top pipe
        var topPipe = CreateEntity($"TopPipe_{DateTime.Now.Ticks}");
        var topTransform = new TransformComponent
        {
            Translation = new Vector3(spawnX, gapCenterY + pipeGap/2 + 2.0f, 0),
            Scale = new Vector3(1, 4, 1),
            Rotation = new Vector3(0, 0, MathF.PI) // Flip top pipe
        };

        topPipe.AddComponent(topTransform);

        var topSprite = new SpriteRendererComponent
        {
            Color = Vector4.One
        };

        topPipe.AddComponent(topSprite);

        var topRigidBody = new RigidBody2DComponent
        {
            BodyType = RigidBodyType.Static
        };

        topPipe.AddComponent(topRigidBody);

        var topCollider = new BoxCollider2DComponent
        {
            Size = new Vector2(0.5f, 2.0f)
        };

        topPipe.AddComponent(topCollider);
        
        Console.WriteLine($"Spawned pipe pair at gap center Y: {gapCenterY}");
    }
    
    private void MovePipes(TimeSpan ts)
    {
        // Double-check game state before moving pipes
        if (gameManager != null && gameManager.GetGameState() != GameState.Playing)
        {
            return; // Don't move pipes when not playing
        }
        
        // Find all pipe entities and move them left
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform != null)
                {
                    var position = transform.Translation;
                    position.X -= pipeSpeed * (float)ts.TotalSeconds;
                    transform.Translation = position;
                }
            }
        }
    }
    
    private void CleanupOldPipes()
    {
        // Always clean up old pipes regardless of game state to prevent memory leaks
        var pipesToRemove = new List<Entity>();
        
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform != null && transform.Translation.X < -10.0f)
                {
                    pipesToRemove.Add(entity);
                }
            }
        }
        
        foreach (var pipe in pipesToRemove)
        {
            DestroyEntity(pipe);
        }
        
        if (pipesToRemove.Count > 0)
        {
            Console.WriteLine($"[PipeSpawner] Cleaned up {pipesToRemove.Count} old pipes");
        }
    }
    
    // Method to pause/resume pipe spawning (can be called by game manager)
    public void SetSpawningEnabled(bool enabled)
    {
        Console.WriteLine($"[PipeSpawner] Spawning {(enabled ? "enabled" : "disabled")}");
        if (!enabled)
        {
            spawnTimer = 0.0f; // Reset spawn timer when disabled
        }
    }
    
    // Method to immediately stop all pipe movement (useful for game over)
    public void StopAllPipeMovement()
    {
        Console.WriteLine("[PipeSpawner] Stopping all pipe movement");
        // This method could be called by the game manager when game over occurs
        // The Update method will handle the actual stopping based on game state
    }
    
    // Method to clean up all pipes (useful for restarting the game)
    public void DestroyAllPipes()
    {
        Console.WriteLine("[PipeSpawner] Destroying all pipes for game restart");
        var pipesToRemove = new List<Entity>();
        
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                pipesToRemove.Add(entity);
            }
        }
        
        foreach (var pipe in pipesToRemove)
        {
            DestroyEntity(pipe);
        }
        
        Console.WriteLine($"[PipeSpawner] Destroyed {pipesToRemove.Count} pipes for restart");
        spawnTimer = 0.0f; // Reset spawn timer
    }
    
    // Debug method to check current state
    public void LogCurrentState()
    {
        Console.WriteLine($"[PipeSpawner] === CURRENT STATE ===");
        Console.WriteLine($"Game manager connected: {hasConnectedToGameManager}");
        Console.WriteLine($"Current game state: {(gameManager != null ? gameManager.GetGameState().ToString() : "Unknown")}");
        Console.WriteLine($"Spawn timer: {spawnTimer:F2}/{spawnInterval:F2}");
        Console.WriteLine($"Pipe speed: {pipeSpeed}");
        Console.WriteLine($"Pipe gap: {pipeGap}");
        Console.WriteLine($"Spawn X position: {spawnX}");
        
        // Count current pipes
        int pipeCount = 0;
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                pipeCount++;
            }
        }
        Console.WriteLine($"Current pipes in scene: {pipeCount}");
        Console.WriteLine($"========================");
    }
}