using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

public static class GameAssemblyCompiler
{
    private static readonly ILogger Logger = Log.ForContext(typeof(GameAssemblyCompiler));
    public const string AssemblyName = "GameAssembly";

    public static string GetNextEditorBuildPath(string outputDirectory) =>
        Path.Combine(outputDirectory, $"GameAssembly_{Guid.NewGuid():N}.dll");

    private const string PlaceholderSource = """
namespace GameAssembly;

internal static class _GameAssemblyPlaceholder
{
}
""";

    public static bool TryCompile(
        string scriptsDirectory,
        string outputDllPath,
        bool emitPdb,
        bool useDebugOptimization,
        [NotNullWhen(false)] out string[]? errors)
    {
        errors = null;
        if (string.IsNullOrWhiteSpace(scriptsDirectory) || !Directory.Exists(scriptsDirectory))
        {
            errors = new[] { $"Scripts directory is missing or invalid: {scriptsDirectory}" };
            return false;
        }

        var scriptFiles = Directory.GetFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories);
        var syntaxTrees = new List<SyntaxTree>();

        foreach (var scriptPath in scriptFiles)
        {
            if (string.Equals(Path.GetFileName(scriptPath), "GameAssembly.Placeholder.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var scriptContent = File.ReadAllText(scriptPath, Encoding.UTF8);
            var syntaxTree = CSharpSyntaxTree.ParseText(
                text: scriptContent,
                options: CSharpParseOptions.Default,
                path: scriptPath,
                encoding: Encoding.UTF8);
            syntaxTrees.Add(syntaxTree);
        }

        if (syntaxTrees.Count == 0)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                PlaceholderSource,
                CSharpParseOptions.Default,
                "GameAssembly.Placeholder.cs",
                Encoding.UTF8));
        }

        var references = ScriptCompilationReferences.GetMetadataReferences();
        var validation = ScriptCompilationReferences.ValidateReferences(references);
        if (!validation.Success)
        {
            errors = validation.Errors;
            return false;
        }

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: useDebugOptimization ? OptimizationLevel.Debug : OptimizationLevel.Release,
            allowUnsafe: true,
            platform: Microsoft.CodeAnalysis.Platform.AnyCpu,
            warningLevel: 4,
            deterministic: true,
            checkOverflow: false);

        var compilation = CSharpCompilation.Create(
            AssemblyName,
            syntaxTrees,
            references,
            compilationOptions);

        var preErrors = compilation.GetDiagnostics()
            .AsValueEnumerable()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (preErrors.Length > 0)
        {
            errors = preErrors.AsValueEnumerable().Select(d => d.ToString()).ToArray();
            return false;
        }

        var outDir = Path.GetDirectoryName(Path.GetFullPath(outputDllPath));
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        var emitOptions = new EmitOptions(
            debugInformationFormat: emitPdb
                ? DebugInformationFormat.PortablePdb
                : DebugInformationFormat.Embedded,
            includePrivateMembers: emitPdb);
        var pdbPath = emitPdb ? Path.ChangeExtension(outputDllPath, ".pdb") : null;

        using var peStream = File.Create(outputDllPath);
        EmitResult emitResult;
        if (emitPdb && pdbPath != null)
        {
            using var pdbStream = File.Create(pdbPath);
            emitResult = compilation.Emit(peStream, pdbStream, options: emitOptions);
        }
        else
        {
            emitResult = compilation.Emit(peStream, pdbStream: null, options: emitOptions);
        }

        if (!emitResult.Success)
        {
            errors = emitResult.Diagnostics
                .AsValueEnumerable()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .Distinct()
                .ToArray();
            return false;
        }

        Logger.Information("Compiled game assembly: {Path}", outputDllPath);
        return true;
    }
}
