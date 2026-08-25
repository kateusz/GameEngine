using ECS;

namespace SceneComponents;

public sealed class TiledObjectComponent : IComponent
{
    public int TiledId { get; set; }
    public string ObjectName { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.Ordinal);

    public IComponent Clone()
    {
        return new TiledObjectComponent
        {
            TiledId = TiledId,
            ObjectName = ObjectName,
            ObjectType = ObjectType,
            Properties = new Dictionary<string, string>(Properties, StringComparer.Ordinal)
        };
    }
}
