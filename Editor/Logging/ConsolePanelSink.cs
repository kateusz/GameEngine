using Editor.Panels;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Editor.Logging;

/// <summary>
/// Custom Serilog sink that writes log events to the Editor's ConsolePanel.
/// </summary>
public class ConsolePanelSink : ILogEventSink
{
    private readonly ConsolePanel _consolePanel;
    private readonly ITextFormatter _formatter;

    public ConsolePanelSink(ConsolePanel consolePanel, string outputTemplate = "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        _consolePanel = consolePanel ?? throw new ArgumentNullException(nameof(consolePanel));
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }

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
