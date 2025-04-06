using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class SkyboxComponent : Component
{
    public Skybox? Skybox { get; set; }
    
    public SkyboxComponent()
    {
    }
    
    public void SetSkybox(Skybox skybox)
    {
        Skybox = skybox;
    }
}