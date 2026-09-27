using Editor.Features.Scene;
using Editor.Features.Selection;
using Engine.Scene;
using Serilog;

namespace Editor.Commands;

public class CommandRegistry(
    ISceneContext sceneContext,
    IEditorSelection selection,
    SceneHierarchyPanel hierarchyPanel)
{
    private static readonly ILogger Logger = Log.ForContext<CommandRegistry>();

    private readonly Dictionary<string, EditorCommand> _commands = new(StringComparer.Ordinal);

    public bool Register(EditorCommand command)
    {
        if (!_commands.TryAdd(command.Id, command))
        {
            Logger.Error("Duplicate command id '{Id}' ('{Title}'); not registered", command.Id, command.Title);
            return false;
        }

        return true;
    }

    public CanExecuteResult GetCanExecute(string id) =>
        _commands.TryGetValue(id, out var command)
            ? command.CanExecute()
            : CanExecuteResult.No("Unknown command");

    public void Execute(string id)
    {
        if (id.StartsWith(EditorCommandIds.EntityGotoPrefix, StringComparison.Ordinal))
        {
            JumpToEntity(id);
            return;
        }

        if (!_commands.TryGetValue(id, out var command))
        {
            Logger.Warning("Unknown command id '{Id}'", id);
            return;
        }

        if (!command.CanExecute().Allowed)
            return;

        try
        {
            command.Execute();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Command '{Id}' failed", id);
        }
    }

    public IReadOnlyList<CommandPaletteItem> GetWorkingSet()
    {
        try
        {
            var list = new List<CommandPaletteItem>(_commands.Count + 32);

            foreach (var command in _commands.Values)
            {
                var can = command.CanExecute();
                list.Add(new CommandPaletteItem(
                    command.Id,
                    command.Title,
                    command.Category,
                    command.ShortcutDisplay,
                    can.Allowed,
                    can.Reason));
            }

            if (sceneContext.ActiveScene is { } scene)
            {
                foreach (var entity in scene.Entities)
                {
                    list.Add(new CommandPaletteItem(
                        EditorCommandIds.EntityGoto(entity.Id),
                        entity.Name,
                        "Entities",
                        null,
                        true,
                        null));
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to build command working set");
            return [];
        }
    }

    private void JumpToEntity(string id)
    {
        var suffix = id[EditorCommandIds.EntityGotoPrefix.Length..];
        if (!int.TryParse(suffix, out var entityId))
            return;

        if (sceneContext.ActiveScene is not { } scene || !scene.Context.Contains(entityId))
            return;

        var entity = scene.Context.GetById(entityId);
        selection.Select(entity, SelectionSource.Code);
        hierarchyPanel.RequestScrollToEntity(entityId);
    }
}
