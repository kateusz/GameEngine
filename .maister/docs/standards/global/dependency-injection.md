## Dependency Injection

### No Static Singletons
Never create static singletons; register all singleton instances in the DryIoc container. Only pure constant classes (EditorUIConstants, RenderingConstants) may be static.

**Sources:** documentation, pr-reviews (confidence 90%)

```csharp
Forbidden: private static AnimationSystem? _instance; public static AnimationSystem Instance => ...
Correct: primary constructor injection + container.Register<AnimationSystem>(Reuse.Singleton)
```

### Constructor Injection and Primary Constructors
Inject all dependencies through primary constructors. Forbid property injection and service-locator Resolve calls.

**Sources:** code-patterns, documentation, pr-reviews (confidence 91%)

```csharp
internal sealed class AudioSystem(IAudio audio, IContext context, AudioPlaybackService playbackService) : ISystem
public class SpriteRendererComponentEditor(ITextureFactory textureFactory, UIPropertyRenderer propertyRenderer) : ComponentEditor<SpriteRendererComponent>
```

### DryIoc DI and Singleton Lifetime
Prefer Singleton for stateful services/managers/factories. Use Transient only for short-lived processors. Do not use Scoped. Singleton must not depend on Transient.

**Sources:** code-patterns, config, documentation (confidence 100%)

```csharp
Register services with DryIoc rather than introducing another DI container package
container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
```

### Interface Decision Guide
Use interfaces for multiple implementations, testability, or cross-module boundaries. Skip interfaces for editor panels, ECS systems, pure data classes, and component editors — register concrete types.

**Sources:** documentation (confidence 90%)

```csharp
Use interface: ISceneManager, ITextureFactory, IRendererAPI
Skip interface: ConsolePanel, AnimationSystem, Transform, TransformComponentEditor
```

### No Circular DI Dependencies
Forbid circular constructor dependencies; resolve via shared dependency extraction, events, or passing data as method parameters.

**Sources:** documentation (confidence 90%)

```csharp
Extract shared dependency; subscribe via events; pass Data as method parameter instead of injecting whole service
```
