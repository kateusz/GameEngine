using System.Text;
using ImGuiNET;
using Editor.UI.Constants;
using Editor.UI.Drawers;

namespace Editor.Panels;

public class ConsolePanel : IConsolePanel
{
    private volatile List<LogMessage> _logMessages = new();
    private readonly Lock _writeSync = new();

    private bool _autoScroll = true;
    private string _filterText = string.Empty;
    private readonly ConsoleTextWriter _consoleWriter;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private bool _showInfo = true;
    private bool _showWarnings = true;
    private bool _showErrors = true;
    private const int MaxMessages = 1000;

    public ConsolePanel()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;

        _consoleWriter = new ConsoleTextWriter(this);
        Console.SetOut(_consoleWriter);
        Console.SetError(_consoleWriter);
    }

    public void AddMessage(string message, LogLevel level = LogLevel.Info)
    {
        var logMessage = new LogMessage
        {
            Text = message,
            Timestamp = DateTime.Now,
            Level = level
        };

        lock (_writeSync)
        {
            // Copy current list and modify the copy
            var newList = new List<LogMessage>(_logMessages) { logMessage };

            if (newList.Count > MaxMessages)
                newList.RemoveRange(0, newList.Count - MaxMessages);

            // Atomically replace the reference
            _logMessages = newList;
        }
    }

    public void Clear()
    {
        lock (_writeSync)
        {
            _logMessages = [];
        }
    }

    public void Draw()
    {
        ImGui.Begin("Console");

        RenderToolbar();
        ImGui.Separator();
        RenderLogDisplay();

        ImGui.End();
    }

    private void RenderToolbar()
    {
        // Clear button
        ButtonDrawer.DrawButton("Clear", Clear);
        ImGui.SameLine();

        // Auto-scroll checkbox
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);

        // Filter controls
        ImGui.SameLine();
        LayoutDrawer.DrawFilterInput("Filter", ref _filterText);

        // Log level filters
        ImGui.SameLine();
        LayoutDrawer.DrawColoredCheckbox("Info", ref _showInfo, EditorUIConstants.InfoColor);

        ImGui.SameLine();
        LayoutDrawer.DrawColoredCheckbox("Warnings", ref _showWarnings, EditorUIConstants.WarningColor);

        ImGui.SameLine();
        LayoutDrawer.DrawColoredCheckbox("Errors", ref _showErrors, EditorUIConstants.ErrorColor);
    }

    private void RenderLogDisplay()
    {
        ImGui.BeginChild("ConsoleLog");

        var filteredMessages = GetFilteredMessages();
        foreach (var message in filteredMessages)
        {
            RenderLogMessage(message);
        }

        if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }

        ImGui.EndChild();
    }

    private List<LogMessage> GetFilteredMessages()
    {
        var snapshot = _logMessages; // atomic reference read

        return snapshot
            .Where(message =>
            {
                var levelFilter = message.Level switch
                {
                    LogLevel.Info => _showInfo,
                    LogLevel.Warning => _showWarnings,
                    LogLevel.Error => _showErrors,
                    _ => true
                };

                if (!levelFilter)
                    return false;

                if (!string.IsNullOrEmpty(_filterText))
                {
                    return message.Text.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
                }

                return true;
            })
            .ToList(); // materialize to prevent race on deferred LINQ
    }

    private void RenderLogMessage(LogMessage message)
    {
        // Use semantic helper methods for appropriate message types
        switch (message.Level)
        {
            case LogLevel.Error:
                TextDrawer.DrawErrorText(message.Text);
                break;
            case LogLevel.Warning:
                TextDrawer.DrawWarningText(message.Text);
                break;
            default:
                TextDrawer.DrawInfoText(message.Text);
                break;
        }

        LayoutDrawer.DrawContextMenu(
            $"MessageContext_{message.GetHashCode()}",
            ("Copy", () => ImGui.SetClipboardText(message.Text))
        );
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _consoleWriter?.Dispose();
    }

    private class LogMessage
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}

internal class ConsoleTextWriter : TextWriter
{
    private readonly IConsolePanel _panel;
    private readonly TextWriter _originalOut;
    private readonly StringBuilder _lineBuffer = new();

    public ConsoleTextWriter(IConsolePanel panel)
    {
        _panel = panel;
        _originalOut = Console.Out;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        if (value != null)
        {
            var level = DetermineLogLevel(value);
            _panel.AddMessage(value, level);
            _originalOut.WriteLine(value);
        }
    }

    public override void Write(string? value)
    {
        if (value == null)
            return;

        _lineBuffer.Append(value);

        if (value.EndsWith('\n'))
        {
            var line = _lineBuffer.ToString().TrimEnd('\n', '\r');
            if (!string.IsNullOrEmpty(line))
            {
                var level = DetermineLogLevel(line);
                _panel.AddMessage(line, level);
            }
            _lineBuffer.Clear();
        }

        _originalOut.Write(value);
    }

    private static ConsolePanel.LogLevel DetermineLogLevel(string message)
    {
        var messageSpan = message.AsSpan();

        if (messageSpan.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.Contains("❌", StringComparison.OrdinalIgnoreCase))
        {
            return ConsolePanel.LogLevel.Error;
        }

        if (messageSpan.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.Contains("warn", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.Contains("⚠", StringComparison.OrdinalIgnoreCase) ||
            messageSpan.StartsWith("warning:", StringComparison.OrdinalIgnoreCase))
        {
            return ConsolePanel.LogLevel.Warning;
        }

        return ConsolePanel.LogLevel.Info;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _lineBuffer.Clear();
        base.Dispose(disposing);
    }
}
