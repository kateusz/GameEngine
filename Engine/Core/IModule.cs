
using DryIoc;

namespace Engine.Core;

public interface IModule
{
    void Register(IContainer container);
    int Priority { get; } // Optional, for ordering
}
