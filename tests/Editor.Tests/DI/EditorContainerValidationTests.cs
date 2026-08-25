using DryIoc;
using Editor.DI;
using Engine.Core.DI;
using Ui.ImGui.DI;

namespace Editor.Tests.DI;

public class EditorContainerValidationTests
{
    [Fact]
    public void ConfigureContainer_ValidatesWithoutErrors()
    {
        var container = new Container();
        EngineIoCContainer.RegisterCore(container);
        EngineIoCContainer.RegisterWindowing(container, EngineHostOptions.EditorDefaults);
        ImGuiIoCContainer.Register(container);
        EditorIoCContainer.Register(container);

        try
        {
            container.ValidateAndThrow();
        }
        catch (ContainerException ex)
        {
            var details = string.Join(
                Environment.NewLine,
                (ex.CollectedExceptions ?? []).Select(e => e.ToString()));
            throw new Xunit.Sdk.XunitException($"Container validation failed:{Environment.NewLine}{details}");
        }
    }
}
