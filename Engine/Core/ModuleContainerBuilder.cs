

using DryIoc;

namespace Engine.Core;

public class ModuleContainerBuilder
{
    private readonly IContainer _container = new Container();
    private readonly List<IModule> _modules = new();

    public ModuleContainerBuilder RegisterModule<T>() where T : IModule, new()
    {
        _modules.Add(new T());
        return this;
    }

    public IContainer Build()
    {
        foreach (var module in _modules.OrderBy(m => m.Priority))
        {
            module.Register(_container);
        }
        return _container;
    }
}
