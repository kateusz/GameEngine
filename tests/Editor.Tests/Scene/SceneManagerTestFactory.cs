using ECS.Systems;
using Editor.Features.History;
using Editor.Features.Scene;
using Editor.Features.Scripting;
using Engine.Core;
using Engine.Core.Window;
using Engine.Project;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using NSubstitute;

namespace Editor.Tests.Scene;

internal static class SceneManagerTestFactory
{
    public static SceneManager Create(
        ISceneContext sceneContext,
        IProjectContext projectContext,
        IEditorHistory history,
        ISceneSerializer? serializer = null) =>
        new(
            sceneContext,
            serializer ?? Substitute.For<ISceneSerializer>(),
            new SceneFactory(Substitute.For<ISceneSystemsFactory>(), Substitute.For<IPointerSurface>()),
            () => Enumerable.Empty<IGameSystem>(),
            projectContext,
            new GameScriptWorkspace(
                Substitute.For<IScriptEngine>(),
                Substitute.For<IComponentSerializerRegistry>(),
                _ => true,
                _ => { }),
            history);
}
