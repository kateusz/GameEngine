using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Scene.Components;

namespace Engine.Scene.Serializer;

// TODO FINISH IT
public static class SkyboxSerializer
{
    private const string SkyboxComponentKey = "SkyboxComponent";
    private const string RightPathKey = "RightPath";
    private const string LeftPathKey = "LeftPath";
    private const string TopPathKey = "TopPath";
    private const string BottomPathKey = "BottomPath";
    private const string FrontPathKey = "FrontPath";
    private const string BackPathKey = "BackPath";

    public static void Serialize(Entity entity, JsonObject entityObj)
    {
        if (!entity.HasComponent<SkyboxComponent>())
            return;
        
        var component = entity.GetComponent<SkyboxComponent>();
        
        // Skip if no skybox is attached
        if (component.Skybox == null)
            return;
        
        // For skybox components, we need to save the paths to the textures
        // These would be stored somewhere in the component or skybox
        // This is just a placeholder implementation
        var skyboxObj = new JsonObject();
        skyboxObj["Name"] = SkyboxComponentKey;
        
        // In a real implementation, you would get these paths from the skybox
        // For now, this is just a placeholder to show the structure
        skyboxObj[RightPathKey] = "path/to/right.png";
        skyboxObj[LeftPathKey] = "path/to/left.png";
        skyboxObj[TopPathKey] = "path/to/top.png";
        skyboxObj[BottomPathKey] = "path/to/bottom.png";
        skyboxObj[FrontPathKey] = "path/to/front.png";
        skyboxObj[BackPathKey] = "path/to/back.png";
        
        var components = GetJsonArray(entityObj, "Components");
        components.Add(skyboxObj);
    }
    
    public static void Deserialize(Entity entity, JsonNode componentNode)
    {
        if (componentNode is not JsonObject componentObj)
            return;
        
        var skyboxComponent = new SkyboxComponent();
        
        // Get the paths to the skybox textures
        var rightPath = componentObj[RightPathKey]?.GetValue<string>();
        var leftPath = componentObj[LeftPathKey]?.GetValue<string>();
        var topPath = componentObj[TopPathKey]?.GetValue<string>();
        var bottomPath = componentObj[BottomPathKey]?.GetValue<string>();
        var frontPath = componentObj[FrontPathKey]?.GetValue<string>();
        var backPath = componentObj[BackPathKey]?.GetValue<string>();
        
        // If all paths are valid, create the skybox
        if (rightPath != null && leftPath != null && topPath != null && 
            bottomPath != null && frontPath != null && backPath != null)
        {
            var skybox = Renderer.SkyboxFactory.Create(
                rightPath, leftPath, topPath, bottomPath, frontPath, backPath);
            
            skyboxComponent.SetSkybox(skybox);
        }
        
        entity.AddComponent(skyboxComponent);
    }
    
    // Helper method to get a JsonArray from a JsonNode
    private static JsonArray GetJsonArray(JsonNode jsonObject, string key)
    {
        return jsonObject[key] as JsonArray ?? new JsonArray();
    }
}