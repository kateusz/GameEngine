using Scripting;

namespace Engine.Scene;

public sealed class ScriptRuntimeStore
{
    private readonly Dictionary<int, ScriptableEntity> _scriptsByEntityId = [];

    public bool TryGet(int entityId, out ScriptableEntity script) =>
        _scriptsByEntityId.TryGetValue(entityId, out script!);

    public void Set(int entityId, ScriptableEntity script) => _scriptsByEntityId[entityId] = script;

    public void Remove(int entityId) => _scriptsByEntityId.Remove(entityId);

    public void Clear() => _scriptsByEntityId.Clear();
}
