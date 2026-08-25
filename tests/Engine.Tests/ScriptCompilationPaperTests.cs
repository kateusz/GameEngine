using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class ScriptCompilationPaperTests
{
    [Fact]
    public void TryCompile_SnakeScripts_Succeeds_WhenSdkIncludesProwl()
    {
        var scriptsDir = FindSnakeScriptsDirectory();
        var outputPath = Path.Combine(Path.GetTempPath(), $"GameAssembly_{Guid.NewGuid():N}.dll");
        try
        {
            GameAssemblyCompiler.TryCompile(
                scriptsDir,
                outputPath,
                emitPdb: false,
                useDebugOptimization: true,
                out var errors).ShouldBeTrue(string.Join(Environment.NewLine, errors ?? []));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    private static string FindSnakeScriptsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "games", "Snake", "assets", "scripts");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Snake scripts directory not found.");
    }
}
