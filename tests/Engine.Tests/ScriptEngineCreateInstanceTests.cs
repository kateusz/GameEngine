using Audio;
using Engine.Scene;
using Engine.Scripting;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class ScriptEngineCreateInstanceTests
{
    [Fact]
    public void CreateScriptInstance_SucceedsForScriptWithHierarchyConstructor()
    {
        var audio = Substitute.For<IAudio>();
        var audioPlayback = Substitute.For<IAudioPlayback>();
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.ActiveScene.Returns((IScene?)null);

        var engine = new ScriptEngine(audio, audioPlayback, sceneContext);
        engine.LoadGameAssemblyFromFile(typeof(WaterScript).Assembly.Location);

        var result = engine.CreateScriptInstance("WaterScript");

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error : null);
    }
}
