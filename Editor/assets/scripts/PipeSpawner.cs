// using System;
// using System.Numerics;
// using ECS;
// using Engine.Scene;
// using Engine.Scene.Components;
// using Engine.Renderer.Textures;
//
// public class PipeSpawner : ScriptableEntity
// {
//     private float spawnTimer = 0.0f;
//     private float spawnInterval = 2.0f; // Spawn pipe every 2 seconds
//     private float pipeSpeed = 3.0f;
//     private float pipeGap = 4.0f; // Gap between top and bottom pipes
//     private float spawnX = 15.0f; // Spawn pipes off-screen to the right
//     private Random random = new Random();
//     
//     public override void OnCreate()
//     {
//         Console.WriteLine("Pipe Spawner initialized!");
//     }
//     
//     public override void OnUpdate(TimeSpan ts)
//     {
//         spawnTimer += (float)ts.TotalSeconds;
//         
//         if (spawnTimer >= spawnInterval)
//         {
//             SpawnPipePair();
//             spawnTimer = 0.0f;
//         }
//         
//         // Move existing pipes and clean up old ones
//         MovePipes(ts);
//         CleanupOldPipes();
//     }
//     
//     private void SpawnPipePair()
//     {
//         // Random Y position for the gap center
//         float gapCenterY = random.Next(-2, 3); // Random between -2 and 2
//         
//         // Create bottom pipe
//         var bottomPipe = CreateEntity($"BottomPipe_{DateTime.Now.Ticks}");
//         var bottomTransform = bottomPipe.AddComponent<TransformComponent>();
//         bottomTransform.Translation = new Vector3(spawnX, gapCenterY - pipeGap/2 - 2.0f, 0);
//         bottomTransform.Scale = new Vector3(1, 4, 1);
//         
//         var bottomSprite = bottomPipe.AddComponent<SpriteRendererComponent>();
//         // TODO: Load pipe texture
//         bottomSprite.Color = Vector4.One;
//         
//         var bottomRigidBody = bottomPipe.AddComponent<RigidBody2DComponent>();
//         bottomRigidBody.BodyType = RigidBodyType.Static;
//         
//         var bottomCollider = bottomPipe.AddComponent<BoxCollider2DComponent>();
//         bottomCollider.Size = new Vector2(0.5f, 2.0f);
//         
//         // Create top pipe
//         var topPipe = CreateEntity($"TopPipe_{DateTime.Now.Ticks}");
//         var topTransform = topPipe.AddComponent<TransformComponent>();
//         topTransform.Translation = new Vector3(spawnX, gapCenterY + pipeGap/2 + 2.0f, 0);
//         topTransform.Scale = new Vector3(1, 4, 1);
//         topTransform.Rotation = new Vector3(0, 0, MathF.PI); // Flip top pipe
//         
//         var topSprite = topPipe.AddComponent<SpriteRendererComponent>();
//         topSprite.Color = Vector4.One;
//         
//         var topRigidBody = topPipe.AddComponent<RigidBody2DComponent>();
//         topRigidBody.BodyType = RigidBodyType.Static;
//         
//         var topCollider = topPipe.AddComponent<BoxCollider2DComponent>();
//         topCollider.Size = new Vector2(0.5f, 2.0f);
//         
//         Console.WriteLine($"Spawned pipe pair at gap center Y: {gapCenterY}");
//     }
//     
//     private void MovePipes(TimeSpan ts)
//     {
//         // Find all pipe entities and move them left
//         foreach (var entity in CurrentScene.Entities)
//         {
//             if (entity.Name.Contains("Pipe"))
//             {
//                 var transform = entity.GetComponent<TransformComponent>();
//                 if (transform != null)
//                 {
//                     var position = transform.Translation;
//                     position.X -= pipeSpeed * (float)ts.TotalSeconds;
//                     transform.Translation = position;
//                 }
//             }
//         }
//     }
//     
//     private void CleanupOldPipes()
//     {
//         // Remove pipes that have moved too far to the left
//         var pipesToRemove = new List<Entity>();
//         
//         foreach (var entity in CurrentScene.Entities)
//         {
//             if (entity.Name.Contains("Pipe"))
//             {
//                 var transform = entity.GetComponent<TransformComponent>();
//                 if (transform != null && transform.Translation.X < -10.0f)
//                 {
//                     pipesToRemove.Add(entity);
//                 }
//             }
//         }
//         
//         foreach (var pipe in pipesToRemove)
//         {
//             DestroyEntity(pipe);
//         }
//     }
// }