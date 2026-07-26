using System.Collections.Concurrent;
using System.Numerics;
using Editor.UI.Constants;

namespace Editor.Publisher;

public class PublishProgress : IProgress<string>
{
    private readonly ConcurrentQueue<string> _buildOutput = new();
    private readonly object _lock = new();

    public string CurrentStep { get; private set; } = "";
    public string? ErrorMessage { get; private set; }
    public float Progress { get; private set; }
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

    public void SetProgress(float progress) =>
        Progress = System.Math.Clamp(progress, 0.0f, 1.0f);

    public void SetFailed(string errorMessage)
    {
        lock (_lock)
        {
            HasError = true;
            IsComplete = true;
            ErrorMessage = errorMessage;
            CurrentStep = "Publish failed";
        }
        _buildOutput.Enqueue(errorMessage);
    }

    public void SetSucceeded(string outputPath)
    {
        lock (_lock)
        {
            HasError = false;
            IsComplete = true;
            ErrorMessage = null;
            CurrentStep = "Publish completed successfully";
            Progress = 1.0f;
        }
        _buildOutput.Enqueue($"Publish completed successfully!");
        _buildOutput.Enqueue($"Output: {outputPath}");
    }

    public static Vector4 ProgressBarColor(bool hasError, bool isComplete) =>
        hasError ? EditorUIConstants.ErrorColor
        : isComplete ? EditorUIConstants.SuccessColor
        : new Vector4(0.2f, 0.5f, 0.9f, 1f);
}
