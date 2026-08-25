using System.Reflection;
using Microsoft.CodeAnalysis;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

internal static class ScriptCompilationReferences
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ScriptCompilationReferences));

    private static readonly string[] GameScriptSupportAssemblyNames =
    [
        "ECS",
        "Audio",
        "Scripting",
        "SceneComponents",
        "Input",
        "Math",
        "UI.Paper",
        "Paper",
        "Quill",
        "Scribe",
        "Vector"
    ];

    private static readonly string[] EssentialRuntimeAssemblies =
    [
        "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Collections.dll", "System.Console.dll",
        "System.Linq.dll", "System.Numerics.dll", "System.Numerics.Vectors.dll", "netstandard.dll",
        "mscorlib.dll", "System.Collections.Concurrent.dll", "System.ComponentModel.dll",
        "System.Collections.Immutable.dll", "System.Memory.dll", "System.Runtime.InteropServices.dll"
    ];

    public static MetadataReference[] GetMetadataReferences(string? scriptsDirectory = null)
    {
        Logger.Debug("Loading script compilation references...");
        var references = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        LoadRuntimeAssemblies(references, addedPaths, addedNames);
        if (!string.IsNullOrWhiteSpace(scriptsDirectory))
        {
            var sdkDir = Path.Combine(scriptsDirectory, ".engine", "sdk");
            if (Directory.Exists(sdkDir))
                LoadSdkAssemblies(references, addedPaths, addedNames, sdkDir);
        }

        LoadEngineAssembliesFromDomain(references, addedPaths, addedNames);
        foreach (var assemblyName in GameScriptSupportAssemblyNames)
            EnsureReference(references, addedPaths, addedNames, assemblyName);

        AddBox2D(references, addedPaths, addedNames);
        Logger.Debug("Loaded {ReferenceCount} script compilation references", references.Count);
        return references.ToArray();
    }

    public static (bool Success, string[] Errors) ValidateReferences(MetadataReference[] references)
    {
        if (references.Length == 0)
            return (false, ["No metadata references were loaded for script compilation."]);

        var referenceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references)
        {
            if (reference is PortableExecutableReference { FilePath: { } filePath } &&
                !string.IsNullOrEmpty(filePath))
                referenceNames.Add(Path.GetFileNameWithoutExtension(filePath));
        }

        var required = new[] { "System.Private.CoreLib", "System.Runtime", "System.Numerics.Vectors", "ECS" };
        var missing = required.Where(r => !referenceNames.Contains(r)).ToArray();
        return missing.Length == 0
            ? (true, [])
            : (false, missing.Select(m => $"Missing required assembly: {m}").ToArray());
    }

    private static void LoadRuntimeAssemblies(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (string.IsNullOrEmpty(runtimeDir))
            return;

        foreach (var assemblyName in EssentialRuntimeAssemblies)
            TryAddReference(references, addedPaths, addedNames, Path.Combine(runtimeDir, assemblyName));
    }

    private static void EnsureReference(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames,
        string assemblyName)
    {
        if (addedNames.Contains(assemblyName))
            return;

        var path = FindOutputDll(assemblyName);
        if (path is not null)
            TryAddReference(references, addedPaths, addedNames, path);
    }

    private static bool TryAddReference(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            AssemblyName.GetAssemblyName(path);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Skipping non-assembly metadata reference {Path}", path);
            return false;
        }

        var full = Path.GetFullPath(path);
        if (!addedPaths.Add(full))
        {
            TrackReferenceName(addedNames, full);
            return true;
        }

        try
        {
            references.Add(MetadataReference.CreateFromFile(full));
            TrackReferenceName(addedNames, full);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error adding metadata reference {Path}", full);
            addedPaths.Remove(full);
            return false;
        }
    }

    private static void TrackReferenceName(HashSet<string> addedNames, string path) =>
        addedNames.Add(Path.GetFileNameWithoutExtension(path));

    private static void LoadSdkAssemblies(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames,
        string sdkDir)
    {
        foreach (var dllPath in Directory.EnumerateFiles(sdkDir, "*.dll"))
            TryAddReference(references, addedPaths, addedNames, dllPath);
    }

    private static bool IncludeAssemblyForScriptMetadata(string? assemblySimpleName)
    {
        if (assemblySimpleName is null)
            return false;

        if (assemblySimpleName.StartsWith("Engine", StringComparison.Ordinal) ||
            assemblySimpleName.StartsWith("ECS", StringComparison.Ordinal) ||
            assemblySimpleName.Equals("Paper", StringComparison.Ordinal) ||
            assemblySimpleName.Equals("Quill", StringComparison.Ordinal) ||
            assemblySimpleName.Equals("Scribe", StringComparison.Ordinal) ||
            assemblySimpleName.Equals("Vector", StringComparison.Ordinal) ||
            assemblySimpleName.StartsWith("Editor", StringComparison.Ordinal))
            return true;

        return GameScriptSupportAssemblyNames.AsValueEnumerable()
            .Any(n => assemblySimpleName.Equals(n, StringComparison.Ordinal));
    }

    private static void LoadEngineAssembliesFromDomain(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().AsValueEnumerable().Where(a => !a.IsDynamic))
        {
            var name = assembly.GetName().Name;
            if (!IncludeAssemblyForScriptMetadata(name))
                continue;

            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    TryAddReference(references, addedPaths, addedNames, assembly.Location);
                    continue;
                }

                if (name is not null && FindOutputDll(name) is { } path)
                    TryAddReference(references, addedPaths, addedNames, path);
                else
                    Logger.Warning("Could not find assembly file for: {AssemblyName}", name);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error adding engine assembly {AssemblyName}", name);
            }
        }
    }

    private static string? FindOutputDll(string assemblyName)
    {
        var dll = $"{assemblyName}.dll";
        var engineDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var candidates = new List<string?>();

        if (engineDir is not null)
            candidates.Add(Path.Combine(engineDir, dll));

        candidates.Add(Path.Combine(AppContext.BaseDirectory, dll));

        var currentDir = Environment.CurrentDirectory;
        candidates.Add(Path.Combine(currentDir, dll));
        candidates.Add(Path.Combine(currentDir, "bin", "Debug", "net10.0", dll));
        candidates.Add(Path.Combine(currentDir, "..", assemblyName, "bin", "Debug", "net10.0", dll));

        foreach (var path in candidates)
        {
            if (path is not null && File.Exists(path))
                return path;
        }

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
        if (loaded is not null && !string.IsNullOrEmpty(loaded.Location))
            return loaded.Location;

        return null;
    }

    private static void AddBox2D(
        List<MetadataReference> references,
        HashSet<string> addedPaths,
        HashSet<string> addedNames)
    {
        try
        {
            var engineDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(engineDir))
                TryAddReference(references, addedPaths, addedNames, Path.Combine(engineDir, "Box2D.NetStandard.dll"));

            TryAddReference(
                references,
                addedPaths,
                addedNames,
                Path.Combine(Environment.CurrentDirectory, "Box2D.NetStandard.dll"));
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error adding Box2D");
        }
    }
}
