using System.Numerics;
using ECS;
using SceneComponents;

namespace Hounds.Marches;

static class March
{
    public static float Dt(TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;
        return dt > 0.05f ? 0.05f : dt;
    }

    public static MarchGameComponent? Game(Context context)
    {
        foreach (var (_, game) in context.View<MarchGameComponent>())
            return game.Pack != null ? game : null;
        return null;
    }

    public static Entity? Player(Context context)
    {
        foreach (var (entity, actor) in context.View<ActorComponent>())
            if (actor.Kind == "player")
                return entity;
        return null;
    }

    public static Entity? Named(Context context, string name)
    {
        foreach (var entity in context.Entities)
            if (entity.Name == name)
                return entity;
        return null;
    }

    public static RoomInfo Room(MarchGameComponent game) => new()
    {
        Id = game.LastRoom,
        CamMin = game.CamMin,
        CamMax = game.CamMax,
        CamY = game.CamY
    };

    public static Vector4 Fallback(string key) => key switch
    {
        "mud" => new(0.48f, 0.32f, 0.2f, 1f),
        "stone" => new(0.45f, 0.47f, 0.5f, 1f),
        "bridge" => new(0.46f, 0.3f, 0.16f, 1f),
        "palisade" => new(0.38f, 0.26f, 0.16f, 1f),
        "player" => new(0.62f, 0.2f, 0.18f, 1f),
        "swordsman" => new(0.78f, 0.48f, 0.2f, 1f),
        "halberdier" => new(0.32f, 0.3f, 0.28f, 1f),
        "boss" => new(0.16f, 0.12f, 0.12f, 1f),
        "puddle" => new(0.2f, 0.28f, 0.38f, 0.8f),
        _ => new(0.55f, 0.5f, 0.42f, 1f)
    };

    public static void Show(Entity entity, bool visible)
    {
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Visible = visible;
    }
}
