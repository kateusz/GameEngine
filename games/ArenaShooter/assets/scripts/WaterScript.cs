using Audio;
using ECS;
using Scripting;

namespace ArenaShooter.assets.scripts;

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