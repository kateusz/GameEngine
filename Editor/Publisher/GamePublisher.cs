using System.Diagnostics;
using Serilog;

namespace Editor.Publisher;

public class GamePublisher : IGamePublisher
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<GamePublisher>();
    
    private static string _buildDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Builds");
    
    public void Publish()
    {
        var buildTargetDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Builds", "assets", "scripts")
        );
        
        Directory.CreateDirectory(buildTargetDir);
    }
    
    private void BuildGame()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "publish ../Runtime/Runtime.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true",
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Logger.Information(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Logger.Error("ERR: {ErrorData}", e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
    
    public static void CopyAssets(string buildOutput)
    {
        var assetsSource = Path.Combine(AppContext.BaseDirectory, "Assets");
        var assetsTarget = Path.Combine(buildOutput, "Assets");
        Directory.CreateDirectory(assetsTarget);
        foreach (var file in Directory.GetFiles(assetsSource, "*.*", SearchOption.AllDirectories))
        {
            var dest = file.Replace(assetsSource, assetsTarget);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
        }
    }

}