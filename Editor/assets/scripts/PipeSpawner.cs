using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class PipeSpawner : ScriptableEntity
{
    // Screen/camera bounds (adjust based on your camera setup)
    private const float ScreenRight = 7.0f;   // Where pipes spawn (off-screen right)
    private const float ScreenLeft = -16.0f;   // Where pipes are destroyed (off-screen left)
    private const float BirdXPosition = 0.0f; // Must match FlappyBirdController.BIRD_X_POSITION
    
    // Pipe settings
    private float _pipeSpeed = 4.0f;         // Speed pipes move left
    private float _spawnInterval = 2.5f;     // Time between pipe spawns
    private float _pipeGap = 3.5f;          // Gap between top and bottom pipe
    private float _pipeWidth = 1.5f;        // Width of pipe for cleanup detection
    
    // Pipe positioning
    private float _minPipeY = -2.0f;        // Minimum Y for gap center
    private float _maxPipeY = 2.0f;         // Maximum Y for gap center
    private float _pipeHeight = 6.0f;       // Height of each pipe segment
    
    // Spawning state
    private float _spawnTimer = 0.0f;
    private int _pipeCounter = 0;           // For unique pipe naming
    private Random _random = new Random();
    
    // Game Manager connection
    private Entity _gameManagerEntity;
    private FlappyBirdGameManager _gameManager;
    private bool _hasConnectedToGameManager = false;
    
    // Debug
    private float _debugLogTimer = 0.0f;
    private bool _enableDebugLogs = false;

    public override void OnCreate()
    {
        Console.WriteLine("[PipeSpawner] Simplified pipe spawner initialized!");
        
        // Connect to Game Manager
        _gameManagerEntity = FindEntity("Game Manager");
        if (_gameManagerEntity != null)
        {
            var scriptComponent = _gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is FlappyBirdGameManager manager)
            {
                _gameManager = manager;
                _hasConnectedToGameManager = true;
                Console.WriteLine("[PipeSpawner] Connected to Game Manager");
            }
        }
        
        Console.WriteLine($"[PipeSpawner] Settings - Speed: {_pipeSpeed}, Interval: {_spawnInterval}s, Gap: {_pipeGap}");
        Console.WriteLine($"[PipeSpawner] Spawn at X: {ScreenRight}, Destroy at X: {ScreenLeft}");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        _debugLogTimer += deltaTime;
        
        // Only spawn and move pipes during gameplay
        bool shouldBeActive = _gameManager?.GetGameState() == GameState.Playing;
        
        if (shouldBeActive)
        {
            HandlePipeSpawning(deltaTime);
            MovePipes(deltaTime);
            CleanupOffscreenPipes();
            CheckForScoring();
        }
        
        // Debug logging
        if (_enableDebugLogs && _debugLogTimer >= 2.0f)
        {
            int pipeCount = CountPipes();
            Console.WriteLine($"[PipeSpawner] Active pipes: {pipeCount}, Next spawn in: {_spawnInterval - _spawnTimer:F1}s");
            _debugLogTimer = 0.0f;
        }
    }

    private void HandlePipeSpawning(float deltaTime)
    {
        _spawnTimer += deltaTime;
        
        if (_spawnTimer >= _spawnInterval)
        {
            SpawnPipePair();
            _spawnTimer = 0.0f;
        }
    }

    private void SpawnPipePair()
    {
        // Random Y position for the gap center
        float gapCenterY = _random.NextSingle() * (_maxPipeY - _minPipeY) + _minPipeY;
        float topPipeY = gapCenterY + _pipeGap / 2.0f + _pipeHeight / 2.0f;
        float bottomPipeY = gapCenterY - _pipeGap / 2.0f - _pipeHeight / 2.0f;

        // Clamp the calculated Y values
        topPipeY = Math.Min(topPipeY, 2.5f);
        bottomPipeY = Math.Max(bottomPipeY, -1.0f);

        _pipeCounter++;

        Console.WriteLine($"[PipeSpawner] Spawning pipe pair #{_pipeCounter} at gap center Y: {gapCenterY:F2}");

        // Create top pipe
        CreatePipe($"Pipe_Top_{_pipeCounter}", ScreenRight, topPipeY, isTopPipe: true);

        // Create bottom pipe  
        CreatePipe($"Pipe_Bottom_{_pipeCounter}", ScreenRight, bottomPipeY, isTopPipe: false);

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
                Scale = new Vector3(_pipeWidth, _pipeHeight, 1.0f)
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
                Size = new Vector2(0.8f, _pipeHeight), // Forgiving: width matches visible pipe, but slightly smaller
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
                HasScored = false,
                PairId = _pipeCounter
            };
            pipeEntity.AddComponent(pipeData);
            
            Console.WriteLine($"[PipeSpawner] Created {name} at ({x:F2}, {y:F2})");

            // Ensure physics body is created for this pipe using global scene access
            // TODO: finish this
            //CurrentScene.Instance?.AddPhysicsBodyForEntity(pipeEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PipeSpawner] Error creating pipe {name}: {ex.Message}");
        }
    }

    private void MovePipes(float deltaTime)
    {
        float moveDistance = _pipeSpeed * deltaTime;
        
        foreach (var entity in CurrentScene.Instance?.Entities ?? new System.Collections.Concurrent.ConcurrentBag<Entity>())
        {
            if (entity.Name.Contains("Pipe_"))
            {
                if (entity.HasComponent<TransformComponent>() && entity.HasComponent<RigidBody2DComponent>())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    var rigidBody = entity.GetComponent<RigidBody2DComponent>();
                    
                    if (rigidBody?.RuntimeBody != null)
                    {
                        // Move pipe left using physics body
                        var body = rigidBody.RuntimeBody;
                        var currentPos = body.GetPosition();
                        var newPos = new Vector2(currentPos.X - moveDistance, currentPos.Y);
                        
                        Console.WriteLine($"Moving pipe {entity.Name}: X: {currentPos.X:F2} -> {newPos.X:F2}");
                        body.SetTransform(newPos, body.GetAngle());
                    }
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
                if (entity.HasComponent<TransformComponent>())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    if (transform.Translation.X < ScreenLeft)
                    {
                        pipesToRemove.Add(entity);
                        Console.WriteLine($"Removing pipe {entity.Name}");
                    }
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
                if (entity.HasComponent<TransformComponent>() && entity.HasComponent<PipeDataComponent>())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    var pipeData = entity.GetComponent<PipeDataComponent>();
                    
                    if (pipeData != null && !pipeData.HasScored)
                    {
                        float pipeX = transform.Translation.X;
                        
                        // If pipe has passed the bird (pipe right edge passed bird center)
                        if (pipeX + _pipeWidth/2 < BirdXPosition)
                        {
                            pipeData.HasScored = true;
                            
                            // Also mark the corresponding bottom pipe as scored
                            string bottomPipeName = entity.Name.Replace("Pipe_Top_", "Pipe_Bottom_");
                            var bottomPipe = FindEntity(bottomPipeName);
                            if (bottomPipe != null)
                            {
                                var bottomPipeData = bottomPipe.GetComponent<PipeDataComponent>();
                                if (bottomPipeData != null)
                                {
                                    bottomPipeData.HasScored = true;
                                }
                            }
                            
                            // Notify game manager
                            if (_gameManager != null)
                            {
                                _gameManager.IncrementScore();
                                Console.WriteLine($"[PipeSpawner] Score! Pipe pair {pipeData.PairId} passed bird at X: {BirdXPosition}");
                            }
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
        _spawnTimer = 0.0f;
        _pipeCounter = 0;
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
    public bool HasScored = false;
    public int PairId = 0;
}