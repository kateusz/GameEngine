using ECS;
using Editor.Features.Project;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Editor.Features.Components;

public class GameComponentFactory(
    IProjectManager projectManager,
    IScriptEngine scriptEngine,
    IComponentSerializerRegistry serializerRegistry)
    : IGameComponentFactory
{
    private static readonly ILogger Logger = Log.ForContext<GameComponentFactory>();

    public string[] DiscoverComponentNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (projectManager.ScriptsDir is { } scriptsDir)
        {
            foreach (var name in GameComponentDiscovery.DiscoverFromScriptsDir(scriptsDir))
                names.Add(name);
        }

        if (scriptEngine.GetLoadedGameAssembly() is { } assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsClass: true, IsAbstract: false } && typeof(IGameComponent).IsAssignableFrom(type))
                    names.Add(type.Name);
            }
        }

        return names.OrderBy(n => n).ToArray();
    }

    public async Task<(bool Success, string? Error)> CreateAndAttachAsync(Entity entity, string baseName)
    {
        var scriptsDir = projectManager.ScriptsDir;
        if (scriptsDir is null)
            return (false, "Open a project first.");

        var className = GameComponentTemplates.ToClassName(baseName);
        if (entity.GetAllComponents().Any(c => c.GetType().Name == className))
            return (false, "Entity already has this component.");

        var filePath = Path.Combine(scriptsDir, $"{className}.cs");
        if (File.Exists(filePath))
            return (false, "Component file already exists.");

        await File.WriteAllTextAsync(filePath, GameComponentTemplates.Generate(className));
        Logger.Information("Created game component file {Path}", filePath);

        var (compiled, errors) = scriptEngine.TryCompileAllScripts();
        if (!compiled)
            return (false, string.Join('\n', errors.Take(5)));

        var type = scriptEngine.GetLoadedGameType(className);
        if (type is null)
            return (false, "Component compiled but type not found.");

        if (scriptEngine.GetLoadedGameAssembly() is { } assembly)
            serializerRegistry.RegisterFromAssembly(assembly);

        if (entity.GetAllComponents().Any(c => c.GetType() == type))
            return (false, "Entity already has this component.");

        entity.AddComponentDynamic((IComponent)Activator.CreateInstance(type)!);
        Logger.Information("Attached {ComponentName} to entity {EntityName}", className, entity.Name);
        return (true, null);
    }

    public (bool Success, string? Error) AttachExisting(Entity entity, string typeName)
    {
        if (projectManager.ScriptsDir is null)
            return (false, "Open a project first.");

        if (!DiscoverComponentNames().Contains(typeName))
            return (false, $"Component type '{typeName}' not found in scripts.");

        var type = scriptEngine.GetLoadedGameType(typeName);
        if (type is null)
        {
            var (compiled, errors) = scriptEngine.TryCompileAllScripts();
            if (!compiled)
                return (false, string.Join('\n', errors.Take(5)));

            type = scriptEngine.GetLoadedGameType(typeName);
        }

        if (type is null || !typeof(IGameComponent).IsAssignableFrom(type))
            return (false, $"Component type '{typeName}' could not be loaded.");

        if (entity.GetAllComponents().Any(c => c.GetType() == type))
            return (false, "Entity already has this component.");

        if (scriptEngine.GetLoadedGameAssembly() is { } assembly)
            serializerRegistry.RegisterFromAssembly(assembly);

        entity.AddComponentDynamic((IComponent)Activator.CreateInstance(type)!);
        Logger.Information("Attached existing {ComponentName} to entity {EntityName}", typeName, entity.Name);
        return (true, null);
    }
}
