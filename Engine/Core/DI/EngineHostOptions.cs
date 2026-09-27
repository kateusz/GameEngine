using Engine.Core.Window;

namespace Engine.Core.DI;

public sealed record EngineHostOptions(string WindowTitle, int WindowWidth, int WindowHeight)
{
    public bool Maximized { get; init; }
    public bool Fullscreen { get; init; }
    public int TargetFrameRate { get; init; }

    /// <summary>
    /// When <c>true</c>, <c>ModelFactory</c> rebuilds or refreshes the sibling <c>.mesh</c> file from the
    /// source model (e.g. <c>.glb</c>) before loading it at runtime.
    /// </summary>
    /// <remarks>
    /// <para>Default is <c>false</c> (including shipped games and the editor host): loading goes straight to the
    /// pre-generated <c>{source}.mesh</c> next to the asset. If that file is missing or invalid, the load fails.</para>
    /// <para>Set to <c>true</c> in standalone runtime or sandbox hosts when you want the engine to call
    /// <c>RuntimeMeshBuilder.TryEnsureUpToDate</c> on each <c>ModelFactory.Create</c> — Assimp import and write
    /// only when the sibling is missing or its stamp no longer matches the source file.</para>
    /// <para>The editor ignores this flag; <c>EditorModelLoadService</c> always ensures siblings on a background
    /// thread before handing bytes to <c>ModelFactory</c>.</para>
    /// </remarks>
    public bool EnsureRuntimeMeshSiblingOnLoad { get; init; }

    public static EngineHostOptions EditorDefaults => new(
        "MulEngine",
        (int)DisplayConfig.DefaultWindowWidth,
        (int)DisplayConfig.DefaultWindowHeight)
    {
        Maximized = true,
        EnsureRuntimeMeshSiblingOnLoad = false
    };
}
