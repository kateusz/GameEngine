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

    public static MetadataReference[] GetMetadataReferences()
    {
        Logger.Debug("=== LOADING REFERENCES FOR SCRIPT COMPILATION ===");
        var references = new List<MetadataReference>();
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        Logger.Debug("Runtime directory: {RuntimeDir}", runtimeDir);
        LoadEssentialAssemblies(references, runtimeDir);
        LoadEngineAssembliesFromDomain(references);
        TryAddEcsAssembly(references);
        AddBox2D(references);
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

        return (errors.Count == 0, errors.ToArray());    }

    private static void LoadEssentialAssemblies(List<MetadataReference> references, string? runtimeDir)
    {
        if (string.IsNullOrEmpty(runtimeDir))
            return;

        var essentialAssemblies = new[]
        {
            "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Collections.dll", "System.Console.dll",
            "System.Linq.dll", "System.Numerics.dll", "System.Numerics.Vectors.dll", "netstandard.dll",
            "mscorlib.dll", "System.Collections.Concurrent.dll", "System.ComponentModel.dll"
        };

        foreach (var assemblyName in essentialAssemblies)
        {
            var path = Path.Combine(runtimeDir, assemblyName);
            if (!File.Exists(path))
            {
                Logger.Warning("Missing: {AssemblyName}", assemblyName);
                continue;
            }

            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error adding {AssemblyName}", assemblyName);
            }
        }
    }

    private static void LoadEngineAssembliesFromDomain(List<MetadataReference> references)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .Where(a => !a.IsDynamic)
            .ToArray();

        foreach (var assembly in loadedAssemblies)
        {
            var name = assembly.GetName().Name;
            if (name is null)
                continue;
            if (!name.StartsWith("Engine", StringComparison.Ordinal) &&
                !name.StartsWith("ECS", StringComparison.Ordinal) &&
                !name.StartsWith("Editor", StringComparison.Ordinal))
                continue;

            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    continue;
                }

                var currentDir = Environment.CurrentDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(currentDir, $"{name}.dll"),
                    Path.Combine(currentDir, "bin", DebugConfiguration, TargetFramework, $"{name}.dll"),
                    Path.Combine(currentDir, "..", name, "bin", DebugConfiguration, TargetFramework, $"{name}.dll")
                };
                if (!TryAddAssemblyFromPaths(references, possiblePaths))
                    Logger.Warning("Could not find assembly file for: {AssemblyName}", name);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error adding engine assembly {AssemblyName}", name);
            }
        }
    }

    private static bool TryAddAssemblyFromPaths(List<MetadataReference> references, string[] possiblePaths)
    {
        foreach (var possiblePath in possiblePaths)
        {
            if (!File.Exists(possiblePath))
                continue;
            references.Add(MetadataReference.CreateFromFile(possiblePath));
            return true;
        }

        return false;
    }

    private static void TryAddEcsAssembly(List<MetadataReference> references)
    {
        var ecsAssemblyPath = FindEcsAssembly();
        if (string.IsNullOrEmpty(ecsAssemblyPath))
        {
            Logger.Error("ECS assembly not found for script metadata references");
            return;
        }

        try
        {
            references.Add(MetadataReference.CreateFromFile(ecsAssemblyPath));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error adding ECS assembly");
        }
    }

    private static void AddBox2D(List<MetadataReference> references)
    {
        try
        {
            var box2dPath = Path.Combine(Environment.CurrentDirectory, "Box2D.NetStandard.dll");
            if (File.Exists(box2dPath))
                references.Add(MetadataReference.CreateFromFile(box2dPath));
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
