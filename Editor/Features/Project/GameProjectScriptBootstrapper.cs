using System.Text;
using System.Text.Json;
using Serilog;

namespace Editor.Features.Project;

public sealed class GameProjectScriptBootstrapper : IGameProjectScriptBootstrapper
{
    private static readonly ILogger Logger = Log.ForContext<GameProjectScriptBootstrapper>();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool TryInstallScriptSdkForNewProject(string projectRoot, string projectDisplayName, out string error)
    {
        error = string.Empty;
        if (!TryCopySdkFromStaging(projectRoot, out var stagingPath, out var config, out var copyError))
        {
            error = copyError;
            return false;
        }

        try
        {
            var scriptsDir = Path.Combine(projectRoot, "assets", "scripts");
            var fileBase = SanitizeFileName(projectDisplayName);
            if (string.IsNullOrEmpty(fileBase))
                fileBase = "Game";

            WriteManifest(scriptsDir, stagingPath, config);
            WriteGameScriptsCsproj(scriptsDir, fileBase);
            WriteStarterScript(scriptsDir, ToNamespaceIdentifier(projectDisplayName));
            AppendGitignoreRule(projectRoot);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to write script project files: {ex.Message}";
            Logger.Error(ex, "Script SDK bootstrap failed after copy");
            return false;
        }
    }

    public void TryEnsureScriptSdkAfterOpen(string projectRoot)
    {
        var sdkDir = GetSdkDirectory(projectRoot);
        if (Directory.Exists(sdkDir) &&
            Directory.EnumerateFiles(sdkDir, "*.dll").Any() &&
            HasPaperUiSdkDlls(sdkDir))
            return;

        if (!TryCopySdkFromStaging(projectRoot, out _, out _, out var err))
        {
            Logger.Warning("Script SDK folder is empty and could not be refreshed: {Reason}", err);
            return;
        }

        Logger.Information("Populated local script SDK under {SdkDir}", sdkDir);
    }

    private static string GetSdkDirectory(string projectRoot) =>
        Path.Combine(projectRoot, "assets", "scripts", ".engine", "sdk");

    private static bool HasPaperUiSdkDlls(string sdkDir) =>
        File.Exists(Path.Combine(sdkDir, "Paper.dll")) &&
        File.Exists(Path.Combine(sdkDir, "Scribe.dll")) &&
        File.Exists(Path.Combine(sdkDir, "Vector.dll"));

    private static bool TryCopySdkFromStaging(
        string projectRoot,
        out string stagingPath,
        out string configuration,
        out string error)
    {
        stagingPath = string.Empty;
        configuration = GameEngineCheckoutLocator.DefaultSdkConfiguration;
        error = string.Empty;

        var staging = GameEngineCheckoutLocator.TryGetGameScriptSdkStagingDirectory(configuration);
        if (staging is null)
        {
            error =
                $"Game script SDK not found at artifacts/GameScriptSdk/{configuration}/net10.0. Build the engine solution first (e.g. dotnet build GameScriptSdk/GameScriptSdk.csproj).";
            return false;
        }

        stagingPath = staging;
        var dest = GetSdkDirectory(projectRoot);
        if (Directory.Exists(dest))
            Directory.Delete(dest, recursive: true);
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.EnumerateFiles(staging))
        {
            var ext = Path.GetExtension(file);
            if (!string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".pdb", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(dest, name), overwrite: true);
        }

        if (!Directory.EnumerateFiles(dest, "*.dll").Any())
        {
            error = "Staging folder contained no DLL files to copy.";
            return false;
        }

        return true;
    }

    private static void WriteManifest(string scriptsDir, string stagingPath, string configuration)
    {
        var engineRoot = GameEngineCheckoutLocator.TryFindEngineCheckoutRoot() ?? "";
        var manifestPath = Path.Combine(scriptsDir, ".engine", "sdk-manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var dto = new SdkManifestDto(
            EngineCheckout: engineRoot,
            StagingPath: stagingPath,
            Configuration: configuration,
            CopiedAtUtc: DateTime.UtcNow);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static void WriteGameScriptsCsproj(string scriptsDir, string fileBase)
    {
        var path = Path.Combine(scriptsDir, $"{fileBase}.csproj");
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine();
        sb.AppendLine("    <PropertyGroup>");
        sb.AppendLine("        <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("        <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("        <Nullable>enable</Nullable>");
        sb.AppendLine("    </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("    <ItemGroup>");
        sb.AppendLine("        <Reference Include=\"ECS\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/ECS.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Input\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Input.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Audio\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Audio.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"SceneComponents\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/SceneComponents.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Scripting\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Scripting.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Math\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Math.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Box2D.NetStandard\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Box2D.NetStandard.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Paper\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Paper.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Scribe\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Scribe.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("        <Reference Include=\"Vector\">");
        sb.AppendLine("            <HintPath>$(MSBuildProjectDirectory)/.engine/sdk/Vector.dll</HintPath>");
        sb.AppendLine("        </Reference>");
        sb.AppendLine("    </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");
        sb.AppendLine();
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteStarterScript(string scriptsDir, string namespaceIdentifier)
    {
        var path = Path.Combine(scriptsDir, "GameScriptsEntry.cs");
        var body =
            $"namespace {namespaceIdentifier};" + Environment.NewLine +
            Environment.NewLine +
            "public static class GameScriptsEntry" + Environment.NewLine +
            "{" + Environment.NewLine +
            "}" + Environment.NewLine;
        File.WriteAllText(path, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void AppendGitignoreRule(string projectRoot)
    {
        const string rule = "assets/scripts/.engine/";
        var gitignorePath = Path.Combine(projectRoot, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            var text = File.ReadAllText(gitignorePath);
            if (text.Contains(rule, StringComparison.Ordinal))
                return;
            File.AppendAllText(gitignorePath, Environment.NewLine + rule + Environment.NewLine + "Builds/");
            return;
        }

        File.WriteAllText(gitignorePath, rule + Environment.NewLine);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars).Trim();
    }

    private static string ToNamespaceIdentifier(string projectDisplayName)
    {
        var sb = new StringBuilder();
        foreach (var c in projectDisplayName.Trim())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_')
                sb.Append('_');
        }

        var s = sb.ToString().Trim('_');
        if (s.Length == 0)
            return "Game";
        if (char.IsDigit(s[0]))
            return "Game_" + s;
        return s;
    }

    private sealed record SdkManifestDto(
        string EngineCheckout,
        string StagingPath,
        string Configuration,
        DateTime CopiedAtUtc);
}
