using System.Numerics;
using System.Text;
using ImGuiNET;

namespace Editor.Panels;

public class ConsolePanel
{
    private readonly List<LogMessage> _logMessages = new();
    private readonly object _lockObject = new();
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
        // Store original console outputs
        _originalOut = Console.Out;
        _originalError = Console.Error;
        
        // Create and set our custom writer
        _consoleWriter = new ConsoleTextWriter(this);
        Console.SetOut(_consoleWriter);
        Console.SetError(_consoleWriter);
    }

    public void AddMessage(string message, LogLevel level = LogLevel.Info)
    {
        lock (_lockObject)
        {
            var logMessage = new LogMessage
            {
                Text = message,
                Timestamp = DateTime.Now,
                Level = level
            };
            
            _logMessages.Add(logMessage);
            
            // Keep only last N messages to prevent memory issues
            if (_logMessages.Count > MaxMessages)
            {
                _logMessages.RemoveRange(0, _logMessages.Count - MaxMessages);
            }
        }
    }

    public void OnImGuiRender()
    {
        ImGui.Begin("Console");

        // Toolbar
        RenderToolbar();
        
        ImGui.Separator();

        // Log display
        RenderLogDisplay();

        ImGui.End();
    }

    private void RenderToolbar()
    {
        // Clear button
        if (ImGui.Button("Clear"))
        {
            Clear();
        }

        ImGui.SameLine();
        
        // Auto-scroll checkbox
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);

        // Filter controls
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("Filter", ref _filterText, 256);

        // Log level filters
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
        ImGui.Checkbox("Info", ref _showInfo);
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
        ImGui.Checkbox("Warnings", ref _showWarnings);
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
        ImGui.Checkbox("Errors", ref _showErrors);
        ImGui.PopStyleColor();
    }

    private void RenderLogDisplay()
    {
        ImGui.BeginChild("ConsoleLog");
        lock (_lockObject)
        {
            var filteredMessages = GetFilteredMessages();
            
            foreach (var message in filteredMessages)
            {
                RenderLogMessage(message);
            }
        }

        // Auto-scroll to bottom
        if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }

        ImGui.EndChild();
    }

    private IEnumerable<LogMessage> GetFilteredMessages()
    {
        return _logMessages.Where(message =>
        {
            // Filter by log level
            var levelFilter = message.Level switch
            {
                LogLevel.Info => _showInfo,
                LogLevel.Warning => _showWarnings,
                LogLevel.Error => _showErrors,
                _ => true
            };

            if (!levelFilter) return false;

            // Filter by text using ReadOnlySpan<char> for better performance
            if (!string.IsNullOrEmpty(_filterText))
            {
                var messageSpan = message.Text.AsSpan();
                var filterSpan = _filterText.AsSpan();
                return messageSpan.Contains(filterSpan, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        });
    }

    private void RenderLogMessage(LogMessage message)
    {
        // Set color based on log level
        Vector4 color = message.Level switch
        {
            LogLevel.Warning => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),  // Yellow
            LogLevel.Error => new Vector4(1.0f, 0.3f, 0.3f, 1.0f),    // Red
            _ => new Vector4(0.9f, 0.9f, 0.9f, 1.0f)                  // Light gray
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);

        // Display only the message text, no timestamp
        var displayText = message.Text;

        ImGui.TextUnformatted(displayText);
        
        ImGui.PopStyleColor();

        // Context menu for individual messages
        if (ImGui.BeginPopupContextItem($"MessageContext_{message.GetHashCode()}"))
        {
            if (ImGui.MenuItem("Copy"))
            {
                ImGui.SetClipboardText(displayText);
            }
            ImGui.EndPopup();
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _logMessages.Clear();
        }
    }

    public void Dispose()
    {
        // Restore original console outputs
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
    private readonly ConsolePanel _panel;
    private readonly TextWriter _originalOut;
    private readonly StringBuilder _lineBuffer = new();

    public ConsoleTextWriter(ConsolePanel panel)
    {
        _panel = panel;
        _originalOut = Console.Out;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        if (value != null)
        {
            // Determine log level based on content
            var level = DetermineLogLevel(value);
            _panel.AddMessage(value, level);
            _originalOut.WriteLine(value); // Also write to original console
        }
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            _lineBuffer.Append(value);
            
            // If we have a complete line, process it
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
    }

    private static ConsolePanel.LogLevel DetermineLogLevel(string message)
    {
        var messageSpan = message.AsSpan();
        
        // Use ReadOnlySpan<char> for more efficient string operations
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
        if (disposing)
        {
            _lineBuffer.Clear();
        }
        base.Dispose(disposing);
    }
}