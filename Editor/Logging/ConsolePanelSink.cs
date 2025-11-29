using Editor.Panels;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Editor.Logging;

/// <summary>
/// Custom Serilog sink that writes log events to the Editor's ConsolePanel.
/// </summary>
public class ConsolePanelSink(
    IConsolePanel consolePanel,
    string outputTemplate = "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    : ILogEventSink
{
    private readonly IConsolePanel _consolePanel = consolePanel;
    private readonly ITextFormatter _formatter = new MessageTemplateTextFormatter(outputTemplate);

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        // Format the log event to a string
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var message = writer.ToString().TrimEnd('\r', '\n');

        // Map Serilog level to ConsolePanel level
        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => ConsolePanel.LogLevel.Info,
            LogEventLevel.Debug => ConsolePanel.LogLevel.Info,
            LogEventLevel.Information => ConsolePanel.LogLevel.Info,
            LogEventLevel.Warning => ConsolePanel.LogLevel.Warning,
            LogEventLevel.Error => ConsolePanel.LogLevel.Error,
            LogEventLevel.Fatal => ConsolePanel.LogLevel.Error,
            _ => ConsolePanel.LogLevel.Info
        };

        _consolePanel.AddMessage(message, level);
    }
}
