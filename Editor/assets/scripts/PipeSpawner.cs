using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class PipeSpawner : ScriptableEntity
{
    // Screen/camera bounds (adjust based on your camera setup)
    private const float SCREEN_RIGHT = 16.0f;   // Where pipes spawn (off-screen right)
    private const float SCREEN_LEFT = -16.0f;   // Where pipes are destroyed (off-screen left)
    private const float BIRD_X_POSITION = 4.0f; // Must match FlappyBirdController.BIRD_X_POSITION
    
    // Pipe settings
    private float pipeSpeed = 4.0f;         // Speed pipes move left
    private float spawnInterval = 2.5f;     // Time between pipe spawns
    private float pipeGap = 3.5f;          // Gap between top and bottom pipe
    private float pipeWidth = 1.5f;        // Width of pipe for cleanup detection
    
    // Pipe positioning
    private float minPipeY = -2.0f;        // Minimum Y for gap center
    private float maxPipeY = 2.0f;         // Maximum Y for gap center
    private float pipeHeight = 6.0f;       // Height of each pipe segment
    
    // Spawning state
    private float spawnTimer = 0.0f;
    private int pipeCounter = 0;           // For unique pipe naming
    private Random random = new Random();
    
    // Game Manager connection
    private Entity gameManagerEntity;
    private FlappyBirdGameManager gameManager;
    private bool hasConnectedToGameManager = false;
    
    // Debug
    private float debugLogTimer = 0.0f;
    private bool enableDebugLogs = false;

    public override void OnCreate()
    {
        Console.WriteLine("[PipeSpawner] Simplified pipe spawner initialized!");
        
        // Connect to Game Manager
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is FlappyBirdGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
                Console.WriteLine("[PipeSpawner] Connected to Game Manager");
            }
        }
        
        Console.WriteLine($"[PipeSpawner] Settings - Speed: {pipeSpeed}, Interval: {spawnInterval}s, Gap: {pipeGap}");
        Console.WriteLine($"[PipeSpawner] Spawn at X: {SCREEN_RIGHT}, Destroy at X: {SCREEN_LEFT}");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        debugLogTimer += deltaTime;
        
        // Only spawn and move pipes during gameplay
        bool shouldBeActive = gameManager?.GetGameState() == GameState.Playing;
        
        if (shouldBeActive)
        {
            HandlePipeSpawning(deltaTime);
            MovePipes(deltaTime);
            CleanupOffscreenPipes();
            CheckForScoring();
        }
        
        // Debug logging
        if (enableDebugLogs && debugLogTimer >= 2.0f)
        {
            int pipeCount = CountPipes();
            Console.WriteLine($"[PipeSpawner] Active pipes: {pipeCount}, Next spawn in: {spawnInterval - spawnTimer:F1}s");
            debugLogTimer = 0.0f;
        }
    }

    private void HandlePipeSpawning(float deltaTime)
    {
        spawnTimer += deltaTime;
        
        if (spawnTimer >= spawnInterval)
        {
            SpawnPipePair();
            spawnTimer = 0.0f;
        }
    }

    private void SpawnPipePair()
    {
        // Random Y position for the gap center
        float gapCenterY = random.NextSingle() * (maxPipeY - minPipeY) + minPipeY;
        float topPipeY = gapCenterY + pipeGap / 2.0f + pipeHeight / 2.0f;
        float bottomPipeY = gapCenterY - pipeGap / 2.0f - pipeHeight / 2.0f;

        // Clamp the calculated Y values
        topPipeY = Math.Min(topPipeY, 2.5f);
        bottomPipeY = Math.Max(bottomPipeY, -1.0f);

        pipeCounter++;

        Console.WriteLine($"[PipeSpawner] Spawning pipe pair #{pipeCounter} at gap center Y: {gapCenterY:F2}");

        // Create top pipe
        CreatePipe($"Pipe_Top_{pipeCounter}", SCREEN_RIGHT, topPipeY, isTopPipe: true);

        // Create bottom pipe  
        CreatePipe($"Pipe_Bottom_{pipeCounter}", SCREEN_RIGHT, bottomPipeY, isTopPipe: false);

        Console.WriteLine($"[PipeSpawner] Pipe pair spawned - Top Y: {topPipeY:F2}, Bottom Y: {bottomPipeY:F2}");
    }

    private void CreatePipe(string name, float x, float y, bool isTopPipe)
    {
        try
        {
            var pipeEntity = CreateEntity(name);
            
            // Add transform
            var transform = new TransformComponent
            {
                Translation = new Vector3(x, y, 0.0f),
                Scale = new Vector3(pipeWidth, pipeHeight, 1.0f)
            };
            pipeEntity.AddComponent(transform);
            
            // Add visual component (SpriteRenderer)
            var sprite = new SpriteRendererComponent
            {
                Color = isTopPipe ? new Vector4(0.2f, 0.8f, 0.2f, 1.0f) : new Vector4(0.8f, 0.2f, 0.2f, 1.0f)
            };
            pipeEntity.AddComponent(sprite);
            
            // Add collider for collision detection
            var collider = new BoxCollider2DComponent
            {
                Size = new Vector2(pipeWidth, pipeHeight),
                Offset = new Vector2(0.5f, 0.5f),
                Density = 0,
                Friction = 1,
                Restitution = 0,
                RestitutionThreshold = 0.5f,
                IsTrigger = false
            };
            pipeEntity.AddComponent(collider);

            var rigidBody = new RigidBody2DComponent
            {
                BodyType = RigidBodyType.Static,
                FixedRotation = false
            };
            pipeEntity.AddComponent(rigidBody);
            
            // Add custom component to track if this pipe has been scored
            var pipeData = new PipeDataComponent
            {
                hasScored = false,
                pairId = pipeCounter
            };
            pipeEntity.AddComponent(pipeData);
            
            Console.WriteLine($"[PipeSpawner] Created {name} at ({x:F2}, {y:F2})");

            // Ensure physics body is created for this pipe using global scene access
            CurrentScene.Instance?.AddPhysicsBodyForEntity(pipeEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PipeSpawner] Error creating pipe {name}: {ex.Message}");
        }
    }

    private void MovePipes(float deltaTime)
    {
        float moveDistance = pipeSpeed * deltaTime;
        
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_"))
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform != null)
                {
                    // Move pipe left
                    var pos = transform.Translation;
                    transform.Translation = new Vector3(pos.X - moveDistance, pos.Y, pos.Z);
                }
            }
        }
    }

    private void CleanupOffscreenPipes()
    {
        var pipesToRemove = new List<Entity>();
        
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_"))
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform != null && transform.Translation.X < SCREEN_LEFT)
                {
                    pipesToRemove.Add(entity);
                }
            }
        }
        
        foreach (var pipe in pipesToRemove)
        {
            Console.WriteLine($"[PipeSpawner] Destroying offscreen pipe: {pipe.Name}");
            DestroyEntity(pipe);
        }
        
        if (pipesToRemove.Count > 0)
        {
            Console.WriteLine($"[PipeSpawner] Cleaned up {pipesToRemove.Count} offscreen pipes");
        }
    }

    private void CheckForScoring()
    {
        // Since bird X is fixed, we just check if any unscored pipe has passed the bird
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_Top_")) // Only check top pipes to avoid double scoring
            {
                var transform = entity.GetComponent<TransformComponent>();
                var pipeData = entity.GetComponent<PipeDataComponent>();
                
                if (transform != null && pipeData != null && !pipeData.hasScored)
                {
                    float pipeX = transform.Translation.X;
                    
                    // If pipe has passed the bird (pipe right edge passed bird center)
                    if (pipeX + pipeWidth/2 < BIRD_X_POSITION)
                    {
                        pipeData.hasScored = true;
                        
                        // Also mark the corresponding bottom pipe as scored
                        string bottomPipeName = entity.Name.Replace("Pipe_Top_", "Pipe_Bottom_");
                        var bottomPipe = FindEntity(bottomPipeName);
                        if (bottomPipe != null)
                        {
                            var bottomPipeData = bottomPipe.GetComponent<PipeDataComponent>();
                            if (bottomPipeData != null)
                            {
                                bottomPipeData.hasScored = true;
                            }
                        }
                        
                        // Notify game manager
                        if (gameManager != null)
                        {
                            gameManager.IncrementScore();
                            Console.WriteLine($"[PipeSpawner] Score! Pipe pair {pipeData.pairId} passed bird at X: {BIRD_X_POSITION}");
                        }
                    }
                }
            }
        }
    }

    // Public methods for Game Manager
    public void DestroyAllPipes()
    {
        Console.WriteLine("[PipeSpawner] Destroying all pipes for game restart");
        var pipesToRemove = new List<Entity>();
        
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_"))
            {
                pipesToRemove.Add(entity);
            }
        }
        
        foreach (var pipe in pipesToRemove)
        {
            DestroyEntity(pipe);
        }
        
        Console.WriteLine($"[PipeSpawner] Destroyed {pipesToRemove.Count} pipes");
        
        // Reset spawning state
        spawnTimer = 0.0f;
        pipeCounter = 0;
    }
    
    private int CountPipes()
    {
        int count = 0;
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_"))
            {
                count++;
            }
        }
        return count;
    }
}

// Custom component to track pipe scoring state
public class PipeDataComponent : Component
{
    public bool hasScored = false;
    public int pairId = 0;
}