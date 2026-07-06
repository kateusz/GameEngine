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

    private static readonly CSharpParseOptions ScriptParseOptions = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Latest);

    private const string ScriptGlobalUsingsSource = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Numerics;
        global using System.Globalization;
        global using System.Text;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private const string EmptyPlaceholderSource = """
                                                  namespace GameAssembly;

                                                  internal static class EmptyGameAssemblyPlaceholder
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
            errors = [$"Scripts directory is missing or invalid: {scriptsDirectory}"];
            return false;
        }

        var scriptFiles = EnumerateGameScriptFiles(scriptsDirectory);
        var syntaxTrees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(
                ScriptGlobalUsingsSource,
                ScriptParseOptions,
                path: "GameAssembly.GlobalUsings.g.cs",
                encoding: Encoding.UTF8)
        };

        foreach (var scriptPath in scriptFiles)
        {
            var scriptContent = File.ReadAllText(scriptPath, Encoding.UTF8);
            var syntaxTree = CSharpSyntaxTree.ParseText(
                text: scriptContent,
                options: ScriptParseOptions,
                path: scriptPath,
                encoding: Encoding.UTF8);
            syntaxTrees.Add(syntaxTree);
        }

        if (syntaxTrees.Count == 1)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                EmptyPlaceholderSource,
                ScriptParseOptions,
                "GameAssembly.Placeholder.cs",
                Encoding.UTF8));
        }

        var references = ScriptCompilationReferences.GetMetadataReferences(scriptsDirectory);
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

        var preErrors = ToErrorStrings(compilation.GetDiagnostics());
        if (preErrors.Length > 0)
        {
            errors = preErrors;
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
        using var pdbStream = emitPdb && pdbPath is not null ? File.Create(pdbPath) : null;
        var emitResult = compilation.Emit(peStream, pdbStream, options: emitOptions);

        if (!emitResult.Success)
        {
            errors = ToErrorStrings(emitResult.Diagnostics, distinct: true);
            return false;
        }

        Logger.Information("Compiled game assembly: {Path}", outputDllPath);
        return true;
    }

    public static IEnumerable<string> EnumerateGameScriptFiles(string scriptsDirectory)
    {
        if (!Directory.Exists(scriptsDirectory))
            return [];

        return Directory
            .EnumerateFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => ShouldIncludeGameScriptFile(path, scriptsDirectory));
    }

    private static bool ShouldIncludeGameScriptFile(string filePath, string scriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var fullPath = Path.GetFullPath(filePath);
        var root = Path.GetFullPath(scriptsDirectory);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return false;

        var relative = Path.GetRelativePath(root, fullPath);
        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var fileName = Path.GetFileName(fullPath);
        if (fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("AssemblyAttributes", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string[] ToErrorStrings(IEnumerable<Diagnostic> diagnostics, bool distinct = false)
    {
        var query = diagnostics
            .AsValueEnumerable()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString());
        return distinct ? query.Distinct().ToArray() : query.ToArray();
    }
}
