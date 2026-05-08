using Scripting;

namespace Engine.Scripting;

public static class ScriptRuntimeStore
{
    private static readonly Dictionary<int, ScriptableEntity> RuntimeScripts = [];

    public static bool TryGet(int entityId, out ScriptableEntity scriptableEntity) =>
        RuntimeScripts.TryGetValue(entityId, out scriptableEntity!);

    public static void Set(int entityId, ScriptableEntity scriptableEntity) =>
        RuntimeScripts[entityId] = scriptableEntity;

    public static void Remove(int entityId) => RuntimeScripts.Remove(entityId);

    public static void Clear() => RuntimeScripts.Clear();
}
