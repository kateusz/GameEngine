namespace Engine.Core;

/// <summary>
/// Centralized time management for the engine.
/// Handles time scaling, pause state, and delta time calculations.
/// </summary>
/// <remarks>
/// Performance: Static class with minimal overhead. All time calculations are
/// performed once per frame in the Application main loop, then cached for frame-wide access.
/// Thread Safety: Not thread-safe. Should only be updated from the main game thread.
/// </remarks>
public static class Time
{
    private static float _timeScale = 1.0f;
    private static TimeSpan _deltaTime = TimeSpan.Zero;
    private static TimeSpan _unscaledDeltaTime = TimeSpan.Zero;
    private static double _totalTime = 0.0;

    /// <summary>
    /// Time scale factor. 0 = paused, 1 = normal speed, 2 = double speed, etc.
    /// Setting this to 0 effectively pauses all gameplay without stopping rendering.
    /// </summary>
    /// <remarks>
    /// Negative values are clamped to 0. Common values:
    /// - 0.0f: Paused (freeze gameplay)
    /// - 0.5f: Slow motion (50% speed)
    /// - 1.0f: Normal speed
    /// - 2.0f: Fast forward (2x speed)
    /// </remarks>
    public static float TimeScale
    {
        get => _timeScale;
        set => _timeScale = Math.Max(0.0f, value); // Prevent negative time scale
    }

    /// <summary>
    /// Delta time scaled by TimeScale. Use this for gameplay logic.
    /// When paused (TimeScale = 0), this will be zero, effectively freezing gameplay.
    /// </summary>
    public static TimeSpan DeltaTime => _deltaTime;

    /// <summary>
    /// Delta time without time scaling. Use for UI, camera, particles, or anything
    /// that should continue updating even when the game is paused.
    /// </summary>
    public static TimeSpan UnscaledDeltaTime => _unscaledDeltaTime;

    /// <summary>
    /// Total elapsed time since application start (scaled by TimeScale).
    /// This value does not increase when the game is paused.
    /// </summary>
    public static double TotalTime => _totalTime;

    /// <summary>
    /// Delta time as float seconds (scaled). Convenience property for gameplay code.
    /// Equivalent to (float)DeltaTime.TotalSeconds.
    /// </summary>
    public static float DeltaTimeF => (float)_deltaTime.TotalSeconds;

    /// <summary>
    /// Unscaled delta time as float seconds. Convenience property for UI code.
    /// Equivalent to (float)UnscaledDeltaTime.TotalSeconds.
    /// </summary>
    public static float UnscaledDeltaTimeF => (float)_unscaledDeltaTime.TotalSeconds;

    /// <summary>
    /// True if time is currently frozen (TimeScale = 0).
    /// Useful for checking pause state without accessing SceneManager.
    /// </summary>
    public static bool IsPaused => _timeScale == 0.0f;

    /// <summary>
    /// Internal method to update time values each frame.
    /// Should only be called by the Application main loop.
    /// </summary>
    /// <param name="unscaledDelta">The real-time delta since last frame</param>
    internal static void Update(TimeSpan unscaledDelta)
    {
        _unscaledDeltaTime = unscaledDelta;
        _deltaTime = TimeSpan.FromSeconds(unscaledDelta.TotalSeconds * _timeScale);
        _totalTime += _deltaTime.TotalSeconds;
    }

    /// <summary>
    /// Reset time tracking (useful when entering/exiting play mode).
    /// Clears total time and resets time scale to 1.0f.
    /// </summary>
    internal static void Reset()
    {
        _totalTime = 0.0;
        _deltaTime = TimeSpan.Zero;
        _unscaledDeltaTime = TimeSpan.Zero;
        _timeScale = 1.0f;
    }
}
