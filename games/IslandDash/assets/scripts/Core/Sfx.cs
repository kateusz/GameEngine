using Audio;

namespace IslandDash;

/// <summary>Sound effect paths (project-relative, resolved by the engine). Kenney CC0, see CREDITS.md.</summary>
public static class Sfx
{
    public const string Jump = "audio/jump.ogg";
    public const string DoubleJump = "audio/double_jump.ogg";
    public const string Spin = "audio/spin.ogg";
    public const string Collect = "audio/collect.ogg";
    public const string CrateBreak = "audio/crate_break.ogg";
    public const string Stomp = "audio/stomp.ogg";
    public const string Land = "audio/land.ogg";
    public const string Death = "audio/death.ogg";
    public const string Checkpoint = "audio/checkpoint.ogg";
    public const string ExtraLife = "audio/extra_life.ogg";
    public const string LevelComplete = "audio/level_complete.ogg";
    public const string GameOver = "audio/game_over.ogg";
    public const string Menu = "audio/menu.ogg";

    private static readonly HashSet<string> Failed = [];

    /// <summary>One-shot that never takes the game loop down: a missing/broken clip is reported once and then skipped.</summary>
    public static void PlaySfx(this IAudio audio, string path, float volume = 1f)
    {
        if (Failed.Contains(path))
            return;
        try
        {
            audio.PlayOneShot(path, volume);
        }
        catch (Exception ex)
        {
            Failed.Add(path);
            Console.WriteLine($"[IslandDash] Sound '{path}' disabled: {ex.Message}");
        }
    }
}
