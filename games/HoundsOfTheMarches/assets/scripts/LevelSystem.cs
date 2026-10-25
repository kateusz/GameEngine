using ECS;
using ECS.Systems;
using Engine.Project;
using Engine.Scene;
using Scripting;

namespace Hounds.Marches;

/// <summary>
/// Binds the art pack to a March entity already authored in the scene.
/// Levels are saved scenes — no runtime geometry spawn.
/// </summary>
[Register(typeof(IGameSystem))]
public sealed class LevelSystem(Context context, ISceneContext scenes) : IGameSystem
{
    public int Priority => 80;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (scenes.ActiveScene is null)
            return;

        foreach (var (_, game) in context.View<MarchGameComponent>())
        {
            if (game.Pack != null)
                return;

            try
            {
                var root = PathBuilder.AssetsPath;
                game.Art = AssetPack.Select(root, Environment.GetCommandLineArgs());
                game.Pack = AssetPack.Load(root, game.Art);
                game.Built = true;
                game.Fade = 1f;
                if (game.HelpTimer <= 0f)
                    game.HelpTimer = 0f;
                if (game.BannerKey.Length == 0 && game.LastRoom.Length > 0)
                {
                    game.BannerKey = game.LastRoom switch
                    {
                        "village" => "title_village",
                        "bridge" => "title_bridge",
                        "courtyard" => "title_courtyard",
                        _ => ""
                    };
                    if (game.BannerKey.Length > 0)
                        game.BannerTimer = 2.2f;
                }

                Console.WriteLine($"Hounds of the Marches — scene ready, art '{game.Art}'.");
                Console.WriteLine("A/D move, Space jump, W/S ladder, J light, K heavy, L block, Shift dodge, E rest.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Level bind failed: {ex}");
            }

            return;
        }
    }
}
