using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Engine.Core;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;

namespace Sandbox;

public class Program
{
    public static void Main(string[] args)
    {
        // Test script serialization
        TestScriptSerialization();
        
        // Original sandbox code
        var app = new SandboxApplication();
        app.Run();
    }
    
    private static void TestScriptSerialization()
    {
        Console.WriteLine("Testing script serialization...");
        
        // Create a test scene
        var scene = new Scene("test");
        
        // Create an entity with a script
        var entity = scene.CreateEntity("Test Camera");
        entity.AddComponent(new TransformComponent
        {
            Translation = new System.Numerics.Vector3(0, 0, 0),
            Rotation = new System.Numerics.Vector3(0, 0, 0),
            Scale = new System.Numerics.Vector3(1, 1, 1)
        });
        
        entity.AddComponent(new CameraComponent
        {
            Primary = true,
            FixedAspectRatio = false
        });
        
        entity.AddComponent(new NativeScriptComponent
        {
            ScriptableEntity = new CameraController()
        });
        
        // Save the scene
        string testPath = "test_script_scene.scene";
        SceneSerializer.Serialize(scene, testPath);
        Console.WriteLine($"Scene saved to {testPath}");
        
        // Create a new scene and load the saved one
        var loadedScene = new Scene("loaded_test");
        SceneSerializer.Deserialize(loadedScene, testPath);
        Console.WriteLine("Scene loaded successfully");
        
        // Verify the script was restored
        var loadedEntity = loadedScene.Entities.FirstOrDefault(e => e.Name == "Test Camera");
        if (loadedEntity != null)
        {
            if (loadedEntity.HasComponent<NativeScriptComponent>())
            {
                var scriptComponent = loadedEntity.GetComponent<NativeScriptComponent>();
                if (scriptComponent.ScriptableEntity != null)
                {
                    var scriptType = scriptComponent.ScriptableEntity.GetType().Name;
                    Console.WriteLine($"✓ Script restored successfully: {scriptType}");
                }
                else
                {
                    Console.WriteLine("✗ Script component exists but ScriptableEntity is null");
                }
            }
            else
            {
                Console.WriteLine("✗ NativeScriptComponent not found on loaded entity");
            }
        }
        else
        {
            Console.WriteLine("✗ Test Camera entity not found in loaded scene");
        }
        
        // Clean up
        if (File.Exists(testPath))
        {
            File.Delete(testPath);
            Console.WriteLine($"Test file {testPath} cleaned up");
        }
        
        Console.WriteLine("Script serialization test completed.");
        Console.WriteLine("Press any key to continue to the main application...");
        Console.ReadKey();
    }
}