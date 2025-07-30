using System.Diagnostics;

namespace Editor.Publisher;

public class GamePublisher
{
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
        process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
        process.ErrorDataReceived += (s, e) => Console.WriteLine("ERR: " + e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
    
    public static void CopyAssets(string buildOutput)
    {
        string assetsSource = Path.Combine(AppContext.BaseDirectory, "Assets");
        string assetsTarget = Path.Combine(buildOutput, "Assets");
        Directory.CreateDirectory(assetsTarget);
        foreach (var file in Directory.GetFiles(assetsSource, "*.*", SearchOption.AllDirectories))
        {
            var dest = file.Replace(assetsSource, assetsTarget);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
        }
    }

}