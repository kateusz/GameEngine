using ECS;
using Engine.Events;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scripting;
using SceneComponents;
using Scripting;
using Serilog;

namespace Engine.Scene.Systems;

internal static class NativeScriptIteration
{
    private static readonly ILogger Logger = Log.ForContext(typeof(NativeScriptIteration));

    public static void Update(IContext context, IScriptEngine scriptEngine, ScriptRuntimeStore store, TimeSpan deltaTime)
    {
        foreach (var (entity, scriptComponent) in context.View<NativeScriptComponent>())
        {
            var scriptableEntity = GetOrCreate(scriptEngine, store, entity, scriptComponent);
            if (scriptableEntity == null)
                continue;

            if (!scriptableEntity.IsInitialized)
            {
                scriptableEntity.SetEntity(entity);
                try
                {
                    scriptableEntity.OnCreate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error initializing script on entity {EntityName}", entity.Name);
                }
            }

            try
            {
                scriptableEntity.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating script on entity {EntityName}", entity.Name);
            }
        }
    }

    public static void Shutdown(IContext context, ScriptRuntimeStore store)
    {
        var errorCount = 0;

        foreach (var (entity, _) in context.View<NativeScriptComponent>())
        {
            if (store.TryGet(entity.Id, out var scriptableEntity))
            {
                try
                {
                    scriptableEntity.OnDestroy();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in script OnDestroy for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
                    errorCount++;
                }
            }
            store.Remove(entity.Id);
        }

        if (errorCount > 0)
        {
            Logger.Warning(
                "Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.",
                errorCount);
        }
    }

    public static void Refresh(IContext context, IScriptEngine scriptEngine, ScriptRuntimeStore store)
    {
        foreach (var (entity, scriptComponent) in context.View<NativeScriptComponent>())
        {
            if (string.IsNullOrWhiteSpace(scriptComponent.ScriptTypeName))
                continue;

            if (scriptEngine.GetScriptType(scriptComponent.ScriptTypeName) is null)
                continue;

            var newInstance = scriptEngine.CreateScriptInstance(scriptComponent.ScriptTypeName);
            if (!newInstance.IsSuccess)
                continue;

            store.Set(entity.Id, newInstance.Value);
            newInstance.Value.SetEntity(entity);
            newInstance.Value.OnCreate();
        }
    }

    public static void ProcessEvent(IContext context, ScriptRuntimeStore store, Event @event)
    {
        foreach (var (entity, _) in context.View<NativeScriptComponent>())
        {
            if (!store.TryGet(entity.Id, out var scriptableEntity))
                continue;

            try
            {
                switch (@event)
                {
                    case KeyPressedEvent kpe:
                        scriptableEntity.OnKeyPressed(kpe.KeyCode);
                        break;
                    case KeyReleasedEvent kpe:
                        scriptableEntity.OnKeyReleased(kpe.KeyCode);
                        break;
                    case MouseButtonPressedEvent mbpe:
                        scriptableEntity.OnMouseButtonPressed(mbpe.Button);
                        break;
                    case MouseMovedEvent mme:
                        scriptableEntity.OnMouseMoved(mme.X, mme.Y);
                        break;
                    case MouseButtonReleasedEvent mbre:
                        scriptableEntity.OnMouseButtonReleased(mbre.Button);
                        break;
                    case MouseScrolledEvent mse:
                        scriptableEntity.OnMouseScrolled(mse.XOffSet, mse.YOffset);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing event in script on entity {EntityName}", entity.Name);
            }
        }
    }

    private static ScriptableEntity? GetOrCreate(
        IScriptEngine scriptEngine,
        ScriptRuntimeStore store,
        Entity entity,
        NativeScriptComponent scriptComponent)
    {
        if (store.TryGet(entity.Id, out var existing))
            return existing;

        if (string.IsNullOrWhiteSpace(scriptComponent.ScriptTypeName))
            return null;

        var result = scriptEngine.CreateScriptInstance(scriptComponent.ScriptTypeName);
        if (!result.IsSuccess)
            return null;

        store.Set(entity.Id, result.Value);
        return result.Value;
    }
}
