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
    
    public override void OnCreate()
    {
        Console.WriteLine("Pipe Spawner initialized!");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
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
        // Remove pipes that have moved too far to the left
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
    }
}