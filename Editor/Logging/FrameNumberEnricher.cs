using Serilog.Core;
using Serilog.Events;

namespace Editor.Logging;

/// <summary>
/// Enriches log events with the current frame number.
/// </summary>
public class FrameNumberEnricher : ILogEventEnricher
{
    private static long _frameNumber;

    /// <summary>
    /// Updates the current frame number. Call this at the start of each frame.
    /// </summary>
    public static void IncrementFrame()
    {
        Interlocked.Increment(ref _frameNumber);
    }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public static long CurrentFrame => Interlocked.Read(ref _frameNumber);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var frameNumber = Interlocked.Read(ref _frameNumber);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Frame", frameNumber));
    }
}
