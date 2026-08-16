using ECS;
using Engine.Renderer;

namespace Engine.Scene;

/// <summary>
/// Runtime-only: mesh path resolved to a factory-owned <see cref="Model"/> at change time. Not serialized.
/// </summary>
internal sealed class ResolvedModelComponent : IComponent
{
    public string? SourcePath { get; set; }
    public Model? Model { get; set; }

    public IComponent Clone() => new ResolvedModelComponent();
}
