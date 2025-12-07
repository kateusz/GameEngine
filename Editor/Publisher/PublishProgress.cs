using System.Collections.Concurrent;

namespace Editor.Publisher;

public class PublishProgress : IProgress<string>
{
    private readonly ConcurrentQueue<string> _buildOutput = new();
    private readonly object _lock = new();

    public string CurrentStep { get; private set; } = "";
    public float Progress { get; private set; } = 0.0f;
    public bool IsComplete { get; set; }
    public bool HasError { get; set; }

    public IEnumerable<string> BuildOutput => _buildOutput;

    public void Report(string value)
    {
        lock (_lock)
        {
            CurrentStep = value;
        }
        _buildOutput.Enqueue(value);
    }

    public void SetProgress(float progress)
    {
        Progress = Math.Clamp(progress, 0.0f, 1.0f);
    }
}
