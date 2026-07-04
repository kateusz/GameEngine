using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class ScriptCompilationReferencesTests
{
    [Fact]
    public void GetMetadataReferences_IncludesSdkDlls_WhenProjectSdkPresent()
    {
        var scriptsDir = Path.Combine(Path.GetTempPath(), $"ge-sdk-ref-{Guid.NewGuid():N}");
        var sdkDir = Path.Combine(scriptsDir, ".engine", "sdk");
        try
        {
            Directory.CreateDirectory(sdkDir);
            var ecsDll = FindEcsDll();
            File.Copy(ecsDll, Path.Combine(sdkDir, "ECS.dll"));

            var references = ScriptCompilationReferences.GetMetadataReferences(scriptsDir);
            var (success, errors) = ScriptCompilationReferences.ValidateReferences(references);

            success.ShouldBeTrue(string.Join("; ", errors));
        }
        finally
        {
            if (Directory.Exists(scriptsDir))
                Directory.Delete(scriptsDir, recursive: true);
        }
    }

    private static string FindEcsDll()
    {
        var ecsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ECS");
        if (ecsAssembly is not null && !string.IsNullOrEmpty(ecsAssembly.Location) && File.Exists(ecsAssembly.Location))
            return ecsAssembly.Location;

        var candidate = Path.Combine(AppContext.BaseDirectory, "ECS.dll");
        if (File.Exists(candidate))
            return candidate;

        throw new InvalidOperationException("ECS.dll not found for SDK reference test.");
    }
}
