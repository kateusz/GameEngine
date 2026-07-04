using System.Reflection;
using Microsoft.CodeAnalysis;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

internal static class ScriptCompilationReferences
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ScriptCompilationReferences));
    private const string DebugConfiguration = "Debug";
    private const string TargetFramework = "net10.0";
    private const string EcsDllName = "ECS.dll";

    private static readonly string[] GameScriptSupportAssemblyNames =
    [
        "Scripting",
        "SceneComponents",
        "Input",
        "Math"
    ];

    public static MetadataReference[] GetMetadataReferences()
    {
        Logger.Debug("=== LOADING REFERENCES FOR SCRIPT COMPILATION ===");
        var references = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        Logger.Debug("Runtime directory: {RuntimeDir}", runtimeDir);
        LoadEssentialAssemblies(references, addedPaths, runtimeDir);
        LoadEngineAssembliesFromDomain(references, addedPaths);
        TryAddEcsAssembly(references, addedPaths);
        TryAddGameScriptSupportAssembliesFromDisk(references, addedPaths);
        AddBox2D(references, addedPaths);
        Logger.Debug("Total references added: {ReferenceCount}", references.Count);
        return references.ToArray();
    }

    public static (bool Success, string[] Errors) ValidateReferences(MetadataReference[] references)
    {
        var errors = new List<string>();
        var referenceNames = new HashSet<string>();
        foreach (var reference in references)
        {
            if (reference is PortableExecutableReference peRef && !string.IsNullOrEmpty(peRef.FilePath))
                referenceNames.Add(Path.GetFileNameWithoutExtension(peRef.FilePath));
        }

        var requiredAssemblies = new[] { "System.Private.CoreLib", "System.Runtime", "System.Numerics.Vectors", "ECS" };
        foreach (var required in requiredAssemblies)
        {
            if (!referenceNames.Contains(required))
                errors.Add($"Missing required assembly: {required}");
        }

        return (errors.Count == 0, errors.ToArray());
    }

    private static bool TryAddReference(List<MetadataReference> references, HashSet<string> addedPaths, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        var full = Path.GetFullPath(path);
        if (!addedPaths.Add(full))
            return true;

        try
        {
            references.Add(MetadataReference.CreateFromFile(full));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error adding metadata reference {Path}", full);
            addedPaths.Remove(full);
            return false;
        }
    }

    private static void LoadEssentialAssemblies(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        string? runtimeDir)
    {
        if (string.IsNullOrEmpty(runtimeDir))
            return;

        var essentialAssemblies = new[]
        {
            "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Collections.dll", "System.Console.dll",
            "System.Linq.dll", "System.Numerics.dll", "System.Numerics.Vectors.dll", "netstandard.dll",
            "mscorlib.dll", "System.Collections.Concurrent.dll", "System.ComponentModel.dll",
            "System.Collections.Immutable.dll", "System.Memory.dll", "System.Runtime.InteropServices.dll"
        };

        foreach (var assemblyName in essentialAssemblies)
        {
            var path = Path.Combine(runtimeDir, assemblyName);
            if (!File.Exists(path))
            {
                Logger.Warning("Missing: {AssemblyName}", assemblyName);
                continue;
            }

            TryAddReference(references, addedPaths, path);
        }
    }

    private static bool IncludeAssemblyForScriptMetadata(string? assemblySimpleName)
    {
        if (assemblySimpleName is null)
            return false;

        if (assemblySimpleName.StartsWith("Engine", StringComparison.Ordinal) ||
            assemblySimpleName.StartsWith("ECS", StringComparison.Ordinal) ||
            assemblySimpleName.StartsWith("Editor", StringComparison.Ordinal))
            return true;

        return GameScriptSupportAssemblyNames.AsValueEnumerable()
            .Any(n => assemblySimpleName.Equals(n, StringComparison.Ordinal));
    }

    private static void LoadEngineAssembliesFromDomain(List<MetadataReference> references, HashSet<string> addedPaths)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .Where(a => !a.IsDynamic)
            .ToArray();

        foreach (var assembly in loadedAssemblies)
        {
            var name = assembly.GetName().Name;
            if (!IncludeAssemblyForScriptMetadata(name))
                continue;

            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    TryAddReference(references, addedPaths, assembly.Location);
                    continue;
                }

                var currentDir = Environment.CurrentDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(currentDir, $"{name}.dll"),
                    Path.Combine(currentDir, "bin", DebugConfiguration, TargetFramework, $"{name}.dll"),
                    Path.Combine(currentDir, "..", name, "bin", DebugConfiguration, TargetFramework, $"{name}.dll")
                };
                if (!TryAddAssemblyFromPaths(references, addedPaths, possiblePaths))
                    Logger.Warning("Could not find assembly file for: {AssemblyName}", name);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error adding engine assembly {AssemblyName}", name);
            }
        }
    }

    private static bool TryAddAssemblyFromPaths(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        string[] possiblePaths)
    {
        foreach (var possiblePath in possiblePaths)
        {
            if (TryAddReference(references, addedPaths, possiblePath))
                return true;
        }

        return false;
    }

    private static void TryAddEcsAssembly(List<MetadataReference> references, HashSet<string> addedPaths)
    {
        var ecsAssemblyPath = FindEcsAssembly();
        if (string.IsNullOrEmpty(ecsAssemblyPath))
        {
            Logger.Error("ECS assembly not found for script metadata references");
            return;
        }

        TryAddReference(references, addedPaths, ecsAssemblyPath);
    }

    private static void TryAddGameScriptSupportAssembliesFromDisk(List<MetadataReference> references, HashSet<string> addedPaths)
    {
        foreach (var assemblyName in GameScriptSupportAssemblyNames)
        {
            var path = FindProjectOutputDll(assemblyName);
            if (path is not null)
                TryAddReference(references, addedPaths, path);
        }
    }

    private static string? FindProjectOutputDll(string assemblyName)
    {
        var dll = $"{assemblyName}.dll";
        var engineDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var candidates = new List<string>();

        if (!string.IsNullOrEmpty(engineDir))
            candidates.Add(Path.Combine(engineDir, dll));

        var currentDir = Environment.CurrentDirectory;
        candidates.Add(Path.Combine(currentDir, dll));
        candidates.Add(Path.Combine(currentDir, "bin", DebugConfiguration, TargetFramework, dll));
        candidates.Add(Path.Combine(currentDir, "..", assemblyName, "bin", DebugConfiguration, TargetFramework, dll));

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static void AddBox2D(List<MetadataReference> references, HashSet<string> addedPaths)
    {
        try
        {
            var box2dPath = Path.Combine(Environment.CurrentDirectory, "Box2D.NetStandard.dll");
            TryAddReference(references, addedPaths, box2dPath);

            if (!string.IsNullOrEmpty(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                var nextToEngine = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    "Box2D.NetStandard.dll");
                TryAddReference(references, addedPaths, nextToEngine);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error adding Box2D");
        }
    }

    private static string? FindEcsAssembly()
    {
        var currentDir = Environment.CurrentDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, EcsDllName),
            Path.Combine(currentDir, "bin", DebugConfiguration, TargetFramework, EcsDllName),
            Path.Combine(currentDir, "..", "ECS", "bin", DebugConfiguration, TargetFramework, EcsDllName),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", EcsDllName)
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        var ecsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .FirstOrDefault(a => a.GetName().Name == "ECS");
        if (ecsAssembly != null && !string.IsNullOrEmpty(ecsAssembly.Location))
            return ecsAssembly.Location;
        return null;
    }
}
