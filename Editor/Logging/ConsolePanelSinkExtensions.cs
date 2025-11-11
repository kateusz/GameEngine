using Editor.Panels;
using Serilog;
using Serilog.Configuration;

namespace Editor.Logging;

/// <summary>
/// Extension methods for configuring ConsolePanelSink with Serilog.
/// </summary>
public static class ConsolePanelSinkExtensions
{
    /// <summary>
    /// Writes log events to the Editor's ConsolePanel.
    /// </summary>
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    /// <param name="consolePanel">The ConsolePanel instance to write to.</param>
    /// <param name="outputTemplate">Message template for formatting log events.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration ConsolePanel(
        this LoggerSinkConfiguration sinkConfiguration,
        IConsolePanel consolePanel,
        string outputTemplate = "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
        if (consolePanel == null) throw new ArgumentNullException(nameof(consolePanel));

        return sinkConfiguration.Sink(new ConsolePanelSink(consolePanel, outputTemplate));
    }
}
