using System.Numerics;

namespace Engine.Scene;

/// <summary>
/// Configuration settings for physics simulation in a scene.
/// Allows customization of gravity, solver iterations, and other physics parameters.
/// </summary>
public class ScenePhysicsSettings
{
    /// <summary>
    /// Gravity vector applied to all dynamic bodies in the scene.
    /// Default is Earth gravity (-9.8 m/s² in Y direction).
    /// Set to Zero for space/zero-gravity scenes, or customize for moon, underwater, etc.
    /// </summary>
    public Vector2 Gravity { get; set; } = new Vector2(0, -9.8f);

    /// <summary>
    /// Number of velocity constraint solver iterations per physics step.
    /// Higher values = more accurate but slower. Default is 6.
    /// Range: 1-20 (typical: 6-8 for games, 8-10 for simulations)
    /// </summary>
    public int VelocityIterations { get; set; } = 6;

    /// <summary>
    /// Number of position constraint solver iterations per physics step.
    /// Higher values = more accurate but slower. Default is 2.
    /// Range: 1-10 (typical: 2-3 for games, 3-5 for simulations)
    /// </summary>
    public int PositionIterations { get; set; } = 2;

    /// <summary>
    /// Whether to allow bodies to sleep when at rest.
    /// Sleeping bodies don't consume CPU until awakened by collision or external forces.
    /// Default is true. Set to false for debugging or if sleep causes issues.
    /// </summary>
    public bool AllowSleeping { get; set; } = true;

    /// <summary>
    /// Time scale for physics simulation.
    /// 1.0 = normal speed, 0.5 = slow motion, 2.0 = fast forward.
    /// Default is 1.0. Does not affect fixed timestep, only accumulator.
    /// </summary>
    public float TimeScale { get; set; } = 1.0f;

    /// <summary>
    /// Creates default physics settings with Earth gravity and standard solver parameters.
    /// </summary>
    public ScenePhysicsSettings()
    {
    }

    /// <summary>
    /// Creates physics settings with custom gravity.
    /// </summary>
    /// <param name="gravity">Custom gravity vector (e.g., Vector2.Zero for space)</param>
    public ScenePhysicsSettings(Vector2 gravity)
    {
        Gravity = gravity;
    }

    /// <summary>
    /// Creates a zero-gravity configuration for space scenes.
    /// </summary>
    public static ScenePhysicsSettings ZeroGravity() => new(Vector2.Zero);

    /// <summary>
    /// Creates a low-gravity configuration for moon-like scenes.
    /// Moon gravity is approximately 1/6th of Earth's.
    /// </summary>
    public static ScenePhysicsSettings MoonGravity() => new(new Vector2(0, -1.62f));

    /// <summary>
    /// Creates a high-quality physics configuration with more solver iterations.
    /// Use for puzzle games or simulations requiring high precision.
    /// </summary>
    public static ScenePhysicsSettings HighQuality() => new()
    {
        VelocityIterations = 8,
        PositionIterations = 3
    };

    /// <summary>
    /// Creates a performance-optimized physics configuration with fewer solver iterations.
    /// Use for fast-paced action games with many physics objects.
    /// </summary>
    public static ScenePhysicsSettings Performance() => new()
    {
        VelocityIterations = 4,
        PositionIterations = 1
    };
}
