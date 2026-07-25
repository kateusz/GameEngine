using Audio;
using ECS;
using Input;
using Math;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

public class WaterScript : ScriptableEntity
{
    private readonly IAudio _audio;

    public WaterScript(IComponentAccessor componentAccessor, IAudio audio, IAudioPlayback audioPlayback,
        IPhysicsQueries physicsQueries, IEntityHierarchy hierarchy)
        : base(componentAccessor, audio, audioPlayback, physicsQueries, hierarchy)
    {
        _audio = audio;
    }

    public override void OnTriggerEnter(Entity other)
    {
        if (other.Name != "Player")
            return;

        _audio.PlayOneShot("assets/sounds/water-walk.wav");
    }
}