using Shouldly;

namespace Engine.Tests.Renderer;

/// <summary>
/// Four-host lighting VS must stay in sync for GPU skinning (I3 / FR-05).
/// File-presence smoke — no GL context required (CI headless).
/// </summary>
[Trait("Category", "Unit")]
public class LightingShaderSkinnedHostsTests
{
    private static readonly string[] HostRoots =
    [
        "Editor",
        "Runtime",
        "Sandbox",
        "Benchmark"
    ];

    [Fact]
    public void AllHostLightingVertexShaders_AreByteIdentical()
    {
        var repoRoot = FindRepoRoot();
        repoRoot.ShouldNotBeNull("Could not locate repo root from test base directory");

        string? reference = null;
        string? referenceHost = null;
        foreach (var host in HostRoots)
        {
            var path = Path.Combine(repoRoot, host, "assets", "shaders", "OpenGL", "lightingShader.vert");
            File.Exists(path).ShouldBeTrue($"Missing skinned lighting VS: {path}");

            var src = File.ReadAllText(path);
            src.Contains("a_BoneIndexF", StringComparison.Ordinal).ShouldBeTrue(path);
            src.Contains("a_BoneWeight", StringComparison.Ordinal).ShouldBeTrue(path);
            src.Contains("u_BoneMatrices[100]", StringComparison.Ordinal).ShouldBeTrue(path);
            src.Contains("MulRowVectorMatrix", StringComparison.Ordinal).ShouldBeTrue(path);
            src.Contains("SkinMatrix", StringComparison.Ordinal).ShouldBeTrue(path);

            if (reference is null)
            {
                reference = src;
                referenceHost = host;
                continue;
            }

            src.ShouldBe(reference, $"{host}/lightingShader.vert drifted from {referenceHost}/");
        }
    }

    private static string? FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Editor", "assets", "shaders", "OpenGL", "lightingShader.vert"))
                && File.Exists(Path.Combine(dir.FullName, "Engine", "Engine.csproj")))
                return dir.FullName;
        }

        return null;
    }
}
